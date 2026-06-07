using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace SpaceStay.Tests.E2E;

// Sobe a API real em memória (TestServer) apontando para um banco de teste isolado
// (spacestay_test), derrubado e recriado a cada execução. O startup aplica as migrations
// e o seed. Requer o MySQL no ar (Docker).
public class SpaceStayWebAppFactory : WebApplicationFactory<Program>
{
    private const string ServerConn = "server=127.0.0.1;port=3306;user=root;password=root";
    public const string TestConnection = "server=127.0.0.1;port=3306;database=spacestay_test;user=root;password=root";

    public SpaceStayWebAppFactory()
    {
        // Banco de teste limpo a cada execução (determinístico).
        using var conn = new MySqlConnection(ServerConn);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DROP DATABASE IF EXISTS spacestay_test;";
        cmd.ExecuteNonQuery();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = TestConnection
            });
        });
    }
}
