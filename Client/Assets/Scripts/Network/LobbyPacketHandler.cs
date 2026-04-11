using LobbyProto;
using UnityEngine;

// 로비 서버에서 오는 S_ 패킷 처리
// 항상 메인 스레드에서 호출됨 (UnitySynchronizationContext)
public static class LobbyPacketHandler
{
    // 접속 확인: 서버가 발급한 PlayerId를 로컬에 저장
    public static void Handle_S_Connected(byte[] body)
    {
        var pkt = S_Connected.Parser.ParseFrom(body);
        NetworkManager.Instance.MyPlayerId = pkt.PlayerId;

        Debug.Log($"[Lobby] S_Connected: myPlayerId={pkt.PlayerId}");

        // TODO: 로비 UI 초기화 (방 목록 화면 표시 등)
    }

    // 방 생성 또는 방 참가 성공: 대기방 UI로 전환
    public static void Handle_S_RoomCreated(byte[] body)
    {
        var pkt = S_RoomCreated.Parser.ParseFrom(body);

        Debug.Log($"[Lobby] S_RoomCreated: roomId={pkt.Room.RoomId}, name={pkt.Room.RoomName}");

        // TODO: 대기방 UI 표시 (방 이름, 현재 인원, 시작 버튼 등)
    }

    // 방 목록 수신: 방 목록 UI 갱신
    public static void Handle_S_RoomList(byte[] body)
    {
        var pkt = S_RoomList.Parser.ParseFrom(body);

        Debug.Log($"[Lobby] S_RoomList: {pkt.Rooms.Count}개");

        // TODO: 방 목록 UI 갱신 (각 방의 이름, 인원, 참가 버튼)
    }

    // 다른 플레이어 입장: 대기방 플레이어 목록 추가
    public static void Handle_S_PlayerJoined(byte[] body)
    {
        var pkt = S_PlayerJoined.Parser.ParseFrom(body);

        Debug.Log($"[Lobby] S_PlayerJoined: playerId={pkt.Player.PlayerId}, name={pkt.Player.PlayerName}");

        // TODO: 대기방 플레이어 목록 UI에 항목 추가
    }

    // 다른 플레이어 퇴장: 대기방 플레이어 목록 제거
    public static void Handle_S_PlayerLeft(byte[] body)
    {
        var pkt = S_PlayerLeft.Parser.ParseFrom(body);

        Debug.Log($"[Lobby] S_PlayerLeft: playerId={pkt.PlayerId}");

        // TODO: 대기방 플레이어 목록 UI에서 항목 제거
    }

    // 방장 변경: 내가 새 방장인지 확인 후 시작 버튼 활성화
    public static void Handle_S_CreatorChanged(byte[] body)
    {
        var pkt = S_CreatorChanged.Parser.ParseFrom(body);

        Debug.Log($"[Lobby] S_CreatorChanged: newCreatorId={pkt.NewCreatorId}");

        // TODO: 내가 새 방장이면 시작 버튼 활성화
        // bool iAmCreator = pkt.NewCreatorId == NetworkManager.Instance.MyPlayerId;
    }

    // 게임 서버 준비 완료: 포트 저장 후 씬 로드, 로드 완료 시 게임 서버 접속
    public static void Handle_S_GameReady(byte[] body)
    {
        var pkt = S_GameReady.Parser.ParseFrom(body);

        Debug.Log($"[Lobby] S_GameReady: port={pkt.Port}, roomId={pkt.RoomId}");

        NetworkManager.Instance.GameServerPort = pkt.Port;

        // 씬 로드 완료 콜백에서 게임 서버 접속
        // LoadStation/LoadSubway 중 멀티플레이 시작 씬으로 변경 필요
        // TODO: SceneLoader에 게임 씬 추가 후 아래 연결
        // SceneLoader.Instance.LoadGame(onLoaded: async () =>
        //     await NetworkManager.Instance.ConnectToGameAsync());
    }

    // 서버 에러 수신: 로그 출력 및 UI 알림
    public static void Handle_S_Error(byte[] body)
    {
        var pkt = S_Error.Parser.ParseFrom(body);

        Debug.LogWarning($"[Lobby] S_Error: code={pkt.Code}, msg={pkt.Message}");

        // TODO: 에러 메시지 UI 팝업 표시
    }
}
