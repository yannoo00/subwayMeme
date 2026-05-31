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

echo "[3/6] python -m websockify 8770 (로비) 시작 중..."
python -m websockify 8770 127.0.0.1:7770 &
WS_LOBBY_PID=$!

echo "[4/6] python -m websockify 8771 (게임) 시작 중..."
python -m websockify 8771 127.0.0.1:7771 &
WS_GAME_PID=$!

echo "[5/6] nginx 리버스 프록시(8080) 시작 중..."
# daemon off 로 포그라운드 실행 후 &: PID 추적 가능 (기본은 데몬화되어 PID 가 어긋남).
nginx -c "$ROOT_DIR/nginx.conf" -g 'daemon off;' &
NGINX_PID=$!

echo "[6/6] ngrok(8080 -> static 도메인) 시작 중..."
ngrok http 8080 --url=https://corned-catacomb-agreeably.ngrok-free.dev > /tmp/subwaymeme-ngrok.log 2>&1 &
NGROK_PID=$!

echo ""
echo "전체 시작 완료. Ctrl+C 로 모두 종료."
echo "  ngrok 상태: https://corned-catacomb-agreeably.ngrok-free.dev (로그: /tmp/subwaymeme-ngrok.log)"

trap "echo '종료 중...'; kill $GAME_PID $LOBBY_PID $WS_LOBBY_PID $WS_GAME_PID $NGINX_PID $NGROK_PID 2>/dev/null" EXIT INT TERM
wait
