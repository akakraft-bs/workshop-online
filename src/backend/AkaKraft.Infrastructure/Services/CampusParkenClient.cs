using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AkaKraft.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AkaKraft.Infrastructure.Services;

/// <summary>
/// HTTP-Client für https://tu-braunschweig.campusparken.de.
/// Als Singleton registriert; hält einen HttpClient und einen Token-Cache pro Parkkonto.
/// </summary>
public class CampusParkenClient : ICampusParkenClient
{
    private readonly HttpClient _http;
    private readonly ILogger<CampusParkenClient> _logger;
    private readonly ConcurrentDictionary<Guid, (string Token, DateTime Expiry)> _tokens = new();
    private readonly ConcurrentDictionary<Guid, (List<string> Plates, DateTime Expiry)> _plateCache = new();
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromDays(6);
    private static readonly TimeSpan PlateCacheTtl = TimeSpan.FromMinutes(5);

    public CampusParkenClient(IConfiguration configuration, ILogger<CampusParkenClient> logger)
    {
        _logger = logger;
        var baseUrl = configuration["ParkPortal:BaseUrl"] ?? "https://tu-braunschweig.campusparken.de";
        _http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(20) };
    }

    public async Task<IReadOnlyList<string>> ListAsync(
        Guid accountId, string username, string password, bool allowCache = true, CancellationToken ct = default)
    {
        if (allowCache
            && _plateCache.TryGetValue(accountId, out var cached)
            && cached.Expiry > DateTime.UtcNow)
        {
            return cached.Plates.ToList();
        }

        using var resp = await SendWithAuthAsync(accountId, username, password,
            () => new HttpRequestMessage(HttpMethod.Get, "/sapi/identifications/lp/list?page=0&limit=50"), ct);

        var body = await resp.Content.ReadFromJsonAsync<ListResponse>(cancellationToken: ct);
        var plates = body?.Data?.Select(d => d.Code).Where(c => !string.IsNullOrWhiteSpace(c)).ToList() ?? [];
        _plateCache[accountId] = (plates.ToList(), DateTime.UtcNow + PlateCacheTtl);
        return plates;
    }

    public async Task ReplaceAsync(
        Guid accountId, string username, string password, IReadOnlyList<string> kennzeichen, CancellationToken ct = default)
    {
        var payload = new ReplaceRequest(
            kennzeichen.Select(k => new Identification("lp", k)).ToList());

        using var resp = await SendWithAuthAsync(accountId, username, password, () =>
        {
            var req = new HttpRequestMessage(HttpMethod.Put, "/sapi/identifications/lp")
            {
                Content = JsonContent.Create(payload),
            };
            return req;
        }, ct);

        _ = resp; // Erfolg reicht; Body ist "{}"

        // Cache mit dem gerade gesetzten Stand aktualisieren
        _plateCache[accountId] = (kennzeichen.ToList(), DateTime.UtcNow + PlateCacheTtl);
    }

    // -------------------------------------------------------------------------

    private async Task<HttpResponseMessage> SendWithAuthAsync(
        Guid accountId, string username, string password,
        Func<HttpRequestMessage> requestFactory, CancellationToken ct)
    {
        var token = await GetTokenAsync(accountId, username, password, forceRefresh: false, ct);

        var resp = await SendAsync(requestFactory(), token, ct);
        if (resp.StatusCode == HttpStatusCode.Unauthorized || resp.StatusCode == HttpStatusCode.Forbidden)
        {
            resp.Dispose();
            token = await GetTokenAsync(accountId, username, password, forceRefresh: true, ct);
            resp = await SendAsync(requestFactory(), token, ct);
        }

        if (!resp.IsSuccessStatusCode)
        {
            var status = (int)resp.StatusCode;
            var detail = await SafeReadAsync(resp, ct);
            resp.Dispose();
            throw new CampusParkenException(
                $"Portal-Anfrage fehlgeschlagen ({status}). {detail}".Trim());
        }

        return resp;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, string token, CancellationToken ct)
    {
        req.Headers.TryAddWithoutValidation("Cookie", $"dpca={token}");
        req.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
        return await _http.SendAsync(req, ct);
    }

    private async Task<string> GetTokenAsync(
        Guid accountId, string username, string password, bool forceRefresh, CancellationToken ct)
    {
        if (!forceRefresh
            && _tokens.TryGetValue(accountId, out var cached)
            && cached.Expiry > DateTime.UtcNow)
        {
            return cached.Token;
        }

        HttpResponseMessage resp;
        try
        {
            resp = await _http.PostAsJsonAsync("/auth/login", new LoginRequest(username, password), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Campus-Parken-Login (Konto {AccountId}) nicht erreichbar.", accountId);
            throw new CampusParkenException("Portal nicht erreichbar.");
        }

        if (!resp.IsSuccessStatusCode)
        {
            resp.Dispose();
            throw new CampusParkenException("Login fehlgeschlagen – bitte Zugangsdaten prüfen.");
        }

        LoginResponse? body;
        try
        {
            body = await resp.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: ct);
        }
        finally
        {
            resp.Dispose();
        }

        var token = body?.Token;
        if (string.IsNullOrWhiteSpace(token))
            throw new CampusParkenException("Login-Antwort ohne Token.");

        _tokens[accountId] = (token, DateTime.UtcNow + TokenLifetime);
        return token;
    }

    private static async Task<string> SafeReadAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        try { return (await resp.Content.ReadAsStringAsync(ct)).Trim(); }
        catch { return string.Empty; }
    }

    // -------------------------------------------------------------------------

    private sealed record LoginRequest(string Username, string Password);

    private sealed record LoginResponse(
        [property: JsonPropertyName("tokenType")] string? TokenType,
        [property: JsonPropertyName("token")] string? Token);

    private sealed record ListResponse(
        [property: JsonPropertyName("count")] int Count,
        [property: JsonPropertyName("data")] List<Identification>? Data);

    private sealed record Identification(
        [property: JsonPropertyName("source")] string Source,
        [property: JsonPropertyName("code")] string Code);

    private sealed record ReplaceRequest(
        [property: JsonPropertyName("identifications")] List<Identification> Identifications);
}
