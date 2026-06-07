# 🛰️ SpaceStay - Parte 2: API REST (ASP.NET Core + EF Core)

Backend RESTful do hotel orbital SpaceStay, em arquitetura em camadas
(Controller, Service e Repository) com EF Core Code-First sobre MySQL.

## Stack (dentro do conteúdo do curso)

- .NET 8 / ASP.NET Core Web API
- Entity Framework Core 8 + Pomelo.EntityFrameworkCore.MySql (provider MySQL)
- JWT (autenticação) e PBKDF2 (hash de senha)
- Swagger / OpenAPI (Swashbuckle) para documentação interativa
- Injeção de dependência nativa, DTOs, validação por data annotations

## Estrutura (camadas)

```
backend/
├── SpaceStay.Api/     # Controllers, Program.cs (DI/JWT/Swagger), middleware, seed, JWT service
├── SpaceStay.Core/    # Domínio (entidades, enums), DTOs, interfaces, serviços (regra de negócio)
├── SpaceStay.Infra/   # DbContext (EF Core), repositórios, migrations, hasher PBKDF2
└── test-api.sh        # teste de fumaça ponta a ponta via curl
```

Fluxo de uma requisição: o Controller (HTTP) chama o Service (regra de negócio), que usa
o Repository (acesso a dados via EF Core) sobre o MySQL.

## Cobertura do enunciado (Parte 2)

- [x] 16 endpoints funcionais com GET, POST, PUT e DELETE (o mínimo era 5).
- [x] Organização em camadas Controller, Service e Repository, com DI.
- [x] Swagger em `/swagger` (documentação da API).
- [x] Migrations do EF Core criam o schema da Parte 1.
- [x] `POST /api/readings` implementa a regra de limiar de alerta.
- [x] Status codes corretos (200/201/204/400/401/403/404/409), DTOs e validação.

---

## Como rodar

> Pré-requisitos: .NET 8 SDK e Docker (para o MySQL). O .NET foi instalado em
> `~/.dotnet`. Se `dotnet` não estiver no PATH, rode antes:
> ```bash
> export PATH="$HOME/.dotnet:$PATH:$HOME/.dotnet/tools"
> ```

```bash
# 1) Subir o MySQL (porta 3306)
docker run --name spacestay-db -e MYSQL_ROOT_PASSWORD=root -e MYSQL_DATABASE=spacestay \
  -p 3306:3306 -d mysql:8

# 2) Rodar a API (aplica migrations e popula o seed automaticamente)
cd backend
dotnet run --project SpaceStay.Api --urls "http://localhost:5080"

# 3) Abrir a documentação
#    Swagger UI:  http://localhost:5080/swagger
```

A connection string e o JWT ficam em `SpaceStay.Api/appsettings.json` (ajuste se
necessário). Ao iniciar, a API roda `Database.Migrate()` e, se o banco estiver vazio,
popula um cenário de demonstração (`DbSeeder`).

### Rodar os testes (curl)

```bash
cd backend
bash test-api.sh          # com a API no ar em http://localhost:5080
```

---

## Credenciais de teste (geradas pelo seed, já com hash)

| Tipo | E-mail | Senha | Perfil |
|---|---|---|---|
| Equipe | `helena.vasquez@spacestay.space` | `Admin@123` | admin |
| Equipe | `rafael.lima@spacestay.space` | `Engineer@123` | engineer |
| Equipe | `sofia.mendes@spacestay.space` | `Concierge@123` | concierge |
| Hóspede | `ana.costa@example.com` | `Guest@123` | guest |
| Hóspede | `kenji.tanaka@example.com` | `Guest@123` | guest |

Use `POST /api/auth/login` para obter o token e clique em **Authorize** no Swagger
para colá-lo.

---

## Endpoints

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| POST | `/api/auth/register` | público | Cadastro de hóspede (hash de senha) |
| POST | `/api/auth/login` | público | Login (hóspede ou equipe), devolve o JWT |
| GET | `/api/modules` | autenticado | Lista módulos (com alertas em aberto), paginado |
| GET | `/api/modules/{id}` | autenticado | Detalhe do módulo |
| GET | `/api/modules/{id}/sensors` | autenticado | Sensores e faixas seguras |
| GET | `/api/modules/{id}/readings` | autenticado | Última leitura de cada sensor |
| GET | `/api/bookings` | autenticado | Reservas (equipe: todas; hóspede: as suas) |
| GET | `/api/bookings/{id}` | autenticado | Detalhe da reserva |
| POST | `/api/bookings` | hóspede | Cria reserva (valida capacidade) |
| PUT | `/api/bookings/{id}` | autenticado | Altera ou cancela reserva |
| DELETE | `/api/bookings/{id}` | autenticado | Remove reserva |
| GET | `/api/alerts?status=` | equipe | Feed de alertas (filtro e paginação) |
| PUT | `/api/alerts/{id}/acknowledge` | equipe | Reconhece alerta (resolved_by) |
| POST | `/api/readings` | público* | Ingestão IoT, aplica a regra de limiar de alerta |
| GET | `/api/excursions` | autenticado | Lista excursões (vagas) |
| POST | `/api/excursions/{id}/book` | hóspede | Reserva excursão (valida capacidade) |

\* `POST /api/readings` é aberto para o simulador IoT (Parte 6). Em produção usaria
uma chave de device.

---

## Regra central: `POST /api/readings` (limiar de alerta)

No `ReadingService`: resolve o sensor e sua faixa `min_safe`/`max_safe`, persiste a
leitura e, se o valor estiver fora da faixa, define a gravidade (em `AlertRules`:
CO₂ acima de 2000 = crítico, acima de 1000 = aviso; O₂ e pressão fora da faixa =
crítico; os demais = aviso) e cria um alerta `open` com mensagem legível. A equipe vê
em `GET /api/alerts?status=open` e reconhece em `.../acknowledge`.

## Segurança (prévia da Parte 5)

- Senhas com PBKDF2 e salt (`Pbkdf2PasswordHasher`), nunca em texto puro.
- JWT com perfis; RBAC por `[Authorize(Roles=...)]` mais checagem no Service (o hóspede
  só acessa os próprios dados, com 403 caso contrário).
- Validação de entrada via data annotations (400 em entrada inválida).
- Proteção contra SQL injection: o EF Core usa consultas parametrizadas por padrão.
- O login não revela se o e-mail existe (mensagem genérica).

## Relação com a Parte 1 (banco)

O schema é a fonte única de verdade: a configuração Fluent do `AppDbContext` espelha o
`database/schema.sql` (ENUM nativo, tamanhos de VARCHAR, índices, UNIQUEs, CHECKs e
`ON DELETE`). Os tipos mapeiam de forma limpa: `INT` para `int`, `BIGINT` para `long`,
`DECIMAL(8,2)` para `decimal`, `BOOLEAN` para `bool` e `DATETIME` para `DateTime`.
