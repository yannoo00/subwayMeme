using System.Collections.Generic;
using LobbyProto;
using UnityEngine;

// 로비 서버에서 오는 S_ 패킷 처리
// 항상 메인 스레드에서 호출됨 (UnitySynchronizationContext)
public static class LobbyPacketHandler
{
    // 접속 확인: PlayerId 저장 후 방 목록 화면으로 전환
    public static void Handle_S_Connected(byte[] body)
    {
        var pkt = S_Connected.Parser.ParseFrom(body);
        NetworkManager.Instance.MyPlayerId = pkt.PlayerId;

        Debug.Log($"[Lobby] S_Connected: myPlayerId={pkt.PlayerId}");

        MainMenuUIDocument.Instance?.ShowRoomListPanel();
    }

    // 방 생성 또는 참가 성공: 대기방 화면으로 전환
    public static void Handle_S_RoomCreated(byte[] body)
    {
        var pkt = S_RoomCreated.Parser.ParseFrom(body);

        Debug.Log($"[Lobby] S_RoomCreated: roomId={pkt.Room.RoomId}, name={pkt.Room.RoomName}");

        MainMenuUIDocument.Instance?.ShowWaitingRoom(pkt.Room);
    }

    // 방 목록 수신: 방 목록 UI 갱신
    public static void Handle_S_RoomList(byte[] body)
    {
        var pkt = S_RoomList.Parser.ParseFrom(body);

        Debug.Log($"[Lobby] S_RoomList: {pkt.Rooms.Count}개");

        MainMenuUIDocument.Instance?.RefreshRoomList(new List<RoomInfo>(pkt.Rooms));
    }

    // 다른 플레이어 입장: 대기방 플레이어 목록에 추가
    public static void Handle_S_PlayerJoined(byte[] body)
    {
        var pkt = S_PlayerJoined.Parser.ParseFrom(body);

        Debug.Log($"[Lobby] S_PlayerJoined: playerId={pkt.Player.PlayerId}, name={pkt.Player.PlayerName}");

        MainMenuUIDocument.Instance?.AddPlayerToWaitingRoom(pkt.Player);
    }

    // 다른 플레이어 퇴장: 대기방 플레이어 목록에서 제거
    public static void Handle_S_PlayerLeft(byte[] body)
    {
        var pkt = S_PlayerLeft.Parser.ParseFrom(body);

        Debug.Log($"[Lobby] S_PlayerLeft: playerId={pkt.PlayerId}");

        MainMenuUIDocument.Instance?.RemovePlayerFromWaitingRoom(pkt.PlayerId);
    }

    // 방장 변경: 내가 새 방장이면 게임 시작 버튼 활성화
    public static void Handle_S_CreatorChanged(byte[] body)
    {
        var pkt = S_CreatorChanged.Parser.ParseFrom(body);

        Debug.Log($"[Lobby] S_CreatorChanged: newCreatorId={pkt.NewCreatorId}");

        MainMenuUIDocument.Instance?.UpdateCreatorStatus(pkt.NewCreatorId);
    }

    // 게임 서버 준비 완료: 포트 저장 후 씬 로드 → 게임 서버 접속
    public static void Handle_S_GameReady(byte[] body)
    {
        var pkt = S_GameReady.Parser.ParseFrom(body);

        Debug.Log($"[Lobby] S_GameReady: port={pkt.Port}, roomId={pkt.RoomId}");

        NetworkManager.Instance.GameServerPort = pkt.Port;
        MainMenuUIDocument.Instance?.OnGameReadyReceived();
    }

    // 서버 에러 수신
    public static void Handle_S_Error(byte[] body)
    {
        var pkt = S_Error.Parser.ParseFrom(body);

        Debug.LogWarning($"[Lobby] S_Error: code={pkt.Code}, msg={pkt.Message}");

        // TODO: 에러 메시지 UI 팝업
    }
}
