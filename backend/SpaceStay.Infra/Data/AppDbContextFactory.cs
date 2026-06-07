using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SpaceStay.Infra.Data;

// Fábrica usada só em tempo de design pelo `dotnet ef` (criar/gerar migrations), sem
// subir a aplicação. A connection string vem de SPACESTAY_CONNECTION ou do padrão local.
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("SPACESTAY_CONNECTION")
                   ?? "server=127.0.0.1;port=3306;database=spacestay;user=root;password=root";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(conn, new MySqlServerVersion(new Version(8, 4, 0)))
            .Options;

        return new AppDbContext(options);
    }
}
