# BankMore — Real-Time Fraud Detection

Sistema bancário event-driven com detecção de fraude em tempo real via PyFlink.
Toda transferência passa por um pipeline de scoring antes de ser efetivada — decisão sub-segundo, modelo de ML treinado, observabilidade ponta-a-ponta.

> **Sprint 1 — done & validado end-to-end (11/05/2026):** stack 100% Docker, fluxo
> `Frontend → Transferência → Kafka → Auto-approver → Worker → Postgres` funcionando.
> `make e2e` valida automaticamente. PyFlink real entra no Sprint 2.

## Como rodar (1 comando)

```bash
cd BankMore
make env            # cria .env (uma vez)
make up             # sobe tudo: postgres + redis + kafka + flink + APIs + worker + auto-approver
make e2e            # valida fluxo end-to-end (Alice → Bob, R$ 200 TED, valida saldos)
```

Acesse:
- **ContaCorrente API**: http://localhost:5000/swagger
- **Transferência API**: http://localhost:5001/swagger
- **Kafka UI**: http://localhost:8080
- **Flink UI**: http://localhost:8082
- **Schema Registry**: http://localhost:8085
- **Postgres**: `make psql`

## Stack

| Camada | Tech |
|---|---|
| Backend | .NET 8 LTS, KafkaFlow 4.1, Dapper |
| Mensageria | Apache Kafka 7.5 + Zookeeper + Schema Registry + Kafka UI |
| Streaming | Apache Flink 1.18 (placeholder p/ Sprint 2 — hoje `auto_approver.py`) |
| Banco | PostgreSQL 16 com `NUMERIC(18,2)` em tudo que é dinheiro |
| Cache | Redis 7 (feature store para o ML — Sprint 3) |
| Frontend | Angular 21 standalone (login + dashboard + extrato + transferência) |
| Observabilidade | Prometheus + Grafana + OTEL — Sprint 4 |

## Estrutura

```
contracts/avro/      Schemas Avro dos eventos Kafka (versionados)
infra/compose/       docker-compose.yml unificado
infra/db/init.sql    Schema canônico Postgres
src/                 Solução .NET (6 projetos + tests)
frontend/            Angular standalone
pyflink/             auto_approver.py (Sprint 1) → fraud_detection_job.py (Sprint 2)
ml/                  Treino e Flask (Sprint 3)
scripts/e2e.sh       Validação automática do fluxo
tests/               xUnit dos handlers
docs/                Walkthrough da demo
```

## Fluxo end-to-end (validado pelo `make e2e`)

```
[Angular :4200]
    │ POST /api/transferencia/efetuar (JWT)
    ▼
[Transferencia.Api :5001]
    │ valida claim cpf, gera id+correlationId
    │ produz JSON em transferencia.solicitada
    ▼
[Kafka :9092]
    │
    ▼
[auto_approver.py]  ← Sprint 1: copia direto. Sprint 2: PyFlink decide.
    │ produz em transferencia.aprovada
    ▼
[Kafka :9092]
    │
    ▼
[Tarifas.Worker]
    │ transação Postgres ATÔMICA:
    │   • idempotência por id da transferência
    │   • movimento D categoria=TRANSFERENCIA (origem, valor)
    │   • movimento D categoria=TARIFA       (origem, taxa)  ← saldo reflete taxa!
    │   • movimento C categoria=TRANSFERENCIA (destino, valor)
    │   • linha em tarifa (auditoria)
    ▼
[Postgres :5432]  → view saldo_conta retorna SUM(C - D) por conta
```

## O que melhorou vs. versões anteriores

| # | Bug antigo | Status |
|---|---|---|
| 1 | Worker em SQLite, API em Postgres | ✅ ambos Postgres |
| 2 | Frontend chamava endpoint errado | ✅ `TransferenciaService` → 5001 |
| 3 | CPF na URL/body | ✅ extraído do claim JWT |
| 4 | `[Authorize]` ausente | ✅ na classe inteira |
| 5 | `ValidateLifetime = false` | ✅ true em ambas APIs |
| 6 | JWT key hardcoded | ✅ env `JWT_KEY`, min 32 chars, falha se ausente |
| 7 | `REAL` para dinheiro | ✅ `NUMERIC(18,2)` |
| 8 | Tarifa não impacta saldo | ✅ vira movimento `D` categoria=TARIFA |
| 9 | KafkaFlow 3.0 vs 4.1 | ✅ tudo 4.1.0 |
| 10 | `net8` + `net9` misturado | ✅ tudo `net8.0` LTS |
| 11 | Cross-context refs (Conta ↔ Transferência) | ✅ removido |
| 12 | `docker-compose` com `services:` duplicado | ✅ um bloco só |
| 13 | Tópico único `transferencia-realizada` | ✅ `solicitada/aprovada/rejeitada/fraude.alerta` |
| 14 | `ObterExtratoHandler` abria Npgsql direto | ✅ usa `IContaCorrenteRepository` (Clean Arch) |
| 15 | Entity strings para datas (incompat. TIMESTAMPTZ) | ✅ `DateTime` UTC |
| 16 | Dockerfiles desatualizados | ✅ multi-stage SDK 8 + aspnet 8 + healthcheck |
| 17 | Sem `.env` (secrets no JSON) | ✅ `.env` + `.env.example` |
| 18 | Sem teste de integração | ✅ `scripts/e2e.sh` automatizado |
| 19 | `ObterExtratoHandlerTests` quebrado | ✅ 9/9 testes verdes |
| 20 | enum `TipoTransferencia` aceitava só int | ✅ `JsonStringEnumConverter` (PIX/TED/TEF) |

## O que ainda não está pronto (Sprints 2+)

- ❌ PyFlink real com event-time, watermark, state, Async I/O para ML
- ❌ Avro + Schema Registry — Sprint 1 usa JSON
- ❌ Modelo de ML treinado
- ❌ Persistência da `transferencia.status` (API ainda não escreve no Postgres)
- ❌ Validação de saldo (transferir mais que tem ainda passa)
- ❌ PasswordHasher PBKDF2 (hoje SHA-256)
- ❌ Validação de CPF com dígitos verificadores
- ❌ Painel ops `/ops/fraude` no frontend
- ❌ Prometheus + Grafana + Jaeger
- ❌ Frontend ainda não está no compose (rodar `cd frontend && ng serve` manual)

Mapeado em [`ROADMAP.md`](ROADMAP.md).

## Rodar local sem Docker (para dev/debug)

```bash
make build
# Em 4 terminais:
make run-contacorrente
make run-transferencia
make run-worker
make run-approver
make seed     # cria Alice e Bob
make e2e      # valida
```

## Testes

```bash
make test     # 9 testes xUnit, todos passando
```
