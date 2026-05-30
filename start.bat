@echo off

echo [1/4] GameServer...
start "GameServer" cmd /k dotnet run --project Server\GameServer

echo Waiting 2s for GameServer to bind...
timeout /t 2 /nobreak > nul

echo [2/4] LobbyServer...
start "LobbyServer" cmd /k dotnet run --project Server\LobbyServer

echo [3/4] websockify lobby  8770 -^> 7770 ...
start "websockify-lobby" cmd /k python -m websockify 8770 127.0.0.1:7770

echo [4/4] websockify game   8771 -^> 7771 ...
start "websockify-game" cmd /k python -m websockify 8771 127.0.0.1:7771

echo Done.
