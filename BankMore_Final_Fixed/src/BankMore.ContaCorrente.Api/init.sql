CREATE TABLE IF NOT EXISTS contacorrente (
    idcontacorrente TEXT PRIMARY KEY,
    numero INTEGER NOT NULL UNIQUE,
    nome TEXT NOT NULL,
    cpf TEXT NOT NULL UNIQUE,
    senha TEXT NOT NULL,
    salt TEXT NOT NULL,
    ativo INTEGER NOT NULL,
    saldo REAL NOT NULL DEFAULT 0.0
);

CREATE TABLE IF NOT EXISTS movimento (
    idmovimento TEXT PRIMARY KEY,
    numeroconta INTEGER NOT NULL,
    datamovimento TEXT NOT NULL,
    tipo TEXT NOT NULL,
    valor REAL NOT NULL
);

CREATE TABLE IF NOT EXISTS idempotencia (
    requestId TEXT PRIMARY KEY,
    dataProcessamento TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS tarifa (
    id TEXT PRIMARY KEY,
    numeroconta INTEGER NOT NULL,
    valor REAL NOT NULL,
    dataprocessamento TEXT NOT NULL
);
