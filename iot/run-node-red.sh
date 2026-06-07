#!/bin/bash
# Sobe o Node-RED ja com o flow do SpaceStay carregado e ativo.
#
# Pre-requisitos: Node.js instalado e a API rodando em http://localhost:5080.
# Na primeira vez, instala o Node-RED via npm (pode levar um minuto).
#
# Uso:   ./run-node-red.sh
# Para:  Ctrl+C
set -e

HERE="$(cd "$(dirname "$0")" && pwd)"
USERDIR="${1:-$HOME/.spacestay-nodered}"

if ! command -v node-red >/dev/null 2>&1; then
  echo "Node-RED nao encontrado, instalando via npm..."
  npm install -g node-red
fi

mkdir -p "$USERDIR"
cp "$HERE/node-red/flows.json" "$USERDIR/flows.json"

echo "Node-RED subindo em http://localhost:1880"
echo "O flow assina o MQTT e faz POST em http://localhost:5080/api/readings"
echo "(se a API estiver em outra maquina, ajuste a URL no no 'POST /api/readings')"
exec node-red -u "$USERDIR" flows.json
