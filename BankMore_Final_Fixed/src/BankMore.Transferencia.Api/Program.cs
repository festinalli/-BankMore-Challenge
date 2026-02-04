using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using KafkaFlow;
using KafkaFlow.Serializer;
using MediatR;
using BankMore.Transferencia.Application.Handlers;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5002");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BankMore Transferência API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Ex: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            new string[] {}
        }
    });
});

// --- CONFIGURAÇÃO DA CHAVE SECRETA ---
var jwtSecretKey = builder.Configuration["Jwt:Key"] ?? "UmaChaveMuitoSecretaEIgualParaTodasAsApis123";
var keyBytes = Encoding.ASCII.GetBytes(jwtSecretKey);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    // Força o uso do validador clássico para evitar erros de versão do .NET e garantir compatibilidade
    x.UseSecurityTokenValidators = true; 
    
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = false,
        ClockSkew = TimeSpan.Zero
    };
});

// --- 4. KAFKA ---
var kafkaBroker = builder.Configuration.GetValue<string>("Kafka:Broker") ?? "localhost:9092";

builder.Services.AddKafka(kafka => kafka
    .UseConsoleLog()
    .AddCluster(cluster => cluster
        .WithBrokers(new[] { kafkaBroker })
        .CreateTopicIfNotExists("transferencia-realizada", 1, 1)
        .AddProducer("transferencia-producer", producer => producer
            .DefaultTopic("transferencia-realizada")
            .AddMiddlewares(m => m.AddSerializer<NewtonsoftJsonSerializer>())
        )
    )
);

// --- 5. MEDIATR ---
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(EfetuarTransferenciaHandler).Assembly));

// --- 6. CORS ---
builder.Services.AddCors(options =>
    options.AddPolicy("DefaultPolicy", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// --- 7. INICIALIZAÇÃO DO KAFKA ---
var kafkaBus = app.Services.CreateKafkaBus();
await kafkaBus.StartAsync();

// --- 8. PIPELINE ---
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BankMore Transferência API v1");
});

app.UseCors("DefaultPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
