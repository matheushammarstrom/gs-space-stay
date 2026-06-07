using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace SpaceStay.Tests.E2E;

// Testes E2E: sobem a API real (TestServer + MySQL) e verificam input e output dos
// endpoints via HTTP. Requer o MySQL no ar (Docker).
public class ApiE2ETests : IClassFixture<SpaceStayWebAppFactory>
{
    private readonly SpaceStayWebAppFactory _factory;
    public ApiE2ETests(SpaceStayWebAppFactory factory) => _factory = factory;

    // helpers
    private static async Task<(HttpStatusCode Code, JsonElement Body)> Send(
        HttpClient client, HttpMethod method, string url, object? body = null, string? token = null)
    {
        using var req = new HttpRequestMessage(method, url);
        if (body is not null) req.Content = JsonContent.Create(body);
        if (token is not null) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await client.SendAsync(req);
        var text = await res.Content.ReadAsStringAsync();
        JsonElement el = default;
        if (!string.IsNullOrWhiteSpace(text))
        {
            try { el = JsonDocument.Parse(text).RootElement.Clone(); } catch { /* corpo não-JSON */ }
        }
        return (res.StatusCode, el);
    }

    private async Task<(string Token, HttpClient Client)> RegisterGuestAsync()
    {
        var client = _factory.CreateClient();
        var email = $"e2e+{Guid.NewGuid():N}@example.com";
        var (code, body) = await Send(client, HttpMethod.Post, "/api/auth/register", new
        {
            name = "E2E Tester", email, password = "Senha@123", nationality = "Brasil", medicalClearance = true
        });
        Assert.Equal(HttpStatusCode.Created, code);
        return (body.GetProperty("token").GetString()!, client);
    }

    private static async Task<int> FindModuleIdByName(HttpClient client, string token, string name)
    {
        var (_, body) = await Send(client, HttpMethod.Get, "/api/modules?page=1&pageSize=50", token: token);
        foreach (var m in body.GetProperty("items").EnumerateArray())
            if (m.GetProperty("name").GetString() == name) return m.GetProperty("id").GetInt32();
        throw new Xunit.Sdk.XunitException($"Módulo '{name}' não encontrado no seed.");
    }

    private static async Task<int> FindCo2SensorId(HttpClient client, string token, int moduleId)
    {
        var (_, body) = await Send(client, HttpMethod.Get, $"/api/modules/{moduleId}/sensors", token: token);
        foreach (var s in body.EnumerateArray())
            if (s.GetProperty("type").GetString() == "co2") return s.GetProperty("id").GetInt32();
        throw new Xunit.Sdk.XunitException("Sensor de CO₂ não encontrado.");
    }

    // testes

    [Fact] // TC1
    public async Task Register_retorna_201_com_token()
    {
        var client = _factory.CreateClient();
        var email = $"e2e+{Guid.NewGuid():N}@example.com";
        var (code, body) = await Send(client, HttpMethod.Post, "/api/auth/register", new
        {
            name = "Novo Hóspede", email, password = "Senha@123"
        });

        Assert.Equal(HttpStatusCode.Created, code);
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("token").GetString()));
        Assert.Equal("guest", body.GetProperty("userType").GetString());
    }

    [Fact] // TC2
    public async Task Login_com_senha_errada_retorna_401()
    {
        var (_, client) = await RegisterGuestAsync();
        // tenta logar com a credencial de seed e senha errada
        var (code, _) = await Send(client, HttpMethod.Post, "/api/auth/login", new
        {
            email = "ana.costa@example.com", password = "senhaErrada"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, code);
    }

    [Fact]
    public async Task Modules_sem_token_retorna_401()
    {
        var client = _factory.CreateClient();
        var (code, _) = await Send(client, HttpMethod.Get, "/api/modules");
        Assert.Equal(HttpStatusCode.Unauthorized, code);
    }

    [Fact] // TC7 (RBAC)
    public async Task Guest_em_rota_de_equipe_retorna_403()
    {
        var (token, client) = await RegisterGuestAsync();
        var (code, _) = await Send(client, HttpMethod.Get, "/api/alerts?status=open", token: token);
        Assert.Equal(HttpStatusCode.Forbidden, code);
    }

    [Fact] // TC5
    public async Task Reading_dentro_da_faixa_nao_cria_alerta()
    {
        var (token, client) = await RegisterGuestAsync();
        var moduleId = await FindModuleIdByName(client, token, "Cupola Suite 3");
        var co2 = await FindCo2SensorId(client, token, moduleId);

        var (code, body) = await Send(client, HttpMethod.Post, "/api/readings", new { sensorId = co2, value = 600 });

        Assert.Equal(HttpStatusCode.Created, code);
        Assert.False(body.GetProperty("alertCreated").GetBoolean());
    }

    [Fact] // TC6: o momento de ouro do pitch
    public async Task Reading_critica_cria_alerta_critico()
    {
        var (token, client) = await RegisterGuestAsync();
        var moduleId = await FindModuleIdByName(client, token, "Cupola Suite 3");
        var co2 = await FindCo2SensorId(client, token, moduleId);

        var (code, body) = await Send(client, HttpMethod.Post, "/api/readings", new { sensorId = co2, value = 2150 });

        Assert.Equal(HttpStatusCode.Created, code);
        Assert.True(body.GetProperty("alertCreated").GetBoolean());
        Assert.Equal("critical", body.GetProperty("alert").GetProperty("severity").GetString());
    }

    [Fact] // TC4
    public async Task Booking_em_modulo_lotado_retorna_409()
    {
        var (token, client) = await RegisterGuestAsync();
        var cupola = await FindModuleIdByName(client, token, "Cupola Suite 3"); // lotado no seed
        var (code, _) = await Send(client, HttpMethod.Post, "/api/bookings", new
        {
            moduleId = cupola, checkIn = DateTime.UtcNow, checkOut = DateTime.UtcNow.AddDays(2)
        }, token);
        Assert.Equal(HttpStatusCode.Conflict, code);
    }

    [Fact] // TC3
    public async Task Booking_em_modulo_disponivel_retorna_201()
    {
        var (token, client) = await RegisterGuestAsync();
        var aurora = await FindModuleIdByName(client, token, "Aurora Suite 1"); // suíte disponível
        var (code, body) = await Send(client, HttpMethod.Post, "/api/bookings", new
        {
            moduleId = aurora, checkIn = DateTime.UtcNow, checkOut = DateTime.UtcNow.AddDays(2)
        }, token);

        Assert.Equal(HttpStatusCode.Created, code);
        Assert.Equal(aurora, body.GetProperty("moduleId").GetInt32());
    }
}
