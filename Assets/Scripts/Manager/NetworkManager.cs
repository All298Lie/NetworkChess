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

    public static event Action<string> OnLoginFailed;

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

    #region + private 접근제한자 함수

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
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[네트워크 수신 종료] {ex.Message}");
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
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[네트워크] 소켓 종료 중 에러 : {ex.Message}");
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
            Debug.Log($"<color=green>[로그인 성공]</color> {res.Message}");

            // 1. 닉네임 저장
            this.MyNickname = res.Nickname;

            // 2. 로비 씬으로 이동
            SceneManager.LoadScene("LobbyScene");
        }
        else
        {
            Debug.LogWarning($"<color=red>[로그인 실패]</color> {res.Message}");

            // 1. 실패 메세지를 이벤트를 통해 전송
            OnLoginFailed?.Invoke(res.Message);
        }
    }
    #endregion

    #endregion -- 패킷 처리 핸들러

    #endregion - private 접근제한자 함수

    #region + public 접근제한자 함수

    #region 비동기 서버 연결 함수
    public async UniTask ConnectToServerAsync(string ip, int port)
    {
        try
        {
            // 1. 소켓 생성
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // 2. 서버로 연결
            await clientSocket.ConnectAsync(ip, port).AsUniTask();
            Debug.Log($"[네트워크] 서버({ip}:{port}) 연결 성공!");

            // 3. 패킷을 전송 받을 수 있도록 설정
            ReceiveLoopAsync().Forget();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[네트워크 에러] 서버 연결 실패 : {ex.Message}");
        }
    }
    #endregion

    #region 패킷 송신 함수
    public async UniTask SendPacket<T>(T packet)
    {
        if (clientSocket == null || clientSocket.Connected == false)
        {
            Debug.LogWarning("[네트워크] 서버와 연결되어있지 않아 패킷을 보낼 수 없습니다.");
            return;
        }

        try
        {
            // 1. PacketHelper를 통해 JSON 직렬화 및 프레이밍
            byte[] sendData = PacketHelper.SerializeAndFrame(packet);

            // 2. 서버로 전송
            await clientSocket.SendAsync(new ArraySegment<byte>(sendData), SocketFlags.None).AsUniTask();
            Debug.Log($"[네트워크] 패킷 전송 완료 : {packet.GetType().Name}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[네트워크 에러] 패킷 전송 실패 : {ex.Message}");
        }
    }
    #endregion

    #endregion - public 접근제한자 함수
}
