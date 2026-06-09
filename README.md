# 🛰️ SpaceStay — Global Solution 2026/1 (FIAP)

> O SpaceStay é a plataforma de operação de um **hotel orbital**: gerencia as estadias dos hóspedes enquanto monitora continuamente o **suporte à vida** de cada módulo (O₂, CO₂, pressão, temperatura, umidade, água), para que a equipe possa agir *antes* que um problema de conforto vire um incidente de segurança.

Tema: **"A Economia Espacial — soluções para o desafio da Indústria Espacial."** Enfoque: turismo espacial e novos modelos de negócio.

## A ideia

O turismo orbital é um mercado emergente real, e um quarto de hotel espacial não é um quarto: é uma cápsula selada de suporte à vida. O SpaceStay transforma a telemetria de suporte à vida em conforto para o hóspede e em ação para a equipe, integrando os dados dos sensores do lado do espaço com um aplicativo de operação do lado da Terra.

Dois tipos de usuário guiam o produto:

- **Hóspedes** se cadastram, reservam uma cabine, acompanham o ambiente da própria cabine em tempo real em cartões de conforto e reservam ou cancelam excursões.
- **Equipe** (admin, engenheiro, concierge) monitora todos os módulos num painel de "Central de Operações" e reconhece os alertas conforme eles acontecem.

## O que há neste repositório

| Pasta | Parte | Conteúdo |
|-------|-------|----------|
| `backend/` | 2, 3, 5 | API ASP.NET Core (.NET 8), três camadas, EF Core + MySQL, JWT + RBAC, Swagger, testes xUnit. Documento de segurança em `backend/SECURITY.md`. |
| `database/` | 1 | Schema relacional, seed, consultas de exemplo e o diagrama ER. |
| `mobile/` | 4 | Aplicativo Android (Kotlin + Jetpack Compose, MVVM, Retrofit). |
| `iot/` | 6 | Cadeia IoT: ESP32 no Wokwi, MQTT e Node-RED alimentando a API. |
| `ml/` | 6 | Notebook de manutenção preditiva (scikit-learn). |
| `tests/` | 3 | Suíte xUnit (unidade + ponta a ponta) e evidências de execução. |
| `apresentacao/` | — | Slides do pitch (PowerPoint e imagens). |

As pastas principais têm o seu próprio README com detalhes e instruções de execução.

O **relatório técnico completo** do projeto (capa, introdução e as seis entregas) está em [`relatorio.pdf`](relatorio.pdf).

## Arquitetura

```
Sensores (ESP32 no Wokwi)
        │  MQTT
        ▼
    Node-RED  ──POST /api/readings──►  API ASP.NET Core
                                       Controller → Service → Repository
                                       Auth (hash + perfis), Alertas
                                              │  EF Core
                                              ▼
                                       Banco relacional (MySQL)
                                       guests, modules, bookings,
                                       sensors, readings, alerts

App Android (Kotlin/Compose)  ──REST / Retrofit──►  API
    telas de hóspede e de equipe
```

Quando uma leitura sai da faixa segura de um sensor, a API gera um **alerta** e o aplicativo da equipe acende na hora. Esse é o momento central do produto.

## Stack de tecnologias

- **Backend / API:** ASP.NET Core Web API (.NET 8), Entity Framework Core, injeção de dependência, camadas de serviço e repositório, xUnit.
- **Banco de dados:** relacional (MySQL) via EF Core Code-First e migrations.
- **Mobile:** Kotlin + Jetpack Compose (Navigation, MVVM + LiveData, Retrofit).
- **IoT:** ESP32 simulado no Wokwi, MQTT, Node-RED.
- **ML:** Python com scikit-learn (uma árvore de decisão para manutenção preditiva).

## Como executar (rápido)

1. **Banco de dados:** suba uma instância MySQL 8 (veja `database/README.md`).
2. **Backend:** `cd backend && dotnet run --project SpaceStay.Api --urls http://0.0.0.0:5080`. Swagger em `/swagger`.
3. **Mobile:** abra `mobile/` no Android Studio e execute em um emulador.
4. **IoT:** veja `iot/README.md` (Wokwi para os sensores e `./iot/run-node-red.sh` para a ponte).

As credenciais de teste estão em `backend/README.md`.

## Cobertura dos requisitos

- [x] Banco relacional: diagrama ER (4+ entidades), `CREATE TABLE` com PK/FK, consultas de exemplo — `database/`.
- [x] API REST: 5+ endpoints, em camadas, documentada no Swagger — `backend/`.
- [x] Plano de testes: unidade e ponta a ponta, 46 testes passando com evidências — `tests/`.
- [x] Front-end mobile: 3+ telas consumindo a API — `mobile/`.
- [x] Segurança: senhas com hash (PBKDF2), controle de acesso por perfil, validação de entrada — `backend/SECURITY.md`.
- [x] Simulação IoT alimentando o app, com a lógica explicada — `iot/`.
- [x] (Opcional) ML: notebook de manutenção preditiva — `ml/`.

## Equipe

| Nome | RM |
|------|------|
| Vinicius Taiki | RM554226 |
| Matheus Diniz | RM553083 |
| Matheus Hammarstrom | RM553403 |
| Lucas Fonseca | RM552973 |
