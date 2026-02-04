using System.Data;
using System.Threading.Tasks;
using BankMore.ContaCorrente.Domain.Entities;
using BankMore.ContaCorrente.Domain.Interfaces;
using Dapper;
using Microsoft.Data.Sqlite;

namespace BankMore.ContaCorrente.Infrastructure.Repositories
{
    public class ContaCorrenteRepository : IContaCorrenteRepository
    {
        private readonly string _connectionString;

        public ContaCorrenteRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private IDbConnection CreateConnection() => new SqliteConnection(_connectionString);

        public async Task<Domain.Entities.ContaCorrente?> ObterPorCpf(string cpf)
        {
            using var db = CreateConnection();
            return await db.QueryFirstOrDefaultAsync<Domain.Entities.ContaCorrente>(
                "SELECT * FROM contacorrente WHERE cpf = @cpf", new { cpf });
        }

        public async Task<Domain.Entities.ContaCorrente?> ObterPorNumero(int numero)
        {
            using var db = CreateConnection();
            return await db.QueryFirstOrDefaultAsync<Domain.Entities.ContaCorrente>(
                "SELECT * FROM contacorrente WHERE numero = @numero", new { numero });
        }

        public async Task<Domain.Entities.ContaCorrente?> ObterPorId(string id)
        {
            using var db = CreateConnection();
            return await db.QueryFirstOrDefaultAsync<Domain.Entities.ContaCorrente>(
                "SELECT * FROM contacorrente WHERE idcontacorrente = @id", new { id });
        }

        public async Task<string> Cadastrar(Domain.Entities.ContaCorrente conta)
        {
            using var db = CreateConnection();
            var sql = @"INSERT INTO contacorrente (idcontacorrente, numero, nome, cpf, senha, salt, ativo) 
                        VALUES (@IdContaCorrente, @Numero, @Nome, @Cpf, @Senha, @Salt, @Ativo)";
            await db.ExecuteAsync(sql, conta);
            return conta.Numero.ToString();
        }

        public async Task Inativar(string id)
        {
            using var db = CreateConnection();
            await db.ExecuteAsync("UPDATE contacorrente SET ativo = 0 WHERE idcontacorrente = @id", new { id });
        }

       public async Task AdicionarMovimento(Movimento movimento)
        {
            using var db = CreateConnection();
            var sql = @"INSERT INTO movimento (idmovimento, idcontacorrente, numeroconta, datamovimento, tipomovimento, valor) 
                VALUES (@IdMovimento, @IdContaCorrente, @NumeroConta, @DataMovimento, @TipoMovimento, @Valor)";

         await db.ExecuteAsync(sql, movimento);
        }


        public async Task<decimal> ObterSaldo(string idContaCorrente)
        {
            using var db = CreateConnection();
            var sql = @"
                SELECT 
                    COALESCE(SUM(CASE WHEN tipomovimento = 'C' THEN valor ELSE 0 END), 0) - 
                    COALESCE(SUM(CASE WHEN tipomovimento = 'D' THEN valor ELSE 0 END), 0) 
                FROM movimento 
                WHERE idcontacorrente = @idContaCorrente";
            return await db.ExecuteScalarAsync<decimal>(sql, new { idContaCorrente });
        }

        public async Task<IEnumerable<Movimento>> ObterMovimentos(string idContaCorrente)
        {
            using var db = CreateConnection();
            return await db.QueryAsync<Movimento>(
                "SELECT * FROM movimento WHERE idcontacorrente = @idContaCorrente ORDER BY datamovimento DESC", new { idContaCorrente });
        }

        public async Task<IEnumerable<Tarifa>> ObterTarifas(int numeroConta)
        {
            using var db = CreateConnection();
            return await db.QueryAsync<Tarifa>(
                "SELECT * FROM tarifa WHERE numeroconta = @numeroConta ORDER BY dataprocessamento DESC", new { numeroConta });
        }

        public async Task<bool> ExisteChaveIdempotencia(string chave)
        {
            using var db = CreateConnection();
            return await db.ExecuteScalarAsync<bool>(
                "SELECT COUNT(1) FROM idempotencia WHERE chave_idempotencia = @chave", new { chave });
        }

        public async Task SalvarIdempotencia(Idempotencia idempotencia)
        {
            using var db = CreateConnection();
            var sql = @"INSERT INTO idempotencia (chave_idempotencia, requisicao, resultado, data_processamento) 
                        VALUES (@ChaveIdempotencia, @Requisicao, @Resultado, @DataProcessamento)";
            await db.ExecuteAsync(sql, idempotencia);
        }
    }
}
