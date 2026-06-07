using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SpaceStay.Api.Data;
using SpaceStay.Api.Middleware;
using SpaceStay.Api.Security;
using SpaceStay.Core.Abstractions;
using SpaceStay.Core.Services;
using SpaceStay.Infra.Data;
using SpaceStay.Infra.Repositories;
using SpaceStay.Infra.Security;

var builder = WebApplication.CreateBuilder(args);

// Mantém os nomes das claims do JWT como definidos (sem o mapeamento legado).
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// Controllers + JSON (enums como string)
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// EF Core (MySQL via Pomelo)
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "server=127.0.0.1;port=3306;database=spacestay;user=root;password=root";
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 4, 0))));

// Injeção de dependência: repositórios
builder.Services.AddScoped<IGuestRepository, GuestRepository>();
builder.Services.AddScoped<IStaffRepository, StaffRepository>();
builder.Services.AddScoped<IModuleRepository, ModuleRepository>();
builder.Services.AddScoped<ISensorRepository, SensorRepository>();
builder.Services.AddScoped<ISensorReadingRepository, SensorReadingRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IAlertRepository, AlertRepository>();
builder.Services.AddScoped<IExcursionRepository, ExcursionRepository>();
builder.Services.AddScoped<IExcursionBookingRepository, ExcursionBookingRepository>();

// Injeção de dependência: serviços
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IModuleService, ModuleService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IReadingService, ReadingService>();
builder.Services.AddScoped<IExcursionService, ExcursionService>();

// Segurança: hash de senha e emissão de token
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();

// Autenticação JWT
var jwt = builder.Configuration.GetSection("Jwt");
var keyBytes = Encoding.UTF8.GetBytes(jwt["Key"] ?? "chave-de-desenvolvimento-troque-em-producao-please-32+");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            NameClaimType = JwtRegisteredClaimNames.Email,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

// CORS (libera o app a consumir a API)
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// Swagger / OpenAPI (com suporte a JWT)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SpaceStay API",
        Version = "v1",
        Description = "API do hotel orbital SpaceStay (Global Solution 2026/1, FIAP)."
    });

    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe o token JWT obtido em /api/auth/login.",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { scheme, Array.Empty<string>() } });

    var xml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xml)) c.IncludeXmlComments(xml);
});

var app = builder.Build();

// Migração e seed automáticos no startup (cria o schema da Parte 1 e popula a demo).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await DbSeeder.SeedAsync(db, hasher);
}

// Pipeline HTTP
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SpaceStay API v1"));
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Expõe Program para o WebApplicationFactory dos testes E2E (Parte 3).
public partial class Program { }
