using System.Collections.Concurrent;
using Game.Persistence;
using Serilog;

namespace Game.Server.Persistence;

public enum LoadOutcome
{
    Loaded,

    /// <summary>Não existe ou é de outra conta.</summary>
    NotFound,

    /// <summary>Banco fora do ar ou documento corrompido.</summary>
    Failed,
}

/// <summary>Resultado de um carregamento pedido por <see cref="CharacterPersistence.BeginLoad"/>, entregue à thread do loop.</summary>
public sealed record LoadedCharacter(int Peer, Guid CharacterId, LoadOutcome Outcome, string Name, string ClassId, CharacterState? State, string? Error);

/// <summary>
/// Carrega e grava personagens fora da thread do loop (T-212). O loop só chama <see cref="BeginLoad"/>,
/// <see cref="TryTakeLoaded"/> e <see cref="Save"/>, que nunca esperam o banco.
/// <list type="bullet">
/// <item>Gravações passam por uma fila com um trabalhador só. Para cada personagem vale o estado mais novo: pedidos que chegam
/// enquanto um espera são juntados.</item>
/// <item>Falha do banco (exceção) é registrada e repetida depois de <c>retryDelay</c>, sem perder estado mais novo.
/// Conflito de versão ou personagem apagado não se resolve repetindo: registra erro e descarta.</item>
/// <item>Enquanto há estado não gravado de um personagem, carregá-lo devolve esse estado (quem sai e volta logo não vê o banco
/// velho).</item>
/// </list>
/// </summary>
public sealed class CharacterPersistence : IDisposable
{
    private readonly ICharacterStore _store;
    private readonly ILogger _log;
    private readonly TimeSpan _retryDelay;
    private readonly Lock _lock = new();

    // Última linha conhecida de cada personagem carregado (versão para a checagem de concorrência). Só o trabalhador altera.
    private readonly Dictionary<Guid, Character> _records = [];

    // Estado mais novo ainda não gravado (na fila ou sendo gravado).
    private readonly Dictionary<Guid, Pending> _pending = [];
    private readonly Queue<Guid> _queue = new();
    private readonly ConcurrentQueue<LoadedCharacter> _loaded = new();
    private readonly SemaphoreSlim _signal = new(0);
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _worker;
    private long _saved;
    private long _failures;

    public CharacterPersistence(ICharacterStore store, ILogger log, TimeSpan? retryDelay = null)
    {
        _store = store;
        _log = log;
        _retryDelay = retryDelay ?? TimeSpan.FromSeconds(5);
        _worker = Task.Run(WorkAsync);
    }

    /// <summary>Personagens com estado ainda não gravado.</summary>
    public int PendingSaves
    {
        get
        {
            lock (_lock)
            {
                return _pending.Count;
            }
        }
    }

    public long SavesCompleted => Interlocked.Read(ref _saved);

    public long SaveFailures => Interlocked.Read(ref _failures);

    /// <summary>Começa a carregar o personagem da conta; o resultado sai em <see cref="TryTakeLoaded"/>.</summary>
    public void BeginLoad(int peer, Guid characterId, Guid accountId) =>
        _ = Task.Run(async () => _loaded.Enqueue(await LoadAsync(peer, characterId, accountId)));

    public bool TryTakeLoaded(out LoadedCharacter result) => _loaded.TryDequeue(out result!);

    /// <summary>Pede a gravação do estado (só de personagem carregado por aqui). Não espera.</summary>
    public void Save(Guid characterId, CharacterState state, string reason)
    {
        lock (_lock)
        {
            if (!_records.ContainsKey(characterId))
            {
                _log.Warning("Gravação de {Character} ignorada: personagem não foi carregado", characterId);
                return;
            }

            var queued = _pending.TryGetValue(characterId, out var p) && p.Queued;
            _pending[characterId] = new Pending(state, reason, Queued: true);
            if (!queued)
            {
                _queue.Enqueue(characterId);
            }
        }

        _signal.Release();
    }

    /// <summary>Espera a fila esvaziar (parada do servidor). False se o prazo acabou com gravações pendentes.</summary>
    public async Task<bool> FlushAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (PendingSaves > 0)
        {
            if (DateTime.UtcNow >= deadline)
            {
                return false;
            }

            await Task.Delay(20);
        }

        return true;
    }

    public void Dispose()
    {
        _stop.Cancel();
        try
        {
            _worker.Wait(TimeSpan.FromSeconds(1));
        }
        catch (AggregateException)
        {
        }

        _stop.Dispose();
        _signal.Dispose();
    }

    private async Task<LoadedCharacter> LoadAsync(int peer, Guid id, Guid accountId)
    {
        lock (_lock)
        {
            if (_pending.TryGetValue(id, out var p) && _records.TryGetValue(id, out var known))
            {
                return known.AccountId == accountId
                    ? new LoadedCharacter(peer, id, LoadOutcome.Loaded, known.Name, known.ClassId, p.State, null)
                    : new LoadedCharacter(peer, id, LoadOutcome.NotFound, "", "", null, "personagem de outra conta");
            }
        }

        Character? c;
        try
        {
            c = await _store.LoadAsync(id, _stop.Token);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.Warning(ex, "Falha ao carregar o personagem {Character}", id);
            return new LoadedCharacter(peer, id, LoadOutcome.Failed, "", "", null, "falha ao ler o banco");
        }

        if (c is null || c.AccountId != accountId)
        {
            return new LoadedCharacter(peer, id, LoadOutcome.NotFound, "", "", null, c is null ? "personagem não existe" : "personagem de outra conta");
        }

        CharacterState state;
        try
        {
            state = CharacterDocument.Read(c.ProgressionVersion, c.Progression, c.Level, c.Experience);
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or InvalidDataException or OverflowException)
        {
            _log.Error("Progressão corrompida do personagem {Character} ({Name}): {Error}", id, c.Name, ex.Message);
            return new LoadedCharacter(peer, id, LoadOutcome.Failed, c.Name, c.ClassId, null, "progressão corrompida");
        }

        lock (_lock)
        {
            // Se nesse meio-tempo apareceu estado não gravado, ele é mais novo que o banco.
            if (_pending.TryGetValue(id, out var p) && _records.ContainsKey(id))
            {
                state = p.State;
            }
            else
            {
                _records[id] = c;
            }
        }

        return new LoadedCharacter(peer, id, LoadOutcome.Loaded, c.Name, c.ClassId, state, null);
    }

    private async Task WorkAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            try
            {
                await _signal.WaitAsync(_stop.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            while (TryNext(out var id, out var pending, out var record))
            {
                if (!await SaveOneAsync(id, pending, record))
                {
                    try
                    {
                        await Task.Delay(_retryDelay, _stop.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            }
        }
    }

    private bool TryNext(out Guid id, out Pending pending, out Character record)
    {
        lock (_lock)
        {
            while (_queue.TryDequeue(out id))
            {
                if (_pending.TryGetValue(id, out pending!) && _records.TryGetValue(id, out record!))
                {
                    _pending[id] = pending with { Queued = false };
                    return true;
                }
            }
        }

        pending = null!;
        record = null!;
        return false;
    }

    /// <summary>Grava um estado; false se falhou e vai ser repetido.</summary>
    private async Task<bool> SaveOneAsync(Guid id, Pending pending, Character record)
    {
        var state = pending.State;
        record.Level = state.Build.Level;
        record.Experience = state.Build.Experience;
        record.ProgressionVersion = CharacterDocument.Version;
        record.Progression = CharacterDocument.Write(state);

        SaveStatus status;
        try
        {
            status = await _store.SaveAsync(record, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _failures);
            _log.Warning("Falha ao gravar {Name} ({Reason}); repetindo em {Delay} s: {Error}", record.Name, pending.Reason, _retryDelay.TotalSeconds, ex.Message);
            lock (_lock)
            {
                var current = _pending[id];
                if (!current.Queued)
                {
                    _pending[id] = current with { Queued = true };
                    _queue.Enqueue(id);
                }
            }

            return false;
        }

        lock (_lock)
        {
            if (status == SaveStatus.Saved)
            {
                Interlocked.Increment(ref _saved);
                if (ReferenceEquals(_pending[id].State, state) && !_pending[id].Queued)
                {
                    _pending.Remove(id);
                }
            }
            else
            {
                // Outro processo gravou (conflito) ou o personagem foi apagado: repetir não resolve.
                Interlocked.Increment(ref _failures);
                _pending.Remove(id);
                _records.Remove(id);
            }
        }

        if (status == SaveStatus.Saved)
        {
            _log.Debug("{Name} gravado ({Reason}): nível {Level}", record.Name, pending.Reason, record.Level);
        }
        else
        {
            _log.Error("Gravação de {Name} ({Reason}) descartada: {Status}", record.Name, pending.Reason, status);
        }

        return true;
    }

    private sealed record Pending(CharacterState State, string Reason, bool Queued);
}
