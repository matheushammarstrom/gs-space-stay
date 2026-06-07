# 📱 SpaceStay - Parte 4: Front-end Mobile (Kotlin + Jetpack Compose)

App Android que consome a API REST (Parte 2), com 3 telas cobrindo os dois perfis de
usuário (hóspede e equipe). Arquitetura MVVM + LiveData, navegação com Navigation
Compose e rede com Retrofit.

## Stack (dentro do conteúdo do curso)

- Kotlin + Jetpack Compose (Material 3)
- MVVM + LiveData (sem Coroutines/Flow explícitos; Retrofit via `Call.enqueue` e callbacks)
- Navigation Compose (rotas e roteamento por perfil)
- Retrofit + Gson + OkHttp (consumo da API; um interceptor injeta o token JWT)
- i18n em `strings.xml` e validação de entrada no login

> Sem Hilt/Dagger e sem Coroutines/Flow (conforme escopo). DI manual simples e estado via LiveData.

## Cobertura do enunciado (Parte 4)

- [x] 3 telas funcionais (Login, Guest "My Stay", Staff "Mission Control").
- [x] Integração com a API (login, módulos, leituras, alertas, excursões, reservas).
- [x] Layout responsivo e com boa usabilidade (estados de loading, erro e vazio, `LazyColumn`/`LazyRow`).

## Estrutura

```
mobile/app/src/main/java/br/com/spacestay/
├── MainActivity.kt            # Activity + NavHost (roteia login para guest/staff)
├── data/                      # DTOs, Retrofit (ApiService/ApiClient), Repository, Session(JWT)
├── viewmodel/ViewModels.kt    # MVVM: Login, Guest, MissionControl (expõem LiveData)
└── ui/
    ├── theme/Theme.kt         # tema escuro "espacial" (Material 3)
    ├── components/Components.kt# medidores de conforto, cards de módulo/alerta
    └── screens/               # LoginScreen, GuestStayScreen, MissionControlScreen
```

Fluxo: o Composable observa o LiveData do ViewModel, que usa o Repository, que chama o
Retrofit (ApiService). O token JWT fica na `Session` e é anexado por um interceptor do
OkHttp.

## Telas

1. Login (com papel): valida e-mail e senha; ao autenticar, roteia para hóspede ou
   equipe conforme o `userType` do token.
2. Hóspede, "Minha Estadia": mostra o módulo da reserva ativa, os medidores de conforto
   (O₂, CO₂, pressão, temperatura, umidade, água) com barra dentro da faixa segura e
   rótulo (Ideal/Atenção/Crítico), e a lista de excursões com botão Reservar.
3. Equipe, "Mission Control": módulos em `LazyRow` (cor por status e número de alertas) e
   o feed de alertas em aberto em `LazyColumn`, cada um com botão Reconhecer (PUT
   acknowledge).

## Como rodar

> Pré-requisito: a API (Parte 2) precisa estar rodando. O emulador acessa o host pela
> URL `http://10.0.2.2:5080` (já configurada em `data/ApiClient.kt`).

No Android Studio (recomendado): abra a pasta `mobile/`, deixe sincronizar o Gradle e
clique em Run (escolha um emulador ou device API 26+).

Por linha de comando (com Android SDK + JDK 17):
```bash
export JAVA_HOME=/opt/homebrew/opt/openjdk@17
export ANDROID_HOME=/opt/homebrew/share/android-commandlinetools
cd mobile
./gradlew assembleDebug                 # gera o APK
adb install -r app/build/outputs/apk/debug/app-debug.apk
adb shell am start -n br.com.spacestay/.MainActivity
```

> Para device físico, troque `BASE_URL` em `ApiClient.kt` por `http://<IP-da-máquina>:5080/`
> (ou use `adb reverse tcp:5080 tcp:5080` e mantenha `10.0.2.2`/`localhost`).

## Credenciais de teste

| Perfil | E-mail | Senha |
|---|---|---|
| Hóspede | `ana.costa@example.com` | `Guest@123` |
| Equipe (engineer) | `rafael.lima@spacestay.space` | `Engineer@123` |
| Equipe (admin) | `helena.vasquez@spacestay.space` | `Admin@123` |

## Screenshots

Em `mobile/screenshots/` (Login, Minha Estadia, Mission Control), para o relatório PDF.
