# 🧪 SpaceStay - Parte 3: Plano de Testes

Testes automatizados em xUnit (.NET 8), divididos em unitários (regra de negócio
isolada, sem banco) e E2E (sobem a API real e verificam input e output via HTTP).
Total atual: 46 testes, todos passando.

## Como rodar

> Pré-requisito: `export PATH="$HOME/.dotnet:$PATH:$HOME/.dotnet/tools"`.
> Os testes E2E exigem o MySQL no ar (Docker); os unitários não.

```bash
# subir o MySQL (se ainda não estiver rodando), necessário só para os E2E
docker start spacestay-db   # ou o docker run do backend/README.md

cd /Users/matheus/Workspace/fiap/gs
dotnet test tests/SpaceStay.Tests/SpaceStay.Tests.csproj

# só os unitários (não precisam de banco):
dotnet test tests/SpaceStay.Tests/SpaceStay.Tests.csproj --filter "FullyQualifiedName~Unit"
```

A evidência da última execução está em [`evidence/test-run.txt`](./evidence/test-run.txt)
(e o `.trx` ao lado).

## Estrutura

```
tests/SpaceStay.Tests/
├── Unit/
│   ├── AlertRulesTests.cs           # regra de limiar de alerta (valor-limite e equivalência)
│   ├── Pbkdf2PasswordHasherTests.cs # hashing de senha
│   ├── AuthServiceTests.cs          # cadastro e login com hash
│   ├── BookingServiceTests.cs       # capacidade, disponibilidade e RBAC
│   ├── ReadingServiceTests.cs       # ingestão IoT (a regra central)
│   └── Fakes/Fakes.cs               # repositórios fake em memória (test doubles)
└── E2E/
    ├── SpaceStayWebAppFactory.cs    # sobe a API real (TestServer) e o banco de teste
    └── ApiE2ETests.cs               # checa input e output dos endpoints via HTTP
```

---

## Plano de testes (cenário, entrada, saída esperada, status)

| # | Cenário | Entrada | Saída esperada | Status |
|---|---------|---------|----------------|--------|
| TC1 | Cadastro de hóspede com sucesso | nome/e-mail/senha válidos | `201`, senha armazenada com hash (não texto puro) | Automatizado |
| TC2 | Login com senha errada | e-mail correto, senha errada | `401`, sem token | Automatizado |
| TC3 | Reserva em módulo disponível | hóspede válido, módulo com vaga, datas | `201`, reserva criada | Automatizado |
| TC4 | Reserva em módulo lotado | módulo na capacidade máxima | `409 Conflict`, reserva rejeitada | Automatizado |
| TC5 | Leitura dentro da faixa, sem alerta | CO₂ = 600 ppm (seguro) | `201`, nenhum alerta criado | Automatizado |
| TC6 | Leitura fora da faixa gera alerta | CO₂ = 2150 ppm (acima de 2000) | `201`, alerta crítico (`status=open`) | Automatizado |
| TC7 | Hóspede não acessa dados de outros nem rota de equipe | token de hóspede | `403 Forbidden` | Automatizado |
| TC8 | Valor-limite: CO₂ exatamente no máximo | CO₂ = 1000 (igual ao limite) | Comportamento definido: seguro (sem alerta) | Automatizado |

> Cobertura extra automatizada (além do mínimo): validação de DTO (`400`), acesso sem
> token (`401`), módulo em manutenção (`409`), excursão duplicada (`409`), CO₂ entre
> 1000 e 2000 = aviso, O₂ e pressão fora da faixa = crítico, salt aleatório no hash,
> e o fluxo E2E completo (cadastro, login, módulos, reserva, leitura e alerta).

### Técnicas de teste aplicadas (exigidas pelo enunciado)

- Análise de valor-limite (boundary): em torno do limite de CO₂, com `1000` (seguro),
  `1001` (aviso), `2000` (aviso) e `2001` (crítico); e nos limites de O₂ (`19,5` seguro,
  `19,4` crítico). Ver `AlertRulesTests`.
- Classes de equivalência: uma representante de cada faixa (dentro, abaixo, acima) por
  tipo de sensor.
- Test doubles: repositórios *fake* em memória isolam a regra de negócio dos serviços
  nos testes unitários (sem dependência de banco).

---

## Mapa: caso de teste para teste automatizado

| Caso | Teste(s) |
|---|---|
| TC1 | `AuthServiceTests.RegisterGuest_armazena_senha_hasheada...`, `ApiE2ETests.Register_retorna_201_com_token` |
| TC2 | `AuthServiceTests.Login_com_senha_errada...`, `ApiE2ETests.Login_com_senha_errada_retorna_401` |
| TC3 | `BookingServiceTests.Create_em_modulo_disponivel...`, `ApiE2ETests.Booking_em_modulo_disponivel_retorna_201` |
| TC4 | `BookingServiceTests.Create_em_modulo_lotado...`, `ApiE2ETests.Booking_em_modulo_lotado_retorna_409` |
| TC5 | `ReadingServiceTests.Ingest_leitura_segura...`, `ApiE2ETests.Reading_dentro_da_faixa...` |
| TC6 | `ReadingServiceTests.Ingest_leitura_critica...`, `ApiE2ETests.Reading_critica_cria_alerta_critico` |
| TC7 | `BookingServiceTests.GetById_de_outro_hospede...`, `ApiE2ETests.Guest_em_rota_de_equipe_retorna_403` |
| TC8 | `ReadingServiceTests.Ingest_no_limite_maximo_e_seguro`, `AlertRulesTests` (caso `value:1000`) |

> Também existe um teste de fumaça por curl em `backend/test-api.sh` (21 casos), útil
> para demonstração manual e como evidência adicional.
