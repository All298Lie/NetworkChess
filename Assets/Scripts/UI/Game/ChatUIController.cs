using Cysharp.Threading.Tasks;
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
        this.input.gameObject.SetActive(false);

        this.input.onSubmit.AddListener(OnSubmitChat);
        this.input.onEndEdit.AddListener(OnEndEditChat);


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
    private void OpenChatInput()
    {
        this.input.gameObject.SetActive(true);
        this.input.text = "";

        this.input.ActivateInputField();
        EventSystem.current.SetSelectedGameObject(this.input.gameObject);
    }
    #endregion

    #region 채팅 전송 (엔터 키로 완료 시)
    private void OnSubmitChat(string text)
    {
        // 1. 채팅이 작성되었는지 확인
        if (string.IsNullOrWhiteSpace(text) == false)
        {
            // TODO: 서버로 채팅 패킷 전송 로직

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

        EventSystem.current.SetSelectedGameObject(null);
    }
    #endregion

    #region 서버로부터 채팅 패킷 수신 시 호출될 함수
    public void ReceiveChatPacket(string senderName, string message)
    {
        // 1. 채팅 프리팹 생성 및 데이터 세팅
        GameObject newChat = Instantiate(this.chatMessagePrefab, this.content);
        newChat.GetComponent<TMP_Text>().text = $"[{senderName}]: {message}";

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
        if (this.input.gameObject.activeSelf == false)
        {
            OpenChatInput();
        }
    }
}
