using MediatR;
using System.Threading;
using System.Threading.Tasks;
using KafkaFlow;
using KafkaFlow.Producers;
using System;
using BankMore.Transferencia.Domain.Enums; // Adicionado para usar o Enum

namespace BankMore.Transferencia.Application.Handlers
{
    // Passo 1: Atualizado para incluir o Tipo da Transferência
    public record EfetuarTransferenciaCommand(string CpfOrigem, string CpfDestino, decimal Valor, TipoTransferencia Tipo) : IRequest<bool>;

    public class TransferenciaRealizadaMessage
    {
        public string CpfOrigem { get; set; } = string.Empty;
        public string CpfDestino { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public DateTime Data { get; set; }
        public decimal Taxa { get; set; }
        public int Tipo { get; set; } // Adicionado para o Worker saber o tipo
    }

    public class EfetuarTransferenciaHandler(IProducerAccessor producers) : IRequestHandler<EfetuarTransferenciaCommand, bool>
    {
        public async Task<bool> Handle(EfetuarTransferenciaCommand request, CancellationToken cancellationToken)
        {
            var producer = producers.GetProducer("transferencia-producer");

            // Passo 2: Usando a regra de negócio do Domínio para obter a taxa
            decimal valorTaxa = request.Tipo.ObterValorTarifa();

            var message = new TransferenciaRealizadaMessage 
            {
                CpfOrigem = request.CpfOrigem,
                CpfDestino = request.CpfDestino,
                Valor = request.Valor,
                Data = DateTime.Now,
                Taxa = valorTaxa, // Agora usa os 4.00m se for TED
                Tipo = (int)request.Tipo
            };

            // Envia para o Kafka processar o débito/crédito e a tarifa
            await producer.ProduceAsync(request.CpfOrigem, message);

            return true;
        }
    }
}
