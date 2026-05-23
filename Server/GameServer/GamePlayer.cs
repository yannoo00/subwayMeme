using GameProto;

namespace GameServer
{
    public class GamePlayer
    {
        public GameSession Session  { get; }
        public int  SessionId       => Session.SessionId;
        public int  PlayerId        { get; }
        public string PlayerName    { get; }
        // 클라가 C_EnterGame 으로 보낸 선택 캐릭터 ID. 다른 클라들이 모델을 인스턴시에이션할 때 사용
        public int  CharacterId     { get; }
        public bool IsHost          { get; set; }
        public bool IsReady         { get; set; }
        public int  Hp              { get; set; } = 100;

        public GamePlayer(GameSession session, int playerId, string playerName, int characterId, bool isHost)
        {
            Session     = session;
            PlayerId    = playerId;
            PlayerName  = playerName;
            CharacterId = characterId;
            IsHost      = isHost;
        }

        public GamePlayerInfo ToProto() => new GamePlayerInfo
        {
            PlayerId    = PlayerId,
            PlayerName  = PlayerName,
            IsHost      = IsHost,
            CharacterId = CharacterId,
        };
    }
}
