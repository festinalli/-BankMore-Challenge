using BankMore.Tarifas.Worker.Handlers;
using KafkaFlow;
using KafkaFlow.Serializer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

var builder = Host.CreateApplicationBuilder(args);

// ==================================================================================
// 1. CONFIGURAÇÃO DO BANCO DE DADOS E TABELAS (SQLITE)
// ==================================================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

using (var connection = new SqliteConnection(connectionString))
{
    connection.Open();
    var sql = @"
        CREATE TABLE IF NOT EXISTS contacorrente (
            idcontacorrente TEXT PRIMARY KEY,
            numero INTEGER NOT NULL UNIQUE,
            nome TEXT NOT NULL,
            cpf TEXT NOT NULL UNIQUE,
            senha TEXT NOT NULL,
            salt TEXT NOT NULL,
            ativo INTEGER NOT NULL,
            saldo REAL NOT NULL DEFAULT 0.0
        );

        CREATE TABLE IF NOT EXISTS movimento (
            idmovimento TEXT PRIMARY KEY,
            idcontacorrente TEXT NOT NULL, 
            numeroconta INTEGER NOT NULL,
            datamovimento TEXT NOT NULL,
            tipomovimento TEXT NOT NULL, 
            valor REAL NOT NULL
        );

        CREATE TABLE IF NOT EXISTS tarifa (
            id TEXT PRIMARY KEY,
            idcontacorrente TEXT NOT NULL,
            numeroconta INTEGER NOT NULL,
            valor REAL NOT NULL,
            dataprocessamento TEXT NOT NULL,
            tipotransferencia INTEGER NOT NULL DEFAULT 0
        );";

    using var command = new SqliteCommand(sql, connection);
    command.ExecuteNonQuery();
}

var kafkaBroker = builder.Configuration.GetValue<string>("Kafka:Broker") ?? "localhost:9092";

builder.Services.AddKafka(kafka => kafka
    .UseConsoleLog()
    .AddCluster(cluster => cluster
        .WithBrokers(new string[] { kafkaBroker })
        .AddConsumer(consumer => consumer
            .Topic("transferencia-realizada")
            .WithGroupId("tarifa-group")
            .WithBufferSize(100)
            .WithWorkersCount(1)
            .WithAutoOffsetReset(AutoOffsetReset.Earliest)
            .AddMiddlewares(middlewares => middlewares
                .AddSingleTypeDeserializer<TransferenciaRealizadaMessage, NewtonsoftJsonDeserializer>()
                .AddTypedHandlers(handlers => handlers
                    .AddHandler<TarifaConsumer>())
            )
        )
    )
);

var host = builder.Build();

var kafkaBus = host.Services.CreateKafkaBus();
await kafkaBus.StartAsync();

await host.RunAsync();
