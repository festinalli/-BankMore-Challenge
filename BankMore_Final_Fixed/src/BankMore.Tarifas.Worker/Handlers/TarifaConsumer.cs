using KafkaFlow;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;

namespace BankMore.Tarifas.Worker.Handlers
{
    public class TarifaConsumer(IConfiguration configuration, ILogger<TarifaConsumer> logger)
        : IMessageHandler<TransferenciaRealizadaMessage>
    {
        public async Task Handle(IMessageContext context, TransferenciaRealizadaMessage message)
        {
            try
            {
                using var connection = new SqliteConnection(configuration.GetConnectionString("DefaultConnection"));
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    // 1. DÉBITO TOTAL NA ORIGEM (Valor da Transferência + Taxa do TED/TEF)
                    var totalDebito = message.Valor + message.Taxa;
                    await connection.ExecuteAsync(
                        "UPDATE contacorrente SET saldo = saldo - @Total WHERE cpf = @Cpf",
                        new { Total = totalDebito, Cpf = message.CpfOrigem }, transaction);

                    // 2. CRÉDITO NO DESTINO (Apenas o Valor nominal)
                    await connection.ExecuteAsync(
                        "UPDATE contacorrente SET saldo = saldo + @Valor WHERE cpf = @Cpf",
                        new { Valor = message.Valor, Cpf = message.CpfDestino }, transaction);

                    // 3. REGISTRO DA TARIFA (TED/TEF)
                    // Importante: Verifique se a tabela 'tarifa' tem a coluna 'idcontacorrente'
                    // 3. REGISTRO DA TARIFA (TED/TEF) - Forma Explícita e Segura
                    if (message.Taxa > 0)
                    {
                        // Buscamos os dados da conta primeiro para garantir a integridade
                        var contaOrigem = await connection.QueryFirstOrDefaultAsync(
                            "SELECT idcontacorrente, numero FROM contacorrente WHERE cpf = @Cpf",
                            new { Cpf = message.CpfOrigem }, transaction);

                        if (contaOrigem != null)
                        {
                            await connection.ExecuteAsync(
                                @"INSERT INTO tarifa (id, idcontacorrente, numeroconta, valor, dataprocessamento, tipotransferencia) 
              VALUES (@Id, @IdConta, @NumConta, @Valor, @Data, @Tipo)",
                                new
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    IdConta = contaOrigem.idcontacorrente,
                                    NumConta = contaOrigem.numero,
                                    Valor = message.Taxa,
                                    Data = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                    Tipo = message.Tipo
                                }, transaction);
                        }
                        else
                        {
                            logger.LogWarning("Atenção: Conta de origem não encontrada para gravar tarifa. CPF: {Cpf}", message.CpfOrigem);
                        }
                    }


                    // 4. REGISTRO DOS MOVIMENTOS PARA O EXTRATO
                    await connection.ExecuteAsync(
                        @"INSERT INTO movimento (idmovimento, idcontacorrente, numeroconta, datamovimento, tipomovimento, valor) 
                  SELECT @Id, idcontacorrente, numero, @Data, 'D', @Valor FROM contacorrente WHERE cpf = @Cpf",
                        new { Id = Guid.NewGuid().ToString(), Data = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Valor = message.Valor, Cpf = message.CpfOrigem }, transaction);

                    await connection.ExecuteAsync(
                        @"INSERT INTO movimento (idmovimento, idcontacorrente, numeroconta, datamovimento, tipomovimento, valor) 
                  SELECT @Id, idcontacorrente, numero, @Data, 'C', @Valor FROM contacorrente WHERE cpf = @Cpf",
                        new { Id = Guid.NewGuid().ToString(), Data = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Valor = message.Valor, Cpf = message.CpfDestino }, transaction);

                    await transaction.CommitAsync();
                    logger.LogInformation("Sucesso: Transferência Tipo {Tipo} de R$ {Valor} (Taxa: {Taxa})", message.Tipo, message.Valor, message.Taxa);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    logger.LogError(ex, "Erro ao processar TED/TEF: {Msg}", ex.Message);
                    throw;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro crítico no Worker");
                throw;
            }
        }

    }

    public class TransferenciaRealizadaMessage
    {
        public string CpfOrigem { get; set; } = string.Empty;
        public string CpfDestino { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public decimal Taxa { get; set; }
        public int Tipo { get; set; }
    }
}
