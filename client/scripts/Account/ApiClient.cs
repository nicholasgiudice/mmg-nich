using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MmoRpgClient.Account;

/// <summary>Personagem da conta como a API devolve (T-211).</summary>
public sealed record CharacterSummary(Guid Id, string Name, string ClassId, int Level, string ZoneId);

/// <summary>Token de jogo de <c>POST /characters/{id}/play</c>: vale 60 s e uma vez só. <c>Address</c> vazio = a API não sabe.</summary>
public sealed record PlayTicket(string Ticket, DateTimeOffset ExpiresAt, string Server, string? Address);

/// <summary>Erro da API ou da rede, já com a mensagem para o jogador. <c>Code</c> é o código estável do ProblemDetails.</summary>
public sealed class ApiException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

/// <summary>
/// Cliente da <c>Game.Api</c> (T-220). O token de sessão fica só aqui, em memória (nunca em disco), e vai em
/// <c>Authorization: Bearer</c>. Senha não é guardada. As respostas de erro viram <see cref="ApiException"/>.
/// </summary>
public sealed class ApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private string? _session;

    public ApiClient(string baseUrl)
    {
        BaseUrl = baseUrl.TrimEnd('/');
        _http = new HttpClient { BaseAddress = new Uri(BaseUrl + "/"), Timeout = TimeSpan.FromSeconds(10) };
    }

    public string BaseUrl { get; }

    public bool LoggedIn => _session is not null;

    /// <summary>E-mail da sessão atual (para mostrar na tela).</summary>
    public string? Email { get; private set; }

    public async Task Register(string email, string password)
    {
        using var response = await Send(HttpMethod.Post, "accounts", new { email, password }, auth: false);
    }

    public async Task Login(string email, string password)
    {
        Logout();
        using var response = await Send(HttpMethod.Post, "sessions", new { email, password }, auth: false);
        var session = await Read<SessionResponse>(response);
        _session = session.Token;
        Email = email.Trim();
    }

    public void Logout()
    {
        _session = null;
        Email = null;
    }

    public async Task<IReadOnlyList<CharacterSummary>> Characters()
    {
        using var response = await Send(HttpMethod.Get, "characters", null);
        return await Read<List<CharacterSummary>>(response);
    }

    public async Task<CharacterSummary> Create(string name, string classId)
    {
        using var response = await Send(HttpMethod.Post, "characters", new { name, classId });
        return await Read<CharacterSummary>(response);
    }

    public async Task Delete(Guid id)
    {
        using var response = await Send(HttpMethod.Delete, $"characters/{id}", null);
    }

    public async Task<PlayTicket> Play(Guid id)
    {
        using var response = await Send(HttpMethod.Post, $"characters/{id}/play", null);
        return await Read<PlayTicket>(response);
    }

    private async Task<HttpResponseMessage> Send(HttpMethod method, string path, object? body, bool auth = true)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            // Corpo com tamanho conhecido (sem "chunked"): funciona com qualquer proxy na frente da API.
            request.Content = new StringContent(JsonSerializer.Serialize(body, Json), System.Text.Encoding.UTF8, "application/json");
        }

        if (auth)
        {
            if (_session is null)
            {
                throw new ApiException("not_logged_in", "Entre na sua conta primeiro.");
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session);
        }

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new ApiException("offline", $"Servidor de contas fora do ar ou inacessível ({BaseUrl}).");
        }

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        using (response)
        {
            throw await ErrorFrom(response);
        }
    }

    private async Task<ApiException> ErrorFrom(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized && _session is not null)
        {
            Logout();
            return new ApiException("session_expired", "Sua sessão expirou. Entre de novo.");
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return new ApiException("rate_limited", "Muitas tentativas. Espere um minuto e tente de novo.");
        }

        Problem? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<Problem>(Json);
        }
        catch (JsonException)
        {
        }

        var code = problem?.Code ?? $"http_{(int)response.StatusCode}";
        return new ApiException(code, Message(code) ?? problem?.Title ?? $"Erro {(int)response.StatusCode} do servidor de contas.");
    }

    /// <summary>Mensagens do cliente para os códigos da API; códigos novos usam o <c>title</c> que a API manda.</summary>
    private static string? Message(string code) => code switch
    {
        "invalid_credentials" => "E-mail ou senha incorretos.",
        "email_taken" => "Já existe uma conta com esse e-mail.",
        "name_taken" => "Esse nome já está em uso. Escolha outro.",
        "name_invalid" => "Nome inválido: de 3 a 16 letras ou números, começando por letra.",
        "class_invalid" => "Classe inválida.",
        "character_limit" => "Você já tem o máximo de personagens (5). Apague um para criar outro.",
        "character_not_found" => "Personagem não encontrado. Atualize a lista.",
        _ => null,
    };

    private static async Task<T> Read<T>(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<T>(Json)
        ?? throw new ApiException("bad_response", "Resposta vazia do servidor de contas.");

    private sealed record SessionResponse(string Token, DateTimeOffset ExpiresAt);

    private sealed record Problem(string? Title, [property: JsonPropertyName("code")] string? Code);
}
