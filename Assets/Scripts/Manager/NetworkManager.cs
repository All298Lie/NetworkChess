using Cysharp.Threading.Tasks;
using NetworkChess.Core;
using Newtonsoft.Json;
using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }
    public Socket clientSocket;

    public string MyNickname { get; private set; }
    public string nicknameReq = string.Empty;

    public string CurrentRoomId { get; private set; } = string.Empty;

    // 로그인 이벤트
    public static event Action<string> OnLoginFailed;

    // 방 생성/참가, 관전 관련 이벤트
    public static event Action<string> OnRoomFailed;
    public static event Action OnRoomCreateSuccess;
    public static event Action OnRoomJoinSuccess;

    // 방 나가기 이벤트
    public static event Action OnRoomLeave;

    // 방 관전 이벤트
    public static event Action OnRoomSpectateSuccess;

    // 매칭 완료 이벤트
    public static event Action<bool, GameMode, string> OnMatchStarted;

    #region + 유니티 함수

    #region Awake 함수
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region Start 함수
    void Start()
    {
        // 서버 연결
        ConnectAndInitializeAsync().Forget();
    }
    #endregion

    #region OnDestroy 함수
    void OnDestroy()
    {
        CloseSocket();
    }
    #endregion

    #endregion - 유니티 함수

    #region 정확한 바이트 수신 함수
    private async UniTask<int> ReceiveExactAsync(byte[] buffer, int size)
    {
        int totalRead = 0;
        while (totalRead < size)
        {
            int read = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer, totalRead, size - totalRead), SocketFlags.None).AsUniTask();

            if (read == 0) return 0;

            totalRead += read;
        }

        return totalRead;
    }
    #endregion

    #region 비동기서버 연결 함수
    private async UniTaskVoid ConnectAndInitializeAsync()
    {
        await ConnectToServerAsync("127.0.0.1", 7777);
    }
    #endregion

    #region 비동기 패킷 수신 함수
    private async UniTaskVoid ReceiveLoopAsync()
    {
        byte[] headerBuffer = new byte[4];

        while (clientSocket != null && clientSocket.Connected == true)
        {
            try
            {
                // 1. header 데이터(4바이트) 수신
                int headerRead = await ReceiveExactAsync(headerBuffer, 4);
                if (headerRead == 0) break;

                int payloadLength = BitConverter.ToInt32(headerBuffer, 0);
                byte[] payloadBuffer = new byte[payloadLength];

                // 2.payload 데이터 수신
                int payloadRead = await ReceiveExactAsync(payloadBuffer, payloadLength);
                if (payloadRead == 0) break;

                // 3. JSON 문자열로 디코딩
                string jsonPayload = Encoding.UTF8.GetString(payloadBuffer);

                // 4. 부모 클래스 형태로 타입 확인
                BasePacket? basePacket = JsonConvert.DeserializeObject<BasePacket>(jsonPayload);
                if (basePacket == null) continue;

                await UniTask.SwitchToMainThread(); // 소켓 통신은 백그라운드 스레드에서, 유니티 UI 작업은 메인스레드에서


                // 5. 패킷 라우터
                switch (basePacket.Type)
                {
                    case PacketType.S2C_LoginRes:
                        S2C_LoginRes loginRes = JsonConvert.DeserializeObject<S2C_LoginRes>(jsonPayload);
                        HandleLoginRes(loginRes);
                        break;

                    case PacketType.S2C_RoomCreateRes:
                        S2C_RoomCreateRes createRes = JsonConvert.DeserializeObject<S2C_RoomCreateRes>(jsonPayload);
                        HandleRoomCreateRes(createRes);
                        break;

                    case PacketType.S2C_RoomJoinRes:
                        S2C_RoomJoinRes roomJoinRes = JsonConvert.DeserializeObject<S2C_RoomJoinRes>(jsonPayload);
                        HandleRoomJoinRes(roomJoinRes);
                        break;

                    case PacketType.S2C_RoomLeaveRes:
                        S2C_RoomLeaveRes roomLeaveRes = JsonConvert.DeserializeObject<S2C_RoomLeaveRes>(jsonPayload);
                        HandleRoomLeaveRes(roomLeaveRes);
                        break;

                    case PacketType.S2C_RoomMatchNoti:
                        S2C_RoomMatchNoti matchNoti = JsonConvert.DeserializeObject<S2C_RoomMatchNoti>(jsonPayload);
                        HandleRoomMatchNoti(matchNoti);
                        break;
                }

                // await UniTask.SwitchToThreadPool(); // 소켓 통신은 백그라운드 스레드에서, 유니티 UI 작업은 메인스레드에서
            }
            catch (Exception ex)
            {
                CLog.LogWarning($"[네트워크] 수신 종료 : {ex.Message}");
                break;
            }
        } // while 문
    }
    #endregion

    #region 소켓을 닫아주는 함수
    private void CloseSocket()
    {
        if (clientSocket != null && clientSocket.Connected == true)
        {
            try
            {
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();

                CLog.Log("[네트워크] 로그아웃");
            }
            catch (Exception ex)
            {
                CLog.LogWarning($"[네트워크] <color=red>소켓 종료 중 에러</color> : {ex.Message}");
            }
        }
    }
    #endregion

    #region ++ 패킷 처리 핸들러

    #region 1. 로그인 결과
    private void HandleLoginRes(S2C_LoginRes res)
    {
        if (res.IsSuccess == true)
        {
            CLog.Log($"[네트워크] <color=green>로그인 성공</color> : {res.Message}");

            // 1. 닉네임 저장
            this.MyNickname = res.Nickname;

            // 2. 로비 씬으로 이동
            SceneManager.LoadScene("LobbyScene");
        }
        else
        {
            CLog.LogWarning($"[네트워크] <color=red>로그인 실패</color> : {res.Message}");

            // 1. 실패 메세지를 이벤트를 통해 전송
            OnLoginFailed?.Invoke(res.Message);
        }
    }
    #endregion

    #region 2. 방 생성 결과
    private void HandleRoomCreateRes(S2C_RoomCreateRes res)
    {
        if (res.IsSuccess == true)
        {
            CLog.Log($"[네트워크] <color=green>방 생성 성공</color> : {res.Message}");

            // 1. 네트워크 매니저가 자신의 상태를 먼저 갱신
            this.CurrentRoomId = this.MyNickname;

            // 2. 이벤트 발생
            CLog.Log("[방 생성] 생성 완료. 대기 모드로 전환");
            OnRoomCreateSuccess?.Invoke();
        }
        else
        {
            OnRoomFailed?.Invoke(res.Message);
        }
    }
    #endregion

    #region 3. 방 참가 결과
    private void HandleRoomJoinRes(S2C_RoomJoinRes res)
    {
        if (res.IsSuccess == true)
        {
            CLog.Log($"[네트워크] <color=green>방 입장 성공</color> : {res.Message}");

            // 1. 네트워크 매니저가 자신의 상태를 먼저 갱신
            this.CurrentRoomId = res.RoomId;

            // 2. 이벤트 발생
            if (res.IsSpectator == false)
            {
                CLog.Log($"[방 참여] '{res.RoomId}'님 방 참가 완료.");
                OnRoomJoinSuccess?.Invoke();
            }
            else
            {
                CLog.Log($"[방 관전] '{res.RoomId}'님 방 관전 완료.");
                OnRoomSpectateSuccess?.Invoke();
            }
        }
        else
        {
            CLog.LogWarning($"[네트워크] <color=red>방 입장 실패</color> : {res.Message}");
            OnRoomFailed?.Invoke(res.Message);
        }
    }
    #endregion

    #region 4. 방 나가기 결과
    private void HandleRoomLeaveRes(S2C_RoomLeaveRes res)
    {
        if (res.IsSuccess == true)
        {
            CLog.Log($"[네트워크] <color=green>방 나가기 성공</color> : {res.Message}");

            OnRoomLeave?.Invoke();
        }
        else
        {
            CLog.LogWarning($"[네트워크] <color=red>방 나가기 실패</color> : {res.Message}");
        }
    }
    #endregion

    #region 5. 방 매칭 완료 통보
    private void HandleRoomMatchNoti(S2C_RoomMatchNoti noti)
    {
        CLog.Log($"[매칭] {noti.WhitePlayerNickname}(백) VS {noti.BlackPlayerNickname}(흑)");

        // 1. 내 진영 확인
        bool isWhite = (noti.WhitePlayerNickname == this.MyNickname);

        // 2. 이벤트 발생
        OnMatchStarted?.Invoke(isWhite, noti.GameMode, noti.StartingFEN);
    }
    #endregion

    #endregion -- 패킷 처리 핸들러

    #region 비동기 서버 연결 함수
    public async UniTask ConnectToServerAsync(string ip, int port)
    {
        try
        {
            // 1. 소켓 생성
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // 2. 서버로 연결
            await clientSocket.ConnectAsync(ip, port).AsUniTask();
            CLog.Log($"[네트워크] 서버({ip}:{port}) 연결 성공!");

            // 3. 패킷을 전송 받을 수 있도록 설정
            ReceiveLoopAsync().Forget();
        }
        catch (Exception ex)
        {
            CLog.LogError($"[네트워크] <color=red>서버 연결 실패 에러</color> : {ex.Message}");
        }
    }
    #endregion

    #region 패킷 송신 함수
    public async UniTask SendPacket<T>(T packet)
    {
        if (clientSocket == null || clientSocket.Connected == false)
        {
            CLog.LogWarning("[네트워크] 서버와 연결되어있지 않아 패킷을 보낼 수 없습니다.");
            return;
        }

        try
        {
            // 1. PacketHelper를 통해 JSON 직렬화 및 프레이밍
            byte[] sendData = PacketHelper.SerializeAndFrame(packet);

            // 2. 서버로 전송
            await clientSocket.SendAsync(new ArraySegment<byte>(sendData), SocketFlags.None).AsUniTask();
            CLog.Log($"[네트워크] 패킷 전송 완료 : {packet.GetType().Name}");
        }
        catch (Exception ex)
        {
            CLog.LogError($"[네트워크] <color=red>패킷 전송 실패 에러</color> : {ex.Message}");
        }
    }
    #endregion
}
