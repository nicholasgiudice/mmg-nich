using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Game.Server.Tests;

/// <summary>
/// Espera por trabalho que o servidor faz fora da thread do loop (carregar e gravar personagens em
/// <see cref="Persistence.CharacterPersistence"/>, que roda no thread pool).
/// <para>
/// Os testes do xUnit bloqueiam threads do pool (laços de tick, <c>GetAwaiter().GetResult()</c>). Com poucas threads
/// mínimas, o <c>Task.Run</c> do carregamento fica na fila até o pool criar outra thread, o que leva centenas de
/// milissegundos por thread e muito mais com a máquina ocupada (por exemplo, com <c>Game.Design.Tests</c> rodando ao
/// mesmo tempo no <c>./scripts/check.sh</c> completo). Por isso o assembly sobe o mínimo do pool e as esperas usam um
/// prazo longo, que só serve para um teste travado não ficar parado para sempre.
/// </para>
/// </summary>
internal static class BackgroundWork
{
    /// <summary>Prazo de segurança para carregamento e gravação; não é uma medida de desempenho.</summary>
    public static readonly TimeSpan Timeout = TimeSpan.FromMinutes(2);

    [ModuleInitializer]
    internal static void RaiseThreadPoolMinimum()
    {
        ThreadPool.GetMinThreads(out var workers, out var io);
        var wanted = Math.Max(64, Environment.ProcessorCount * 4);
        ThreadPool.SetMinThreads(Math.Max(workers, wanted), io);
    }

    /// <summary>Chama <paramref name="tick"/> até <paramref name="done"/> valer, cedendo a CPU entre os ticks.</summary>
    public static void TickUntil(Func<bool> done, Action tick, string what)
    {
        var watch = Stopwatch.StartNew();
        while (!done())
        {
            Assert.True(watch.Elapsed < Timeout, $"tempo esgotado esperando {what}");
            tick();
            Thread.Sleep(1);
        }
    }
}
