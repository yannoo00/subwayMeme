
@echo off
set PROTO_DIR=D:\Unity\subwayMeme\Common\Protos
set SERVER_OUT=D:\Unity\subwayMeme\Server\LobbyServer\Packet\Generated
set CLIENT_OUT=D:\Unity\subwayMeme\Client\Assets\Scripts\Network\Packets\Generated

if not exist "%SERVER_OUT%" mkdir "%SERVER_OUT%"
if not exist "%CLIENT_OUT%" mkdir "%CLIENT_OUT%"

protoc --proto_path="%PROTO_DIR%" --csharp_out="%SERVER_OUT%" "%PROTO_DIR%\lobby.proto"
protoc --proto_path="%PROTO_DIR%" --csharp_out="%CLIENT_OUT%" "%PROTO_DIR%\lobby.proto"

echo Done.
