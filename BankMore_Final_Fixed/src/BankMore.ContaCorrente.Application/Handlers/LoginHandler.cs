using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BankMore.ContaCorrente.Domain.Interfaces;
using BankMore.ContaCorrente.Application.Services; // Garante o acesso ao IPasswordHasher
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BankMore.ContaCorrente.Application.Handlers
{
    public record LoginCommand(string Cpf, string Senha) : IRequest<LoginResult>;

    public class LoginResult
    {
        public bool Autenticado { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string IdContaCorrente { get; set; } = string.Empty;
        public int NumeroConta { get; set; }
        public string NomeTitular { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
    }

    public class LoginHandler : IRequestHandler<LoginCommand, LoginResult>
    {
        private readonly IContaCorrenteRepository _repository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IConfiguration _configuration;

        public LoginHandler(IContaCorrenteRepository repository, IPasswordHasher passwordHasher, IConfiguration configuration)
        {
            _repository = repository;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
        }

        public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var conta = await _repository.ObterPorCpf(request.Cpf);

            if (conta == null || !_passwordHasher.VerifyPassword(request.Senha, conta.Senha, conta.Salt))
            {
                return new LoginResult { Autenticado = false, Mensagem = "CPF ou Senha inválidos" };
            }

            var jwtSecretKey = _configuration["Jwt:Key"] ?? "UmaChaveMuitoSecretaEIgualParaTodasAsApis123";
            var key = Encoding.ASCII.GetBytes(jwtSecretKey);

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim("id", conta.IdContaCorrente),
            new Claim("numero", conta.Numero.ToString()),
            new Claim(ClaimTypes.Name, conta.Nome),
            new Claim("cpf", conta.Cpf),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        }),
                Expires = DateTime.UtcNow.AddHours(2), // Define a expiração explicitamente
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return new LoginResult
            {
                Autenticado = true,
                Token = tokenString,
                IdContaCorrente = conta.IdContaCorrente,
                NumeroConta = conta.Numero,
                NomeTitular = conta.Nome,
                Cpf = conta.Cpf,
                Mensagem = "Login realizado com sucesso"
            };
        }

    }
}
