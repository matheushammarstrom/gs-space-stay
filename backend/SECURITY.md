# 🔐 Parte 5: Segurança (SpaceStay)

Documento da camada de segurança da API REST (Parte 2). Tudo já está implementado e
testado; este arquivo descreve o que protege o sistema e onde está no código, para o
relatório PDF.

## O que o enunciado pede e como atendemos

| Exigência | Como atendemos | Onde (arquivo) |
|---|---|---|
| Login com senha criptografada (hash) | PBKDF2/HMAC-SHA256 com salt por usuário | `SpaceStay.Infra/Security/Pbkdf2PasswordHasher.cs` |
| Controle de acesso com permissões diferentes | JWT + RBAC (endpoint e posse do dado) | `Controllers/*`, `Core/Services/*`, `Program.cs` |
| Pelo menos 2 práticas adicionais (aplicamos 3) | Validação de entrada, anti-SQLi, token e falha segura | ver seção 3 |

> Escolhemos 3 práticas adicionais (o mínimo é 2). Recursos extras de defesa em
> profundidade (comparação em tempo constante, não vazar *stack trace*) entram como
> detalhe das práticas acima, sem inflar a lista além das 3 principais.

---

## 1) Login com senha criptografada (hash)

A senha nunca é armazenada em texto puro: guardamos apenas o *hash* nas tabelas
`guests` e `staff` (coluna `password_hash`).

- Algoritmo: PBKDF2 com HMAC-SHA256, 100.000 iterações (custo contra força bruta),
  salt aleatório de 128 bits por usuário e chave derivada de 256 bits.
- Formato persistido: `pbkdf2$<iterações>$<saltBase64>$<hashBase64>`. O salt viaja junto,
  então cada usuário tem hash único mesmo com senhas iguais (defesa contra *rainbow tables*).
- O cadastro faz o hash na entrada (`AuthService.cs:26`); o login verifica o hash
  (`AuthService.cs:45` e `:53`).
- Defesa em profundidade: a verificação usa comparação em tempo constante
  (`CryptographicOperations.FixedTimeEquals`, `Pbkdf2PasswordHasher.cs:38`), que não dá
  pista de acerto pelo tempo de resposta (*timing attack*).

Evidência: após cadastrar, a coluna `password_hash` no banco mostra `pbkdf2$100000$...`
(coberto por `test-api.sh` e pelos testes E2E).

## 2) Controle de acesso com permissões (RBAC)

Perfis: guest (hóspede) e equipe com sub-perfis admin, engineer e concierge.

- Autenticação por JWT assinado (HMAC-SHA256). O login emite o token com as *claims*
  do usuário (`SpaceStay.Api/Security/JwtTokenService.cs`); a API valida emissor,
  audiência, validade e assinatura, e usa expiração curta (padrão 120 min) em
  `Program.cs:60-76`.
- Autorização em duas camadas (princípio do menor privilégio e defesa em profundidade):
  1. No endpoint, com os atributos `[Authorize]` e `[Authorize(Roles=...)]`:

     | Recurso | Quem pode |
     |---|---|
     | Alertas (listar e reconhecer) | só equipe: `admin,engineer,concierge` (`AlertsController.cs:14`) |
     | Criar reserva ou excursão | só `guest` (`BookingsController.cs:30`, `ExcursionsController.cs:22`) |
     | Listar módulos e reservas | autenticado (`ModulesController.cs:12`, `BookingsController.cs:13`) |

  2. No serviço, checando a posse do recurso além do perfil:
     - o hóspede só enxerga e abre as próprias reservas (`BookingService.cs:15` filtra;
       `:100-101` bloqueia com 403);
     - só a equipe reconhece alertas (`AlertService.cs:19-20`).
- Respostas corretas: token ausente ou inválido retorna 401; perfil sem permissão retorna 403.

## 3) Três práticas adicionais de segurança

### 3.1 Validação de entrada (contra dados malformados)
- Data annotations em todos os DTOs: `[Required]`, `[EmailAddress]`, `[StringLength]`,
  `[Range]` (`SpaceStay.Core/Dtos/Dtos.cs`).
- Com `[ApiController]` em todos os controllers, uma requisição com `ModelState` inválido é
  rejeitada automaticamente com 400 antes de chegar à regra de negócio.
- As regras de negócio reforçam a validação (datas, capacidade do módulo) com 400 ou 409.

### 3.2 Proteção contra SQL Injection
- Todo o acesso a dados é via EF Core e LINQ, que gera comandos parametrizados; nenhum
  parâmetro do usuário é concatenado em SQL.
- Verificado: zero `FromSqlRaw`, `ExecuteSqlRaw`, `CommandText` ou SQL montado por string no
  projeto (`SpaceStay.Infra/Repositories/Repositories.cs` usa só `Where`, `FirstOrDefaultAsync`, etc.).

### 3.3 Token nas rotas protegidas e falha segura
- Toda rota sensível exige token (seção 2). Pipeline: `UseAuthentication()` e depois
  `UseAuthorization()` (`Program.cs:127-128`).
- Mensagem de login genérica: sempre `"E-mail ou senha inválidos."` (`AuthService.cs:61`),
  que não revela se o e-mail existe (anti enumeração de usuários).
- Defesa em profundidade: o tratamento global de erros devolve Problem Details e não vaza
  *stack trace* em erros 500 (`Middleware/ErrorHandlingMiddleware.cs:30-39`).

---

## Modelo de ameaças (raciocínio, não checklist)

- XSS: o front-end é um app Android nativo (não é navegador), então o XSS refletido
  clássico tem baixo risco aqui; mesmo assim validamos a entrada e o Jetpack Compose
  renderiza texto como dado, não como HTML/JS. Citamos para mostrar que entendemos a
  ameaça, e não só marcamos a caixinha.
- CORS liberado (`AllowAnyOrigin`) é aceitável porque CORS é um mecanismo de navegador e
  nosso cliente é nativo; numa versão web restringiríamos as origens.
- `POST /api/readings` é `AllowAnonymous` (sensor e simulador da Parte 6). É uma decisão
  consciente para a GS; em produção o dispositivo usaria chave ou identidade própria.
- Segredos (chave JWT, senha do banco) ficam em `appsettings` só para desenvolvimento; em
  produção iriam para variáveis de ambiente ou um secret manager.

## Fora de escopo (propositalmente enxuto)

*Rate-limiting* no login, *refresh tokens*, 2FA e *account lockout* não são exigidos pela
disciplina e foram deixados de fora para manter o escopo simples; ficam citados como
evolução futura.

## Evidências
- `tests/evidence/test-run.txt`: testes unitários do hasher mais E2E de RBAC e login inválido.
- `backend/test-api.sh`: casos de fumaça cobrindo 401 (sem token), 403 (perfil), 409 (regra)
  e a senha hasheada no banco.
