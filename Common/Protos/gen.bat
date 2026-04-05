
@echo off
set PROTO_DIR=D:\Unity\subwayMeme\Common\Protos
set LOBBY_SERVER_OUT=D:\Unity\subwayMeme\Server\LobbyServer\Packet\Generated
set GAME_SERVER_OUT=D:\Unity\subwayMeme\Server\GameServer\Packet\Generated
set CLIENT_OUT=D:\Unity\subwayMeme\Client\Assets\Scripts\Network\Packets\Generated

if not exist "%LOBBY_SERVER_OUT%" mkdir "%LOBBY_SERVER_OUT%"
if not exist "%GAME_SERVER_OUT%" mkdir "%GAME_SERVER_OUT%"
if not exist "%CLIENT_OUT%" mkdir "%CLIENT_OUT%"

protoc --proto_path="%PROTO_DIR%" --csharp_out="%LOBBY_SERVER_OUT%" "%PROTO_DIR%\lobby.proto"
protoc --proto_path="%PROTO_DIR%" --csharp_out="%GAME_SERVER_OUT%" "%PROTO_DIR%\game.proto"
protoc --proto_path="%PROTO_DIR%" --csharp_out="%CLIENT_OUT%" "%PROTO_DIR%\lobby.proto"
protoc --proto_path="%PROTO_DIR%" --csharp_out="%CLIENT_OUT%" "%PROTO_DIR%\game.proto"

echo Done.
