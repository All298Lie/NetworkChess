using Cysharp.Threading.Tasks;
using NetworkChess.Core;
using NetworkLibrary;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    #region 필드
    private Socket _tcpSocket;
    private Socket _udpSocket;
    private IPEndPoint _serverUdpEP;

    private readonly ConcurrentQueue<Action> workQueue = new ConcurrentQueue<Action>();
    private PacketHandler _handler = new PacketHandler();

    [SerializeField] private string ip = "127.0.0.1";
    [SerializeField] private int tcpPort = 7777;
    [SerializeField] private int udpPort = 7778;

    #endregion

    public string MyNickname { get; private set; }
    public string nicknameReq = string.Empty;

    #region 이벤트
    // 로그인 이벤트
    public static event Action<string> OnLoginFailed;

    // 방 생성/참가, 관전 관련 이벤트
    public static event Action<string> OnRoomFailed;
    public static event Action OnRoomCreateSuccess;
    public static event Action OnRoomJoinSuccess;

    // 방 나가기 이벤트
    public static event Action<RoomLeaveReason> OnRoomLeave;

    // 방 관전 이벤트
    public static event Action<S2C_RoomSpectateRes> OnRoomSpectateSuccess;

    // 매칭 완료 이벤트
    public static event Action<bool> OnMatchStarted;

    // 게임 상태 통보 이벤트
    public static event Action<S2C_GameStateNoti> OnGameStateNotified;

    // 게임 종료 이벤트
    public static event Action<string, string, string> OnGameOver;

    // 리플레이 이벤트
    public static event Action<S2C_ReplayRes> OnReplayReceived;
    public static event Action<S2C_FindReplayCodeRes> OnReplayCodeReceived;

    // 무승부/무르기 제안 이벤트
    public static event Action<bool, ProposalType?> OnSetProposalUI;
    public static event Action OnRemoveLastHistory;
    public static event Action<bool> OnCancelNetworkTimer;

    public static event Action<string, string, bool> OnChatReceived;
    #endregion

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
        InitializePacketHandler();
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

    #region PacketHandler 초기화 함수
    private void InitializePacketHandler()
    {
        this._handler.Register<S2C_LoginRes>((uint)PacketType.S2C_LoginRes, (res) => this.workQueue.Enqueue(() => HandleLoginRes(res)));

        this._handler.Register<S2C_RoomCreateRes>((uint)PacketType.S2C_RoomCreateRes, (res) => this.workQueue.Enqueue(() => HandleRoomCreateRes(res)));
        this._handler.Register<S2C_RoomJoinRes>((uint)PacketType.S2C_RoomJoinRes, (res) => this.workQueue.Enqueue(() => HandleRoomJoinRes(res)));
        this._handler.Register<S2C_RoomLeaveRes>((uint)PacketType.S2C_RoomLeaveRes, (res) => this.workQueue.Enqueue(() => HandleRoomLeaveRes(res)));
        this._handler.Register<S2C_RoomMatchNoti>((uint)PacketType.S2C_RoomMatchNoti, (noti) => this.workQueue.Enqueue(() => HandleRoomMatchNoti(noti).Forget()));
        this._handler.Register<S2C_RoomSpectateRes>((uint)PacketType.S2C_RoomSpectateRes, (res) => this.workQueue.Enqueue(() => HandleRoomSpectate(res)));

        this._handler.Register<S2C_GameMoveRes>((uint)PacketType.S2C_GameMoveRes, (res) => this.workQueue.Enqueue(() => HandleGameMoveRes(res)));
        this._handler.Register<S2C_GameStateNoti>((uint)PacketType.S2C_GameStateNoti, (noti) => this.workQueue.Enqueue(() => HandleGameStateNoti(noti)));
        this._handler.Register<S2C_GameOverNoti>((uint)PacketType.S2C_GameOverNoti, (noti) => this.workQueue.Enqueue(() => HandleGameOverNoti(noti)));

        this._handler.Register<S2C_ReplayRes>((uint)PacketType.S2C_ReplayRes, (res) => this.workQueue.Enqueue(() => HandleReplayRes(res)));
        this._handler.Register<S2C_FindReplayCodeRes>((uint)PacketType.S2C_FindReplayCodeRes, (res) => this.workQueue.Enqueue(() => HandleFindReplayCode(res)));

        this._handler.Register<S2C_ProposalNoti>((uint)PacketType.S2C_ProposalNoti, (noti) => this.workQueue.Enqueue(() => HandleProposalNoti(noti)));
        this._handler.Register<S2C_ProposalReplyNoti>((uint)PacketType.S2C_ProposalReplyNoti, (noti) => this.workQueue.Enqueue(() => HandleProposalReplyNoti(noti)));
        this._handler.Register<S2C_TakebackNoti>((uint)PacketType.S2C_TakebackNoti, (noti) => this.workQueue.Enqueue(() => HandleTakebackNoti(noti)));

        this._handler.Register<S2C_ChatNoti>((uint)PacketType.S2C_ChatNoti, (noti) => this.workQueue.Enqueue(() => HandleChat(noti)));
    }
    #endregion

    #region 비동기서버 연결 함수
    public async UniTask<bool> ConnectAsync()
    {
        try
        {
            if (this._tcpSocket != null && this._tcpSocket.Connected == true) return true;

            // 1. 소켓 생성
            this._tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await this._tcpSocket.ConnectAsync(this.ip, this.tcpPort).AsUniTask().Timeout(TimeSpan.FromSeconds(3));

            CLog.Log($"<color=green>[네트워크]</color> 서버({this.ip}:{this.tcpPort}) 연결 성공!");

            this._udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            this._serverUdpEP = await CreateEndPointAsync(this.ip, this.udpPort);

            // UDP를 수신받지 않으므로 Bind 필요 X

            ReceiveLoopAsync().Forget();
            HeartbeatLoopAsync().Forget();

            return true;
        }
        catch (TimeoutException) // 시간초과 될 경우
        {
            CLog.LogWarning($"<color=red>[네트워크]</color> 서버 연결 시간 초과(3초). 서버가 닫혀있을 수 있습니다.");

            this._tcpSocket?.Close();
            return false;
        }
        catch (Exception ex) // 그 외의 오류 발생 시
        {
            CLog.LogError($"<color=red>[네트워크]</color> 서버 연결 실패 에러 : {ex.Message}");

            this._tcpSocket?.Close();
            return false;
        }
    }
    #endregion

    #region 비동기 패킷 수신 함수
    private async UniTaskVoid ReceiveLoopAsync()
    {
        while (this._tcpSocket != null && this._tcpSocket.Connected == true)
        {
            try
            {
                string json = await PacketHelper.TcpReceiveAsync(this._tcpSocket);

                this._handler.Handle(json);
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

    #region 비동기 하트비트 송신 함수 
    public async UniTask HeartbeatLoopAsync()
    {
        while (this._tcpSocket != null && this._tcpSocket.Connected == true)
        {
            if (string.IsNullOrEmpty(this.MyNickname) == false && this._udpSocket != null)
            {
                C2S_HeartbeatReq req = new C2S_HeartbeatReq();
                req.Nickname = this.MyNickname;

                try
                {
                    await PacketHelper.UdpSendAsync(this._udpSocket, req, this._serverUdpEP);
                }
                catch (Exception ex)
                {
                    CLog.LogWarning($"[하트비트] 전송 실패 : {ex.Message}");
                }
            }

            // 3초마다 반복
            await UniTask.Delay(3 * 1_000); // 3초
        } // while 문
    }
    #endregion

    #region 패킷 송신 함수
    public async UniTask SendPacket<T>(T packet)
    {
        if (this._tcpSocket == null || this._tcpSocket.Connected == false)
        {
            CLog.LogWarning("[네트워크] 서버와 연결되어있지 않아 패킷을 보낼 수 없습니다.");
            return;
        }

        try
        {
            await PacketHelper.TcpSendAsync(this._tcpSocket, packet);

            CLog.Log($"[네트워크] 패킷 전송 완료 : {packet.GetType().Name}");
        }
        catch (Exception ex)
        {
            CLog.LogError($"[네트워크] <color=red>패킷 전송 실패 에러</color> : {ex.Message}");
        }
    }
    #endregion

    #region 연결 종료 처리하는 함수
    private void Disconnect()
    {
        // 1. UDP 소켓 리소스 정리
        if (this._udpSocket != null)
        {
            try
            {
                this._udpSocket.Close();
            }
            catch
            {
                // 이미 끊긴 경우 무시
            }
            finally
            {
                this._udpSocket = null;
            }
        }

        // 2. TCP 소켓 리소스 정리
        if (this._tcpSocket != null)
        {
            try
            {
                this._tcpSocket.Shutdown(SocketShutdown.Both);
                this._tcpSocket.Close();

                CLog.Log("[네트워크] 로그아웃");
            }
            catch
            {
                // 이미 끊긴 경우 무시
            }
            finally
            {
                this._tcpSocket = null;
            }
        }

        this.workQueue.Enqueue(() =>
        {
            GameData.Clear();

            CLog.LogWarning("<color=red>[네트워크]</color> 서버와의 연결이 끊어졌습니다. 타이틀로 돌아갑니다.");

            SceneManager.LoadScene("TitleScene");
        });
    }
    #endregion

    #region 비동기 EP 생성 함수
    private async UniTask<IPEndPoint> CreateEndPointAsync(string host, int port)
    {
        // 1. 일반 IP 주소인지 확인
        if (IPAddress.TryParse(host, out IPAddress ipAddress) == true)
        {
            return new IPEndPoint(ipAddress, port);
        }

        // 2. 도메인 조회
        try
        {
            IPAddress[] addresses = await Dns.GetHostAddressesAsync(host);

            foreach (IPAddress address in addresses)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    return new IPEndPoint(address, port);
                }
            }
        }
        catch (Exception ex)
        {
            CLog.LogError($"[DNS 에러] 주소를 찾을 수 없습니다. : {ex.Message}");
        }

        throw new Exception("유효한 IPv4 주소를 찾을 수 없습니다.");
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

            CLog.Log($"[방 참여] '{res.RoomOwnerNickname}'님 방 참가 완료.");
            OnRoomJoinSuccess?.Invoke();
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

            OnRoomLeave?.Invoke(res.LeaveReason);
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
        GameData.Clear();

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

    #region 6. 기물 이동 결과
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
        OnGameStateNotified?.Invoke(noti);
    }
    #endregion

    #region 8. 게임오버 통보
    private void HandleGameOverNoti(S2C_GameOverNoti noti)
    {
        OnGameOver?.Invoke(noti.Winner, noti.Reason, noti.ReplayCode);

        LocalCacheManager.Instance.SaveToRecentHistory(noti, this.MyNickname);
    }
    #endregion

    #region 9. 리플레이 결과
    private void HandleReplayRes(S2C_ReplayRes res)
    {
        OnReplayReceived?.Invoke(res);
    }
    #endregion

    #region 10. 리플레이 검색 결과
    private void HandleFindReplayCode(S2C_FindReplayCodeRes res)
    {
        OnReplayCodeReceived?.Invoke(res);
    }
    #endregion

    #region 11. 제안 요청 통보
    private void HandleProposalNoti(S2C_ProposalNoti noti)
    {
        if (noti.Sender == this.MyNickname)
        {
            // 네트워크 타이머 해제
            OnCancelNetworkTimer?.Invoke(false);
        }
        else if (GameData.IsSpectator == false)
        {
            // 수락/거절 버튼을 띄우기
            OnSetProposalUI?.Invoke(true, noti.ProposalType);
        }

        OnChatReceived?.Invoke("$System", noti.Message, false);
    }
    #endregion

    #region 12. 제안 응답 통보
    private void HandleProposalReplyNoti(S2C_ProposalReplyNoti noti)
    {
        OnSetProposalUI?.Invoke(false, null);
        OnCancelNetworkTimer?.Invoke(true);

        OnChatReceived?.Invoke("$System", noti.Message, false);
    }
    #endregion

    #region 13. 무르기 강제 동기화 통보
    private void HandleTakebackNoti(S2C_TakebackNoti noti)
    {
        OnSetProposalUI?.Invoke(false, null);
        OnCancelNetworkTimer?.Invoke(true);

        // 1. 코어 엔진 롤백 (이전 답변에서 추가한 RollbackState 함수 호출)
        GameManager.Instance.ActiveMode.RollbackState(noti.RestoredFEN);

        // 2. 리플레이 매니저 타임라인 갱신 (가장 마지막 수 날리기)
        ReplayManager.Instance.PopLatestMove();

        // 3. 기보 UI에서 맨 마지막 줄 삭제
        OnRemoveLastHistory?.Invoke();

        // 4. 보드판 하드 리셋 (홀로그램이 아닌 진짜 기물을 현재 코어 상태에 맞게 재배치!)
        BoardManager.Instance.HardResetBoard(GameManager.Instance.ActiveMode);

        // 5. 이전 수의 하이라이트로 변경
        if (MoveValidator.IsOnBoard(noti.StartPos) == true && MoveValidator.IsOnBoard(noti.EndPos) == true)
        {
            HighlightManager.Instance.UpdateLastMoveHighlight(noti.StartPos, noti.EndPos);
        }
        else
        {
            HighlightManager.Instance.HideMoveHighlights();
        }
    }
    #endregion

    #region 14. 방 관전 결과
    private void HandleRoomSpectate(S2C_RoomSpectateRes res)
    {
        if (res.IsSuccess == true)
        {
            CLog.Log($"[방 관전] '{res.RoomOwnerNickname}'님 방 관전 완료.");
            OnRoomSpectateSuccess?.Invoke(res);
        }
        else
        {
            CLog.LogWarning($"[네트워크] <color=red>방 입장 실패</color> : {res.Message}");
            OnRoomFailed?.Invoke(res.Message);
        }
    }
    #endregion

    #region 15. 채팅 통보
    private void HandleChat(S2C_ChatNoti noti)
    {
        OnChatReceived?.Invoke(noti.Sender, noti.Message, noti.IsSpectator);
    }
    #endregion

    #endregion - 패킷 처리 핸들러
}