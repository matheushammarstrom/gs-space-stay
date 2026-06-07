# 🛰️ SpaceStay - Parte 1: Modelo de Banco de Dados

Modelagem relacional que sustenta a plataforma do hotel orbital SpaceStay, garantindo
consistência e integridade dos dados e preparando o terreno para a persistência via API
(Parte 2). SGBD-alvo: MySQL 8.0+ / MariaDB 10.2+.

## Conteúdo da pasta `database/`

| Arquivo | O que é |
|---|---|
| `schema.sql` | DDL: criação das 9 tabelas com PKs, FKs, `UNIQUE`, `CHECK`, `ENUM` e índices. |
| `seed.sql` | Dados de exemplo (cenário demonstrável, com um alerta crítico em aberto). |
| `queries.sql` | Consultas de simulação de uso (as 4 exigidas e 4 extras). |
| `er-diagram.md` | Diagrama ER em Mermaid e versão DBML (para exportar imagem no dbdiagram.io). |
| `er-diagram.png` | Imagem do diagrama ER (exportada do dbdiagram.io) usada no relatório PDF. |
| `README.md` | Este documento (visão geral, dicionário de dados, como rodar). |

## O que o enunciado pede (e onde está entregue)

| Entregável oficial | Status | Onde |
|---|---|---|
| Diagrama ER com pelo menos 4 entidades e relacionamentos | OK (temos 9) | `er-diagram.md` |
| Script `.sql` com `CREATE TABLE`, PKs e FKs | OK | `schema.sql` |
| Consultas SQL básicas de uso ("buscar pessoas por abrigo", aqui "hóspedes por módulo") | OK | `queries.sql` |

---

## Como executar

Com um servidor MySQL/MariaDB rodando:

```bash
# 1) cria o schema (banco "spacestay" e tabelas)
mysql -u root -p < schema.sql

# 2) popula com dados de exemplo
mysql -u root -p spacestay < seed.sql

# 3) roda as consultas de demonstração
mysql -u root -p spacestay < queries.sql
```

> Alternativa: abrir os arquivos no MySQL Workbench ou DBeaver e executar o conteúdo
> inteiro. Os scripts são idempotentes (podem ser re-executados: o `schema.sql` recria
> as tabelas e o `seed.sql` faz `TRUNCATE` antes de inserir).

### Subir um MySQL rápido com Docker (opcional)

```bash
docker run --name spacestay-db -e MYSQL_ROOT_PASSWORD=root -p 3306:3306 -d mysql:8
mysql -h 127.0.0.1 -u root -proot < schema.sql
mysql -h 127.0.0.1 -u root -proot spacestay < seed.sql
```

---

## Dicionário de dados (9 entidades)

| # | Tabela | Descrição | Destaques de integridade |
|---|---|---|---|
| 1 | `guests` | Hóspedes (clientes do hotel). | `email` único; `password_hash` (nunca senha pura). |
| 2 | `staff` | Equipe interna. | `role` ENUM (`admin`/`engineer`/`concierge`); `email` único. |
| 3 | `modules` | Módulos/quartos (cápsulas seladas). | `name` único; `capacity > 0` (CHECK). |
| 4 | `bookings` | Reservas de hospedagem (N:N entre guests e modules). | FK guest (CASCADE) e module (RESTRICT); `check_out > check_in` (CHECK). |
| 5 | `sensors` | Sensores IoT por módulo. | um sensor por tipo/módulo (UNIQUE); `max_safe > min_safe` (CHECK). |
| 6 | `sensor_readings` | Leituras de telemetria (alto volume, PK `BIGINT`). | Índice `(sensor_id, recorded_at)`. |
| 7 | `alerts` | Alertas de limite violado. | FK para sensor, module e `resolved_by` (staff, `SET NULL`). |
| 8 | `excursions` | Atividades/excursões. | `capacity > 0` (CHECK). |
| 9 | `excursion_bookings` | Junção N:N entre guests e excursions. | UNIQUE `(guest_id, excursion_id)` evita reserva duplicada. |

### Regras de negócio refletidas no schema

- Senhas só armazenadas como `password_hash` (preparado para a Parte 5, Segurança).
- E-mail único em `guests` e `staff` (login).
- Capacidade de módulo e excursão sempre positiva (`CHECK capacity > 0`).
- Período de reserva coerente (`CHECK check_out > check_in`).
- Faixa de sensor coerente (`CHECK max_safe > min_safe`).
- Um sensor por tipo por módulo (`UNIQUE (module_id, type)`).
- `ON DELETE` pensado por relação:
  - Apagar hóspede apaga suas reservas e excursões (`CASCADE`).
  - Não permite apagar módulo com reservas (`RESTRICT`), preservando o histórico.
  - Apagar membro da equipe mantém o alerta, mas `resolved_by` vira `NULL` (`SET NULL`).
  - Apagar módulo apaga seus sensores e, em cadeia, as leituras (`CASCADE`).

---

## Faixas seguras dos sensores (usadas no `seed.sql` e na lógica de alerta da Parte 6)

| Sensor | Unidade | Faixa segura | Efeito ao violar |
|---|---|---|---|
| O₂ | % | 19,5 a 23,5 | O₂ baixo gera alerta crítico |
| CO₂ | ppm | 0 a 1000 (aviso acima de 1000, crítico acima de 2000) | CO₂ alto gera warning ou crítico |
| Pressão | kPa | 95 a 105 | fora da faixa indica possível vazamento, crítico |
| Temperatura | °C | 18 a 27 | fora da faixa afeta conforto e segurança, warning |
| Umidade | %RH | 30 a 60 | fora da faixa favorece condensação, warning |
| Água (reserva) | % | 20 a 100 (aviso abaixo de 20) | reserva baixa gera warning |

> As colunas `min_safe`/`max_safe` em `sensors` guardam essa faixa por sensor. A
> classificação da severidade (warning ou critical) fica na camada de Service da API
> (Parte 2), comparando a leitura recebida com esses limites.

---

## Consultas de simulação (resumo)

As 4 primeiras são as exigidas; as demais alimentam o app. O detalhe e o resultado
esperado de cada uma estão comentados em `queries.sql`.

1. (exigida) Alertas críticos em aberto com nome do módulo.
2. (exigida) Hóspedes por módulo (reservas ativas), o análogo de "buscar pessoas por abrigo".
3. (exigida) Última leitura de cada sensor de um módulo (telemetria atual).
4. (exigida) Excursões por hóspede (contagem).
5. Painel Mission Control: módulos, status e número de alertas em aberto.
6. Leituras fora da faixa segura (a regra que dispara alertas).
7. Ocupação por módulo (vagas disponíveis).
8. Alertas tratados por membro da equipe (auditoria).

---

## Conexão com as próximas partes

- Parte 2 (API): este schema é a fonte única de verdade. Os modelos do EF Core
  (Code-First) são criados espelhando estas tabelas, e os tipos foram escolhidos para
  mapear de forma limpa (`INT` para `int`, `BIGINT` para `long`, `DECIMAL` para
  `decimal`, `BOOLEAN` para `bool`, `DATETIME` para `DateTime`). Provider EF Core para
  MySQL: Pomelo.EntityFrameworkCore.MySql.
- Parte 5 (Segurança): `password_hash` e `role` já preparam login com hash e controle de
  acesso por perfil (RBAC).
- Parte 6 (IoT): a cadeia `modules`, `sensors`, `sensor_readings` e `alerts` é o caminho
  dos dados de telemetria, do simulador até o alerta na tela da equipe.

---

## Checklist de aceite (Parte 1)

- [x] Diagrama ER com 4 ou mais entidades e cardinalidades (temos 9) em `er-diagram.md`.
- [x] `schema.sql` com todos os `CREATE TABLE`, PKs e FKs.
- [x] `seed.sql` com dados demonstráveis.
- [x] `queries.sql` com pelo menos as 4 consultas pedidas.
- [x] Schema preparado para espelhar os modelos EF Core (fonte única de verdade).
- [x] Imagem do diagrama (`er-diagram.png`) e explicação das consultas no relatório PDF (`../relatorio/relatorio.pdf`).
- [x] Schema validado em MySQL 8.4 real (criação, carga, consultas e testes de integridade).
