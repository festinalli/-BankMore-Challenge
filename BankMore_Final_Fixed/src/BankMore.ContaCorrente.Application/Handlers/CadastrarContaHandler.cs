using System;
using System.Threading;
using System.Threading.Tasks;
using BankMore.ContaCorrente.Domain.Entities;
using BankMore.ContaCorrente.Domain.Interfaces;
using MediatR;
using BankMore.ContaCorrente.Application.Services;

namespace BankMore.ContaCorrente.Application.Handlers
{
    public record CadastrarContaCommand(string Nome, string Cpf, string Senha, decimal SaldoInicial) : IRequest<CadastrarContaResponse>;

    public class CadastrarContaResponse
    {
        public string IdContaCorrente { get; set; } = string.Empty;
        public int NumeroConta { get; set; }
        public string NomeTitular { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
    }

    public class CadastrarContaHandler(IContaCorrenteRepository repository, IPasswordHasher passwordHasher) : IRequestHandler<CadastrarContaCommand, CadastrarContaResponse>
    {
        public async Task<CadastrarContaResponse> Handle(CadastrarContaCommand request, CancellationToken cancellationToken)
        {
            // Validação básica de CPF
            if (string.IsNullOrEmpty(request.Cpf) || request.Cpf.Length != 11)
                throw new ArgumentException("INVALID_DOCUMENT");

            // Verifica se já existe (Evita o crash 500 se tratar na Controller, mas aqui lança erro)
            var contaExistente = await repository.ObterPorCpf(request.Cpf);
            if (contaExistente != null)
                throw new InvalidOperationException("CPF já cadastrado");

            var novaConta = new BankMore.ContaCorrente.Domain.Entities.ContaCorrente();
            novaConta.IdContaCorrente = Guid.NewGuid().ToString();
            novaConta.Cpf = request.Cpf;
            novaConta.Nome = request.Nome ?? "Cliente Sem Nome";
            novaConta.Salt = Guid.NewGuid().ToString().Substring(0, 8);
            novaConta.Senha = passwordHasher.HashPassword(request.Senha, novaConta.Salt); 
            novaConta.Numero = new Random().Next(100000, 999999);
            novaConta.Ativo = true; 

            await repository.Cadastrar(novaConta);

            if (request.SaldoInicial > 0)
            {
                var movimento = new Movimento
                {
                    IdMovimento = Guid.NewGuid().ToString(),
                    IdContaCorrente = novaConta.IdContaCorrente,
                    NumeroConta = novaConta.Numero,
                    // CORREÇÃO AQUI: Usar formato ISO (yyyy-MM-dd) para o banco entender e ordenar certo
                    DataMovimento = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    TipoMovimento = "C",
                    Valor = request.SaldoInicial
                };

                await repository.AdicionarMovimento(movimento);
            }

            return new CadastrarContaResponse
            {
                IdContaCorrente = novaConta.IdContaCorrente,
                NumeroConta = novaConta.Numero,
                NomeTitular = novaConta.Nome,
                Cpf = novaConta.Cpf
            };
        }
    }
}
