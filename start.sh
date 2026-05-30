#!/bin/bash

ROOT_DIR="$(cd "$(dirname "$0")" && pwd)"

echo "[서버 시작]"

echo "[1/4] GameServer 시작 중..."
dotnet run --project "$ROOT_DIR/Server/GameServer" &
GAME_PID=$!

echo "GameServer 준비 대기 중 (2초)..."
sleep 2

echo "[2/4] LobbyServer 시작 중..."
dotnet run --project "$ROOT_DIR/Server/LobbyServer" &
LOBBY_PID=$!

echo "[3/4] python -m websockify 8770 (로비) 시작 중..."
python -m websockify 8770 127.0.0.1:7770 &
WS_LOBBY_PID=$!

echo "[4/4] python -m websockify 8771 (게임) 시작 중..."
python -m websockify 8771 127.0.0.1:7771 &
WS_GAME_PID=$!

echo ""
echo "전체 시작 완료. Ctrl+C 로 모두 종료."

trap "echo '종료 중...'; kill $GAME_PID $LOBBY_PID $WS_LOBBY_PID $WS_GAME_PID 2>/dev/null" EXIT INT TERM
wait
