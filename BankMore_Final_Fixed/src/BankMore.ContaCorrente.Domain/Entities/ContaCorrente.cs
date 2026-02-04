using System;

namespace BankMore.ContaCorrente.Domain.Entities
{
    public class ContaCorrente
    {
        public string IdContaCorrente { get; set; } = Guid.NewGuid().ToString();
        public int Numero { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public string Salt { get; set; } = string.Empty;
        public bool Ativo { get; set; } = true;

        public ContaCorrente() { }

        public ContaCorrente(string nome, string cpf, string senha, int numero)
        {
            IdContaCorrente = Guid.NewGuid().ToString();
            Nome = nome;
            Cpf = cpf;
            Senha = senha;
            Salt = Guid.NewGuid().ToString().Substring(0, 8);
            Numero = numero;
            Ativo = true;
        }
    }
}
