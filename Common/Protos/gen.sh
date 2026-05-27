
PROTO_DIR="$(cd "$(dirname "$0")" && pwd)"
LOBBY_SERVER_OUT="$PROTO_DIR/../../Server/LobbyServer/Packet/Generated"
GAME_SERVER_OUT="$PROTO_DIR/../../Server/GameServer/Packet/Generated"
CLIENT_OUT="$PROTO_DIR/../../Client/Assets/Scripts/Network/Packets/Generated"

mkdir -p "$LOBBY_SERVER_OUT"
mkdir -p "$GAME_SERVER_OUT"
mkdir -p "$CLIENT_OUT"

protoc --proto_path="$PROTO_DIR" --csharp_out="$LOBBY_SERVER_OUT" "$PROTO_DIR/lobby.proto"
protoc --proto_path="$PROTO_DIR" --csharp_out="$GAME_SERVER_OUT"  "$PROTO_DIR/game.proto"
protoc --proto_path="$PROTO_DIR" --csharp_out="$CLIENT_OUT"       "$PROTO_DIR/lobby.proto"
protoc --proto_path="$PROTO_DIR" --csharp_out="$CLIENT_OUT"       "$PROTO_DIR/game.proto"

# internal.proto: LobbyServer + GameServer 전용 (클라이언트 제외)
protoc --proto_path="$PROTO_DIR" --csharp_out="$LOBBY_SERVER_OUT" "$PROTO_DIR/internal.proto"
protoc --proto_path="$PROTO_DIR" --csharp_out="$GAME_SERVER_OUT"  "$PROTO_DIR/internal.proto"

echo ""
echo "[gen.sh] 생성 완료"
echo "  로비서버: $LOBBY_SERVER_OUT"
echo "  게임서버: $GAME_SERVER_OUT"
echo "  클라:     $CLIENT_OUT"
