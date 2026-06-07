# 📡 SpaceStay - Parte 6: Simulação IoT (Wokwi + MQTT + Node-RED)

Esta parte fecha o ciclo entre o "espaço" e a "Terra": sensores simulados de um módulo
publicam a telemetria, que chega na API e vira alerta na tela da equipe.

## Arquitetura (fluxo dos dados)

1. **Wokwi (ESP32)** simula os sensores de um módulo (Cupola Suite 3) e publica cada
   leitura por MQTT.
2. **Broker MQTT** (HiveMQ público) recebe as mensagens no tópico `spacestay/fiap-gs-2026/readings`.
3. **Node-RED** assina esse tópico e faz um `POST /api/readings` para a nossa API.
4. **API** (Parte 2) persiste a leitura e compara com a faixa segura do sensor; se estiver
   fora, cria um **alerta** com a gravidade calculada (regra `AlertRules`).
5. **App** (Parte 4): a "Mission Control" da equipe mostra o alerta ao vivo, e a "Minha
   Estadia" do hóspede reflete o conforto da cabine.

Resumindo: sensor do lado do espaço, operação do lado da Terra. A API não muda nada entre
receber a leitura de um `curl`, do Node-RED ou de um Arduino real.

## Sensores e lógica (o entregável de explicação)

O ESP32 simula três sensores da Cupola Suite 3. Os IDs vêm do `database/seed.sql`
(o2=1, co2=2, pressure=3, temperature=4, humidity=5, water=6).

| Sensor | O que mede | Faixa segura | Efeito ao violar |
|---|---|---|---|
| CO₂ (id 2) | Acúmulo de gás carbônico | abaixo de 1000 ppm | acima de 1000 = aviso; acima de 2000 = crítico (problema no depurador) |
| Temperatura (id 4) | Temperatura da cabine | 18 a 27 °C | fora da faixa = aviso (conforto e segurança) |
| Umidade (id 5) | Umidade relativa | 30 a 60 %RH | fora da faixa = aviso (condensação) |

- O **CO₂** é controlado por um **potenciômetro** no Wokwi: girando o knob, o valor sobe
  de 400 até 3000 ppm. É o nosso "momento de ouro" do pitch: passar de 2000 dispara o
  alerta crítico ao vivo.
- **Temperatura e umidade** vêm de um **DHT22** simulado (dá pra ajustar os valores no
  próprio Wokwi durante a demo).

A regra de severidade é a mesma da API (uma fonte de verdade, em `AlertRules`): CO₂ acima
de 2000 é crítico, acima de 1000 é aviso; O₂ e pressão fora da faixa são críticos; os
demais são aviso.

## Como rodar

Pré-requisito: a **API no ar** (`dotnet run --project SpaceStay.Api --urls http://0.0.0.0:5080`)
e o app aberto (de preferência logado como equipe, na Mission Control).

### 1) Wokwi (sensores)
1. Acesse <https://wokwi.com> e crie um projeto novo de **ESP32**.
2. Cole o conteúdo de `iot/wokwi/sketch.ino` no editor de código.
3. Abra a aba `diagram.json` e cole o conteúdo de `iot/wokwi/diagram.json` (ESP32, DHT22 e
   potenciômetro já ligados).
4. As bibliotecas (`iot/wokwi/libraries.txt`) são instaladas pelo Wokwi automaticamente.
5. Clique em **play**. O monitor serial mostra as leituras sendo publicadas.

### 2) Node-RED (ponte MQTT para a API)

Atalho de um comando (instala o Node-RED na primeira vez e já sobe com o flow ativo):

```bash
./iot/run-node-red.sh
```

Ou manualmente:
1. Com o Node-RED rodando, use **Import** e cole o conteúdo de `iot/node-red/flows.json`.
2. Clique em **Deploy**. O flow assina `spacestay/fiap-gs-2026/readings` no HiveMQ e faz o POST na API.
3. O nó de debug mostra a resposta da API (o `201` com a leitura e, se for o caso, o alerta).

> O Node-RED faz POST em `http://localhost:5080/api/readings`. Se a API estiver em outra
> máquina, ajuste a URL no nó "POST /api/readings".

### 3) Demo
Gire o potenciômetro no Wokwi até o CO₂ passar de 2000 ppm. Em segundos, o alerta crítico
aparece na "Mission Control", e a equipe pode tocar em **Reconhecer**.

## Atalho para testar sem a stack

O endpoint é o mesmo, então dá para validar o caminho leitura, alerta e app com um `curl`
(é exatamente o que o Node-RED envia):

```bash
curl -X POST http://localhost:5080/api/readings \
  -H "Content-Type: application/json" \
  -d '{"sensorId":2,"value":2200}'
```

Resposta: `201` com `alertCreated: true` e a gravidade `critical`.

## Observações

- O broker é público (`broker.hivemq.com`), então não precisa instalar broker local. Em
  produção usaríamos um broker próprio com autenticação.
- O `POST /api/readings` é aberto (AllowAnonymous) de propósito para a simulação; em
  produção o dispositivo usaria uma chave ou identidade própria (ver `backend/SECURITY.md`).
- Esta é a opção "Arduino + MQTT" do enunciado, com o Wokwi no lugar do TinkerCad.
- O caminho MQTT, Node-RED, API e alerta foi validado de ponta a ponta: publicando uma
  leitura no tópico, o Node-RED faz o POST e a API cria o alerta. O Wokwi é a fonte real
  das leituras nesse mesmo tópico.
