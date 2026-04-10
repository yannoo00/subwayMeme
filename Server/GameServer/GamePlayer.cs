using GameProto;

namespace GameServer
{
    public class GamePlayer
    {
        public GameSession Session  { get; }
        public int  SessionId       => Session.SessionId;
        public int  PlayerId        { get; }
        public string PlayerName    { get; }
        public bool IsHost          { get; set; }
        public bool IsReady         { get; set; }
        public int  Hp              { get; set; } = 100;

        public GamePlayer(GameSession session, int playerId, string playerName, bool isHost)
        {
            Session    = session;
            PlayerId   = playerId;
            PlayerName = playerName;
            IsHost     = isHost;
        }

        public GamePlayerInfo ToProto() => new GamePlayerInfo
        {
            PlayerId   = PlayerId,
            PlayerName = PlayerName,
            IsHost     = IsHost,
        };
    }
}
