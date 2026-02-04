using BankMore.ContaCorrente.Application.Handlers;
using BankMore.ContaCorrente.Application.Services;
using BankMore.ContaCorrente.Domain.Interfaces;
using BankMore.ContaCorrente.Infrastructure.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.Sqlite;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Dapper;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURAÇÃO DE PORTA ---
builder.WebHost.UseUrls("http://localhost:5000");

// --- 2. SERVIÇOS BÁSICOS ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BankMore Conta Corrente API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Ex: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            new string[] {}
        }
    });
});

// --- 3. BANCO DE DADOS ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=banco.db";

// --- 4. INJEÇÃO DE DEPENDÊNCIA ---
builder.Services.AddSingleton<IContaCorrenteRepository>(provider => new ContaCorrenteRepository(connectionString));
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CadastrarContaHandler).Assembly));

// --- 5. AUTENTICAÇÃO JWT ---
var secretKey = builder.Configuration["Jwt:Key"] ?? "UmaChaveMuitoSecretaEIgualParaTodasAsApis123";
var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.UseSecurityTokenValidators = true; 
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Environment.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

// --- 6. CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// --- 7. INICIALIZAÇÃO DO BANCO DE DADOS ---
// Inicialização do Banco (Mantendo as 4 tabelas do seu projeto)
using (var scope = app.Services.CreateScope())
{
    using var connection = new SqliteConnection(connectionString);
    connection.Open();
    
    // 1. Contas
    connection.Execute(@"CREATE TABLE IF NOT EXISTS contacorrente (idcontacorrente TEXT PRIMARY KEY, numero INTEGER NOT NULL UNIQUE, nome TEXT NOT NULL, cpf TEXT NOT NULL UNIQUE, senha TEXT NOT NULL, salt TEXT NOT NULL, ativo INTEGER NOT NULL, saldo REAL NOT NULL DEFAULT 0.0);");
    
    // 2. Movimentações (Extrato)
    connection.Execute(@"CREATE TABLE IF NOT EXISTS movimento (idmovimento TEXT PRIMARY KEY, idcontacorrente TEXT NOT NULL, numeroconta INTEGER NOT NULL, datamovimento TEXT NOT NULL, tipomovimento TEXT NOT NULL, valor REAL NOT NULL);");
    
    // 3. Transferências (Histórico de solicitações)
    connection.Execute(@"CREATE TABLE IF NOT EXISTS transferencia (id TEXT PRIMARY KEY, cpf_origem TEXT NOT NULL, cpf_destino TEXT NOT NULL, valor REAL NOT NULL, data TEXT NOT NULL, tipo TEXT NOT NULL, protocolo TEXT NOT NULL);");

    // 4. Tarifas (TED/TEF - Essencial para o Worker)
    connection.Execute(@"CREATE TABLE IF NOT EXISTS tarifa (id TEXT PRIMARY KEY, idcontacorrente TEXT NOT NULL, numeroconta INTEGER NOT NULL, valor REAL NOT NULL, dataprocessamento TEXT NOT NULL, tipotransferencia INTEGER NOT NULL DEFAULT 0);");
}

// --- 8. PIPELINE ---
app.UseCors("DefaultPolicy");
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BankMore Conta Corrente V1");
});

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
