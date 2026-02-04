using System;
using System.Threading;
using System.Threading.Tasks;

using BankMore.ContaCorrente.Domain.Entities;
using BankMore.ContaCorrente.Domain.Interfaces;
using MediatR;
using System.Text.Json;

namespace BankMore.ContaCorrente.Application.Handlers
{
    public record EfetuarMovimentacaoCommand(string RequestId, int NumeroConta, decimal Valor, string Tipo) : IRequest<bool>;

    public class EfetuarMovimentacaoHandler(IContaCorrenteRepository repository) : IRequestHandler<EfetuarMovimentacaoCommand, bool>
    {

        public async Task<bool> Handle(EfetuarMovimentacaoCommand request, CancellationToken cancellationToken)
        {
            // 1. Verificação de Idempotência (Requisito Sênior)
            if (await repository.ExisteChaveIdempotencia(request.RequestId))
            {
                return true; // Já processado, retorna sucesso sem duplicar
            }

            // 2. Validações de Negócio
            var conta = await repository.ObterPorNumero(request.NumeroConta);
            if (conta == null) throw new Exception("INVALID_ACCOUNT");
            if (!conta.Ativo) throw new Exception("INACTIVE_ACCOUNT");
            if (request.Valor <= 0) throw new Exception("INVALID_VALUE");
            if (request.Tipo != "C" && request.Tipo != "D") throw new Exception("INVALID_TYPE");

            // 3. Persistência do Movimento
            var movimento = new Movimento
            {
                IdContaCorrente = conta.IdContaCorrente,
                TipoMovimento = request.Tipo,
                Valor = request.Valor
            };

            await repository.AdicionarMovimento(movimento);

            // 4. Registrar Idempotência
            await repository.SalvarIdempotencia(new Idempotencia
            {
                ChaveIdempotencia = request.RequestId,
                Requisicao = JsonSerializer.Serialize(request),
                Resultado = "Success"
            });

            return true;
        }
    }
}
