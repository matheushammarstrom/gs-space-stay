#!/usr/bin/env bash
# SpaceStay - Parte 2 (API): teste de fumaça ponta a ponta via curl.
# Suba a API (dotnet run) e o MySQL antes; depois rode ./test-api.sh
# Cobre: cadastro e login (hash e JWT), RBAC, CRUD de reservas e regra de limiar de alerta.
set -u
API="${API:-http://localhost:5080}"
pass=0; fail=0

# req METHOD URL TOKEN [JSON] -> popula HTTP_CODE e BODY
req() {
  local m=$1 url=$2 tok=$3 body=${4:-}
  local args=(-s -w $'\n%{http_code}' -X "$m" "$API$url" -H "Content-Type: application/json")
  [ -n "$tok" ] && args+=(-H "Authorization: Bearer $tok")
  [ -n "$body" ] && args+=(-d "$body")
  local out; out=$(curl "${args[@]}")
  HTTP_CODE=$(printf '%s' "$out" | tail -n1)
  BODY=$(printf '%s' "$out" | sed '$d')
}
# jget JSON PYPATH  (ex.: jget "$BODY" "['token']")
jget() { printf '%s' "$1" | python3 -c "import sys,json
try:
 d=json.load(sys.stdin); print(eval('d'+sys.argv[1]))
except Exception: print('')" "$2" 2>/dev/null; }

check() { # DESC EXPECTED ACTUAL
  if [ "$2" = "$3" ]; then echo "  [PASS] $1 (HTTP $3)"; pass=$((pass+1));
  else echo "  [FAIL] $1 (esperado $2, obtido $3)"; fail=$((fail+1)); fi
}

echo "================================================================"
echo " SpaceStay API: testes de fumaça ($API)"
echo "================================================================"

EMAIL="teste+$(date +%s)@example.com"

echo "[Auth & Hashing]"
req POST /api/auth/register "" "{\"name\":\"Teste Curl\",\"email\":\"$EMAIL\",\"password\":\"Senha@123\",\"nationality\":\"Brasil\",\"medicalClearance\":true}"
check "TC1 cadastro de hóspede" 201 "$HTTP_CODE"
GUEST_TOKEN=$(jget "$BODY" "['token']")

req POST /api/auth/register "" "{\"name\":\"x\",\"email\":\"naoeemail\",\"password\":\"123\"}"
check "validação de DTO (400)" 400 "$HTTP_CODE"

req POST /api/auth/login "" "{\"email\":\"$EMAIL\",\"password\":\"errada\"}"
check "TC2 login com senha errada (401)" 401 "$HTTP_CODE"

req POST /api/auth/login "" "{\"email\":\"$EMAIL\",\"password\":\"Senha@123\"}"
check "login correto (200)" 200 "$HTTP_CODE"

req POST /api/auth/login "" "{\"email\":\"rafael.lima@spacestay.space\",\"password\":\"Engineer@123\"}"
check "login da equipe (200)" 200 "$HTTP_CODE"
STAFF_TOKEN=$(jget "$BODY" "['token']")
STAFF_ROLE=$(jget "$BODY" "['role']")
echo "     (role da equipe: $STAFF_ROLE)"

echo "[Segurança / acesso]"
req GET /api/modules "" ""
check "sem token retorna 401" 401 "$HTTP_CODE"

req GET "/api/alerts?status=open" "$GUEST_TOKEN" ""
check "TC7 hóspede em rota de equipe retorna 403" 403 "$HTTP_CODE"

echo "[Módulos & Telemetria]"
req GET "/api/modules?page=1&pageSize=10" "$GUEST_TOKEN" ""
check "listar módulos (200)" 200 "$HTTP_CODE"
# descobre os ids por nome (forma robusta, independente da ordem)
AURORA_ID=$(printf '%s' "$BODY" | python3 -c "import sys,json;d=json.load(sys.stdin);print(next(m['id'] for m in d['items'] if m['name']=='Aurora Suite 1'))" 2>/dev/null)
CUPOLA_ID=$(printf '%s' "$BODY" | python3 -c "import sys,json;d=json.load(sys.stdin);print(next(m['id'] for m in d['items'] if m['name']=='Cupola Suite 3'))" 2>/dev/null)
echo "     (Aurora id=$AURORA_ID, Cupola id=$CUPOLA_ID)"

req GET "/api/modules/$CUPOLA_ID/readings" "$GUEST_TOKEN" ""
check "leituras do módulo (200)" 200 "$HTTP_CODE"

req GET "/api/modules/$CUPOLA_ID/sensors" "$GUEST_TOKEN" ""
CO2_SENSOR=$(printf '%s' "$BODY" | python3 -c "import sys,json;d=json.load(sys.stdin);print(next(s['id'] for s in d if s['type']=='co2'))" 2>/dev/null)
echo "     (sensor CO₂ do Cupola id=$CO2_SENSOR)"

echo "[Reservas: CRUD e capacidade]"
NOW=$(date -u +%Y-%m-%dT%H:%M:%S)
OUT=$(date -u -v+2d +%Y-%m-%dT%H:%M:%S 2>/dev/null || date -u -d "+2 days" +%Y-%m-%dT%H:%M:%S)
req POST /api/bookings "$GUEST_TOKEN" "{\"moduleId\":$AURORA_ID,\"checkIn\":\"$NOW\",\"checkOut\":\"$OUT\"}"
check "TC3 reserva em módulo livre (201)" 201 "$HTTP_CODE"
BOOKING_ID=$(jget "$BODY" "['id']")

req POST /api/bookings "$GUEST_TOKEN" "{\"moduleId\":$CUPOLA_ID,\"checkIn\":\"$NOW\",\"checkOut\":\"$OUT\"}"
check "TC4 reserva em módulo lotado (409)" 409 "$HTTP_CODE"

req PUT "/api/bookings/$BOOKING_ID" "$GUEST_TOKEN" "{\"status\":\"completed\"}"
check "atualizar reserva (200)" 200 "$HTTP_CODE"

req GET /api/bookings "$GUEST_TOKEN" ""
MINE=$(printf '%s' "$BODY" | python3 -c "import sys,json;d=json.load(sys.stdin);print(all(b['guestName']=='Teste Curl' for b in d['items']) and len(d['items'])>=1)" 2>/dev/null)
check "hóspede vê só as próprias reservas" "True" "$MINE"

echo "[Regra IoT: limiar de alerta]"
req POST /api/readings "" "{\"sensorId\":$CO2_SENSOR,\"value\":600}"
SAFE=$(jget "$BODY" "['alertCreated']")
check "TC5 leitura segura (CO₂=600) sem alerta" "201|False" "$HTTP_CODE|$SAFE"

req POST /api/readings "" "{\"sensorId\":$CO2_SENSOR,\"value\":2150}"
CRIT=$(jget "$BODY" "['alertCreated']")
SEV=$(jget "$BODY" "['alert']['severity']")
check "TC6 leitura crítica (CO₂=2150) gera alerta crítico" "201|True|critical" "$HTTP_CODE|$CRIT|$SEV"
ALERT_ID=$(jget "$BODY" "['alert']['id']")

echo "[Alertas: equipe]"
req GET "/api/alerts?status=open" "$STAFF_TOKEN" ""
check "equipe lista alertas abertos (200)" 200 "$HTTP_CODE"

req PUT "/api/alerts/$ALERT_ID/acknowledge" "$STAFF_TOKEN" ""
ACK=$(jget "$BODY" "['status']")
check "reconhecer alerta (200, acknowledged)" "200|acknowledged" "$HTTP_CODE|$ACK"

req PUT "/api/alerts/$ALERT_ID/acknowledge" "$GUEST_TOKEN" ""
check "hóspede não reconhece alerta (403)" 403 "$HTTP_CODE"

echo "[Excursões]"
req GET /api/excursions "$GUEST_TOKEN" ""
check "listar excursões (200)" 200 "$HTTP_CODE"
EXC_ID=$(printf '%s' "$BODY" | python3 -c "import sys,json;d=json.load(sys.stdin);print(d[0]['id'])" 2>/dev/null)

req POST "/api/excursions/$EXC_ID/book" "$GUEST_TOKEN" ""
check "reservar excursão (201)" 201 "$HTTP_CODE"

req POST "/api/excursions/$EXC_ID/book" "$GUEST_TOKEN" ""
check "reservar a MESMA excursão de novo (409)" 409 "$HTTP_CODE"

echo "================================================================"
echo " Resultado: $pass passaram, $fail falharam"
echo "================================================================"
[ "$fail" = "0" ]
