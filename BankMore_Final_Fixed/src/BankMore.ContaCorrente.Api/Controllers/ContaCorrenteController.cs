using System;
using System.Threading.Tasks;
using BankMore.ContaCorrente.Application.Handlers;
using MediatR;
using BankMore.ContaCorrente.Application.Models; // Certifique-se de que este using está correto se for usado em outros lugares
using Microsoft.AspNetCore.Mvc;

namespace BankMore.ContaCorrente.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContaCorrenteController(IMediator mediator) : ControllerBase
    {
        [HttpPost("criar")]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public async Task<IActionResult> Cadastrar([FromBody] CadastrarContaCommand command)
        {
            try
            {
                var resultado = await mediator.Send(command);
                return Ok(resultado);
            }
            catch (ArgumentException ex) // Para validações de entrada como CPF inválido
            {
                // Retorna 400 Bad Request para indicar que a requisição é inválida
                return BadRequest(new { mensagem = ex.Message });
            }
            catch (InvalidOperationException ex) // Para "CPF já cadastrado"
            {
                // Retorna 409 Conflict para indicar que o recurso já existe
                return Conflict(new { mensagem = ex.Message });
            }
            catch (Exception ex) // Para qualquer outra exceção inesperada
            {
                // Loga a exceção para depuração. Em produção, use um logger de verdade.
                Console.WriteLine($"ERRO INESPERADO NO CADASTRO: {ex.Message}");
                // Retorna 500 Internal Server Error com uma mensagem genérica
                return StatusCode(500, new { mensagem = "Ocorreu um erro inesperado ao cadastrar a conta." });
            }
        }

        [HttpPost("login")]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            try
            {
                var resultado = await mediator.Send(command);

                if (resultado == null || !resultado.Autenticado)
                    return Unauthorized(new { mensagem = resultado?.Mensagem ?? "Falha na autenticação" });

                // Retorna o objeto completo (Token, NomeTitular, NumeroConta, Cpf)
                // O ASP.NET converterá para camelCase (ex: nomeTitular) no JSON
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [HttpGet("saldo/{cpf}")]
        public async Task<IActionResult> ObterSaldo(string cpf)
        {
            var resultado = await mediator.Send(new ObterSaldoQuery(cpf));
            return Ok(resultado);
        }

        [HttpGet("extrato/{cpf}")]
        public async Task<IActionResult> ObterExtrato(string cpf)
        {
            try
            {
                var resultado = await mediator.Send(new ObterExtratoQuery(cpf));
                return Ok(resultado ?? new ObterExtratoResponse());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERRO NO EXTRATO: {ex.Message}");
                return Ok(new ObterExtratoResponse());
            }
        }

        [HttpPost("movimentar")]
        public async Task<IActionResult> Movimentar([FromBody] EfetuarMovimentacaoCommand command)
        {
            try
            {
                await mediator.Send(command);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }
}
