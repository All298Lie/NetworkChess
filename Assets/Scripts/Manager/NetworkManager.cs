using Cysharp.Threading.Tasks;
using NetworkChess.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }
    public Socket clientSocket;

    private readonly ConcurrentQueue<Action> workQueue = new ConcurrentQueue<Action>();

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
    public static event Action<bool> OnMatchStarted;

    // 게임 종료 이벤트
    public static event Action<string, string> OnGameOver;

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

    #region Update 함수
    void Update()
    {
        while (this.workQueue.TryDequeue(out Action work))
        {
            work?.Invoke();
        }
    }
    #endregion

    #region OnDestroy 함수
    void OnDestroy()
    {
        Disconnect();
    }
    #endregion

    #region OnApplicationQuit 함수
    void OnApplicationQuit()
    {
        Disconnect();
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
    public async UniTask<bool> ConnectAsync()
    {
        bool isSuccess = await ConnectToServerAsync("127.0.0.1", 7777);

        return isSuccess;
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
                BasePacket basePacket = JsonConvert.DeserializeObject<BasePacket>(jsonPayload);
                if (basePacket == null) continue;

                // 5. 패킷 라우터
                switch (basePacket.Type)
                {
                    case PacketType.S2C_LoginRes:
                        S2C_LoginRes loginRes = JsonConvert.DeserializeObject<S2C_LoginRes>(jsonPayload);
                        this.workQueue.Enqueue(() => { HandleLoginRes(loginRes); });
                        break;

                    case PacketType.S2C_RoomCreateRes:
                        S2C_RoomCreateRes createRes = JsonConvert.DeserializeObject<S2C_RoomCreateRes>(jsonPayload);
                        this.workQueue.Enqueue(() => { HandleRoomCreateRes(createRes); });
                        break;

                    case PacketType.S2C_RoomJoinRes:
                        S2C_RoomJoinRes roomJoinRes = JsonConvert.DeserializeObject<S2C_RoomJoinRes>(jsonPayload);
                        this.workQueue.Enqueue(() => { HandleRoomJoinRes(roomJoinRes); });
                        break;

                    case PacketType.S2C_RoomLeaveRes:
                        S2C_RoomLeaveRes roomLeaveRes = JsonConvert.DeserializeObject<S2C_RoomLeaveRes>(jsonPayload);
                        this.workQueue.Enqueue(() => { HandleRoomLeaveRes(roomLeaveRes); });
                        break;

                    case PacketType.S2C_RoomMatchNoti:
                        S2C_RoomMatchNoti matchNoti = JsonConvert.DeserializeObject<S2C_RoomMatchNoti>(jsonPayload);
                        this.workQueue.Enqueue(() => { HandleRoomMatchNoti(matchNoti).Forget(); });
                        break;

                    case PacketType.S2C_GameMoveRes:
                        S2C_GameMoveRes moveRes = JsonConvert.DeserializeObject<S2C_GameMoveRes>(jsonPayload);
                        this.workQueue.Enqueue(() => { HandleGameMoveRes(moveRes); });
                        break;

                    case PacketType.S2C_GameStateNoti:
                        S2C_GameStateNoti stateNoti = JsonConvert.DeserializeObject<S2C_GameStateNoti>(jsonPayload);
                        this.workQueue.Enqueue(() => { HandleGameStateNoti(stateNoti); });
                        break;

                    case PacketType.S2C_GameOverNoti:
                        S2C_GameOverNoti gameOverNoti = JsonConvert.DeserializeObject<S2C_GameOverNoti>(jsonPayload);
                        this.workQueue.Enqueue(() => { HandleGameOverNoti(gameOverNoti); });
                        break;

                    default:
                        CLog.LogError($"<color=red>[네트워크]</color> 에러 : 등록되지 않은 패킷이 요청되어 무시되었습니다. {basePacket.Type}");
                        break;
                }
            }
            catch (SocketException ex)
            {
                CLog.LogError($"<color=red>[네트워크]</color> 서버와의 연결이 끊어졌습니다 : {ex.Message}");
                Disconnect();
                break;
            }
            catch (Exception ex)
            {
                CLog.LogError($"<color=red>[네트워크]</color> 수신 에러 : {ex.Message}");
                Disconnect();
                break;
            }
        } // while 문
    }
    #endregion

    #region 연결 종료 처리하는 함수
    private void Disconnect()
    {
        if (this.clientSocket != null)
        {
            try
            {
                this.clientSocket.Shutdown(SocketShutdown.Both);
                this.clientSocket.Close();

                CLog.Log("[네트워크] 로그아웃");
            }
            catch
            {
                // 이미 끊긴 경우 무시
            }
            finally
            {
                this.clientSocket = null;
            }
        }

        this.workQueue.Enqueue(() =>
        {
            GameData.Clear();

            if (SceneManager.GetActiveScene().name != "TitleScene")
            {
                CLog.LogWarning("<color=red>[네트워크]</color> 서버와의 연결이 끊어졌습니다. 타이틀로 돌아갑니다.");

                SceneManager.LoadScene("TitleScene");
            }
        });
    }
    #endregion

    #region + 패킷 처리 핸들러

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
                GameData.IsSpectator = true;
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
            CLog.Log($"<color=green>[네트워크]</color> 방 나가기 성공 : {res.Message}");

            GameData.Clear();

            OnRoomLeave?.Invoke();
        }
        else
        {
            CLog.LogWarning($"<color=red>[네트워크]</color> 방 나가기 실패 : {res.Message}");
        }
    }
    #endregion

    #region 5. 방 매칭 완료 통보
    private async UniTaskVoid HandleRoomMatchNoti(S2C_RoomMatchNoti noti)
    {
        CLog.Log($"[매칭] {noti.WhitePlayerNickname}(백) VS {noti.BlackPlayerNickname}(흑)");

        // 1. 내 진영 확인
        bool isWhite = (noti.WhitePlayerNickname == this.MyNickname);
        string opponentNickname = (isWhite == true) ? noti.BlackPlayerNickname : noti.WhitePlayerNickname;

        // 2. 인게임 데이터 준비
        GameData.IsWhite = isWhite;
        GameData.CurrentMode = noti.GameMode;
        GameData.StartingFEN = (string.IsNullOrEmpty(noti.StartingFEN) == true) ? "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR" : noti.StartingFEN;
        GameData.OpponentNickname = opponentNickname;

        // 3. 이벤트 발생
        OnMatchStarted?.Invoke(isWhite);

        // 4. 플레이어가 로비에서 UI를 확인할 시간을 부여
        await UniTask.Delay(2 * 1_000); // 2초

        // 5. 인게임 씬으로 이동
        SceneManager.LoadScene("GameScene");
    }
    #endregion

    #region 6. 기물 이동 결과 통보
    private void HandleGameMoveRes(S2C_GameMoveRes res)
    {
        if (res.IsSuccess == true) return;

        CLog.LogWarning("[서버 이동 거부] 보드판을 강제 동기화합니다.");

        GameManager.Instance.ActiveMode.InitializeBoard(res.RollbackFEN);
        BoardManager.Instance.HardResetBoard(GameManager.Instance.ActiveMode);
    }
    #endregion

    #region 7. 게임 상태 통보
    private void HandleGameStateNoti(S2C_GameStateNoti noti)
    {
        bool didIMove = (GameData.IsWhite != noti.IsWhiteTurn) && (GameData.IsSpectator == false);

        if (didIMove == false)
        {
            CorePiece movedPiece = GameManager.Instance.ActiveMode.Board[noti.StartPos.x, noti.StartPos.y];

            if (movedPiece != null)
            {
                GameManager.Instance.ActiveMode.HandlePieceMoveRequest(movedPiece, noti.EndPos, noti.PromotionType);
            }
        }

        GameManager.Instance.ActiveMode.IsWhiteTurn = noti.IsWhiteTurn;
        BoardManager.Instance.SyncVisualsWithCore(GameManager.Instance.ActiveMode);

        HighlightManager.Instance.UpdateLastMoveHighlight(noti.StartPos, noti.EndPos);

        CLog.Log($"[기물 이동] {noti.StartPos} -> {noti.EndPos} / 다음 턴 : {(noti.IsWhiteTurn == true ? "백" : "흑")}");
    }
    #endregion

    #region 8. 게임오버 통보
    private void HandleGameOverNoti(S2C_GameOverNoti noti)
    {
        OnGameOver?.Invoke(noti.Winner, noti.Reason);
    }
    #endregion

    #endregion - 패킷 처리 핸들러

    #region 비동기 서버 연결 함수
    private async UniTask<bool> ConnectToServerAsync(string ip, int port)
    {
        try
        {
            // 1. 이미 연결이 되어있는지 확인
            if (this.clientSocket != null && this.clientSocket.Connected == true) return true;

            // 2. 소켓 생성
            this.clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // 3. 서버로 연결
            await this.clientSocket.ConnectAsync(ip, port).AsUniTask().Timeout(TimeSpan.FromSeconds(3));

            CLog.Log($"<color=green>[네트워크]</color> 서버({ip}:{port}) 연결 성공!");

            // 4. 패킷을 전송 받을 수 있도록 설정
            ReceiveLoopAsync().Forget();

            return true;
        }
        catch (TimeoutException) // 시간초과 될 경우
        {
            CLog.LogWarning($"<color=red>[네트워크]</color> 서버 연결 시간 초과(3초). 서버가 닫혀있을 수 있습니다.");

            this.clientSocket?.Close();
            return false;
        }
        catch (Exception ex) // 그 외의 오류 발생 시
        {
            CLog.LogError($"<color=red>[네트워크]</color> 서버 연결 실패 에러 : {ex.Message}");

            this.clientSocket?.Close();
            return false;
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
