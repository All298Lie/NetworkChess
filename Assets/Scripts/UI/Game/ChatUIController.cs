using Cysharp.Threading.Tasks;
using NetworkChess.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ChatUIController : MonoBehaviour
{
    [SerializeField] private GameObject chatMessagePrefab;

    [SerializeField] private ScrollRect chatScrollRect;
    [SerializeField] private TMP_InputField input;

    [SerializeField] private Transform content;

    [SerializeField] private InputAction enterAction;

    void Start()
    {
        if (GameData.IsReplay == true)
        {
            gameObject.SetActive(false);

            return;
        }

        this.input.gameObject.SetActive(false);

        this.input.onSubmit.AddListener((text) => OnSubmitChat(text).Forget());
        this.input.onEndEdit.AddListener((text) => OnEndEditChat(text));


        if (NetworkManager.Instance != null)
        {
            NetworkManager.OnChatReceived += ReceiveChatPacket;
        }
    }

    void OnEnable()
    {
        this.enterAction.Enable();
        this.enterAction.performed += OnEnterKeyPressed;
    }

    void OnDisable()
    {
        this.enterAction.Disable();
        this.enterAction.performed -= OnEnterKeyPressed;
    }

    void OnDestroy()
    {

        if (NetworkManager.Instance != null)
        {
            NetworkManager.OnChatReceived -= ReceiveChatPacket;
        }
    }

    #region 채팅창 열기 (엔터 키)
    private async UniTaskVoid OpenChatInputAsync()
    {
        await UniTask.WaitForEndOfFrame(this);

        this.input.gameObject.SetActive(true);
        this.input.text = "";

        this.input.ActivateInputField();
        EventSystem.current.SetSelectedGameObject(this.input.gameObject);
    }
    #endregion

    #region 채팅 전송 (엔터 키로 완료 시)
    private async UniTaskVoid OnSubmitChat(string text)
    {
        // 1. 채팅이 작성되었는지 확인
        if (string.IsNullOrWhiteSpace(text) == false)
        {
            C2S_ChatReq req = new C2S_ChatReq();
            
            req.Message = text;

            await NetworkManager.Instance.SendPacket(req);

            CLog.Log($"[채팅 전송] {text}");
        }

        // 2. 입력창 닫기
        CloseChatInput();
    }
    #endregion

    #region 채팅 취소 (마우스 클릭 등으로 포커스를 잃었을 때)
    private void OnEndEditChat(string text)
    {
        if (this.input.gameObject.activeSelf == true)
        {
            CloseChatInput();
        }
    }
    #endregion

    #region 채팅창 강제 닫기 및 초기화
    private void CloseChatInput()
    {
        this.input.text = "";
        this.input.DeactivateInputField();
        this.input.gameObject.SetActive(false);
    }
    #endregion

    #region 서버로부터 채팅 패킷 수신 시 호출될 함수
    public void ReceiveChatPacket(string senderName, string message, bool isSpectator)
    {
        // 1. 채팅 프리팹 생성 및 데이터 세팅
        GameObject newChat = Instantiate(this.chatMessagePrefab, this.content);
        TMP_Text chatText = newChat.GetComponent<TMP_Text>();

        if (senderName == "$System")
        {
            
            chatText.text = $"[<color=#38BDF8>시스템</color>] {message}";
        }
        else
        {
            chatText.text = $"{(isSpectator == true ? "[관전]" : "")}[<color=#A0AEC0>{senderName}</color>] {message}";
        }

        // 2. 채팅을 추가한 후, 스크롤을 맨 아래로 내림
        UpdateChatScrollAsync().Forget();
    }
    #endregion

    #region UI 스크롤을 맨 아래로 내리는 비동기 함수
    private async UniTaskVoid UpdateChatScrollAsync()
    {
        await UniTask.WaitForEndOfFrame(this);

        if (this.chatScrollRect != null)
        {
            this.chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }
    #endregion

    private void OnEnterKeyPressed(InputAction.CallbackContext context)
    {
        if (context.performed == true && this.input.gameObject.activeSelf == false)
        {
            OpenChatInputAsync().Forget();
        }
    }
}
