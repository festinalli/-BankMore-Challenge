using BankMore.Transferencia.Application.Handlers;
using BankMore.Transferencia.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankMore.Transferencia.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TransferenciaController(IMediator mediator) : ControllerBase
    {
        

        // --- AQUI ESTAVA O ERRO: Faltava o ("efetuar") ---
        [HttpPost("efetuar")] 
        public async Task<IActionResult> Transferir([FromBody] TransferenciaRequest request)
        {
            var command = new EfetuarTransferenciaCommand(
                request.CpfOrigem, 
                request.CpfDestino, 
                request.Valor, 
                request.Tipo
            );

            var result = await mediator.Send(command);

            if (result)
            {
                // Retorno seguindo as melhores práticas para operações assíncronas
                return Accepted(new { 
                    mensagem = "Transferência aceita e enviada para processamento", 
                    tipo = request.Tipo.ToString(),
                    protocolo = Guid.NewGuid() 
                });
            }

            return BadRequest(new { mensagem = "Falha ao processar transferência" });
        }
    }

    /// <summary>
    /// DTO para entrada de dados de transferência.
    /// </summary>
    public record TransferenciaRequest(
        string CpfOrigem, 
        string CpfDestino, 
        decimal Valor, 
        TipoTransferencia Tipo
    );
}
