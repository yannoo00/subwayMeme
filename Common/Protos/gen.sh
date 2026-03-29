
PROTO_DIR="$(cd "$(dirname "$0")" && pwd)"
SERVER_OUT="$PROTO_DIR/../../Server/LobbyServer/Packet/Generated"
CLIENT_OUT="$PROTO_DIR/../../Client/Assets/Scripts/Network/Packets/Generated"

mkdir -p "$SERVER_OUT"
mkdir -p "$CLIENT_OUT"

protoc \
  --proto_path="$PROTO_DIR" \
  --csharp_out="$SERVER_OUT" \
  "$PROTO_DIR/lobby.proto"

protoc \
  --proto_path="$PROTO_DIR" \
  --csharp_out="$CLIENT_OUT" \
  "$PROTO_DIR/lobby.proto"

echo ""
echo "[gen.sh] 생성 완료"
echo "  서버: $SERVER_OUT"
echo "  클라: $CLIENT_OUT"
