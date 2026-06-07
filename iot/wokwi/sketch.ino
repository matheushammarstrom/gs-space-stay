// SpaceStay - Parte 6 (IoT): ESP32 simulando os sensores de um módulo (Cupola Suite 3).
//
// Lê:
//   - CO₂ a partir de um potenciômetro (você gira o knob para forçar o pico na demo).
//   - Temperatura e umidade a partir de um DHT22.
// Publica cada leitura como JSON em um tópico MQTT. O Node-RED assina esse tópico e faz
// o POST em /api/readings da nossa API, que aplica a regra de alerta.
//
// Roda no Wokwi (https://wokwi.com) com um ESP32. O Wokwi tem acesso à internet pela sua
// gateway, então conseguimos publicar num broker MQTT público.

#include <WiFi.h>
#include <PubSubClient.h>
#include <DHT.h>

// WiFi virtual do Wokwi (rede aberta, sem senha).
const char* WIFI_SSID = "Wokwi-GUEST";
const char* WIFI_PASS = "";

// Broker MQTT público (sem cadastro). O Node-RED assina o mesmo broker/tópico.
const char* MQTT_HOST  = "broker.hivemq.com";
const int   MQTT_PORT  = 1883;
const char* MQTT_TOPIC = "spacestay/fiap-gs-2026/readings";

// IDs dos sensores da Cupola Suite 3 no nosso banco (ver database/seed.sql):
// o2=1, co2=2, pressure=3, temperature=4, humidity=5, water=6.
const int SENSOR_CO2  = 2;
const int SENSOR_TEMP = 4;
const int SENSOR_HUM  = 5;

const int PIN_POT = 34;   // potenciômetro (ADC) = CO₂
const int PIN_DHT = 15;   // DHT22

DHT dht(PIN_DHT, DHT22);
WiFiClient wifiClient;
PubSubClient mqtt(wifiClient);

void connectWifi() {
  WiFi.begin(WIFI_SSID, WIFI_PASS);
  Serial.print("Conectando ao WiFi");
  while (WiFi.status() != WL_CONNECTED) { delay(300); Serial.print("."); }
  Serial.println(" ok");
}

void connectMqtt() {
  mqtt.setServer(MQTT_HOST, MQTT_PORT);
  while (!mqtt.connected()) {
    Serial.print("Conectando ao MQTT...");
    String clientId = "spacestay-esp32-" + String(random(0xffff), HEX);
    if (mqtt.connect(clientId.c_str())) {
      Serial.println(" ok");
    } else {
      Serial.print(" falhou, rc="); Serial.print(mqtt.state()); delay(1000);
    }
  }
}

// Publica uma leitura {sensorId, value} no tópico.
void publishReading(int sensorId, float value) {
  char payload[64];
  snprintf(payload, sizeof(payload), "{\"sensorId\":%d,\"value\":%.2f}", sensorId, value);
  mqtt.publish(MQTT_TOPIC, payload);
  Serial.print("publicado: "); Serial.println(payload);
}

void setup() {
  Serial.begin(115200);
  dht.begin();
  connectWifi();
  connectMqtt();
}

void loop() {
  if (!mqtt.connected()) connectMqtt();
  mqtt.loop();

  // CO₂: o potenciômetro (0..4095) vira 400..3000 ppm. No máximo passa de 2000 = crítico.
  int potRaw = analogRead(PIN_POT);
  float co2 = map(potRaw, 0, 4095, 400, 3000);
  publishReading(SENSOR_CO2, co2);

  float temp = dht.readTemperature();
  float hum  = dht.readHumidity();
  if (!isnan(temp)) publishReading(SENSOR_TEMP, temp);
  if (!isnan(hum))  publishReading(SENSOR_HUM, hum);

  delay(3000);   // uma rodada de leituras a cada 3 segundos
}
