# Guia de Execução - BankMore Solution

Este projeto foi corrigido para garantir que a transferência entre contas e a cobrança de tarifas funcionem corretamente via Kafka.

## 1. Pré-requisitos
Certifique-se de que o **Docker** está rodando e inicie o Kafka na raiz do projeto:
```bash
docker-compose up -d
```

## 2. Execução dos Serviços
Abra 3 terminais diferentes e execute os comandos abaixo na ordem:

### Terminal 1: Conta Corrente (Porta 5000)
```bash
cd "src/BankMore.ContaCorrente.Api"
dotnet run
```

### Terminal 2: Transferência API (Porta 5001)
```bash
cd "src/BankMore.Transferencia.Api"
dotnet run
```

### Terminal 3: Worker de Tarifas
```bash
cd "src/BankMore.Tarifas.Worker"
dotnet run
```

## 3. Como Testar
1. Acesse o Dashboard em `http://localhost:5000`.
2. Faça login com um dos CPFs existentes no banco (ex: `164240`).
3. Realize uma transferência para outro CPF (ex: `503791`).
4. **Observe o Terminal 3 (Worker):** Ele mostrará a mensagem `[WORKER] Sucesso: Banco de dados atualizado!`.
5. O saldo será atualizado automaticamente no Dashboard após alguns segundos.

## 4. Verificação via Banco de Dados (Opcional)
Para conferir o saldo diretamente no SQLite, rode:
```bash
sqlite3 "src/BankMore.ContaCorrente.Api/bankmore.db" "SELECT numeroconta, SUM(CASE WHEN tipomovimento = 'C' THEN valor ELSE -valor END) as saldo FROM movimento GROUP BY numeroconta;"
```

---
**Correções Realizadas:**
- Unificação do banco de dados entre os serviços.
- Sincronização de tópicos e serializadores Kafka.
- Inicialização correta do barramento Kafka (`StartAsync`).
- Configuração do Swagger com cadeado (JWT) na porta 5001.
- Lógica de transação atômica no Worker (Débito, Crédito e Tarifa).
