using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance;

    [Header("게임 에셋")]
    [SerializeField] GameObject piecePrefab;
    [SerializeField] PieceData[] pieceDatas;
    [SerializeField] GameObject boardPrefab;
    [SerializeField] GameObject tilePrefab;

    [Header("보드판 세팅")]
    [SerializeField] Vector2 a1Position;
    [SerializeField] float tileSize;

    Dictionary<PieceType, PieceData> pieceDic;

    Piece[,] board;

    Vector2Int? enPassant;

    public Vector2Int? EnPassant
    { 
        get { return enPassant; }
    }

    Vector2Int whiteKing;
    Vector2Int blackKing;

    // FEN 표기법을 통해 초기 보드판 세팅 상태 설정
    const string START_FEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";

    // 오브젝트가 생성된 즉시 호출되는 함수
    void Awake()
    {
        if (Instance == null) // 싱글톤 패턴 디자인
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("보드 매니저가 이미 존재합니다.");
            Destroy(this);
        }
    }

    // Update() 함수 실행 전 딱 한 번 실행되는 함수
    void Start()
    {
        this.board = new Piece[8, 8];

        this.enPassant = null;

        this.pieceDic = new Dictionary<PieceType, PieceData>();
        foreach (PieceData pieceData in pieceDatas)
        {
            PieceType type = pieceData.type;
            if (this.pieceDic.ContainsKey(type) == false)
            {
                this.pieceDic.Add(type, pieceData);
            }
        }

        GenerateTiles();
        InitializeBoard(START_FEN);
    }

    // FEN 기보법을 통해 표기된 문자열을 통해 보드판 세팅 
    private void InitializeBoard(string fen)
    {
        int x = 0;
        int y = 7;

        GameObject piecesObject = new GameObject("Pieces");

        // FEN 기보법 확인
        foreach (char c in fen)
        {
            if (c == '/') // '/'의 경우, 다음 줄로 넘김 표시
            {
                x = 0;
                y = y - 1;
            }
            else if (char.IsDigit(c)) // 숫자일 경우 해당 칸만큼 빈 공간 표시
            {
                x = x + (c - '0');
            }
            else // 영문자일 경우 해당 기물 표시
            {
                bool isWhite = char.IsUpper(c); // 대문자일 경우 백 진영
                PieceType type = GetPieceTypeFromChar(c);

                SpawnPiece(piecesObject.transform, type, isWhite, x, y);
                x = x + 1;
            }
        }
    }

    // 기물에 맞는 열거형을 반환하는 함수
    private PieceType GetPieceTypeFromChar(char c)
    {
        switch (char.ToLower(c))
        {
            case 'n':
                return PieceType.Knight;

            case 'b':
                return PieceType.Bishop;

            case 'r':
                return PieceType.Rook;

            case 'q':
                return PieceType.Queen;

            case 'k':
                return PieceType.King;

            case 'p':
            default:
                return PieceType.Pawn;
        }
    }

    // 기물을 보드판에 배치하는 함수
    private void SpawnPiece(Transform parent, PieceType type, bool isWhite, int x, int y)
    {
        if (this.pieceDic.ContainsKey(type) == false) return;

        PieceData data = this.pieceDic[type];

        Vector3 worldPos = GetWorldPosition(x, y);

        GameObject pieceObject = Instantiate(piecePrefab, worldPos, Quaternion.identity, parent);
        pieceObject.name = $"{(isWhite ? "White" : "Black")}_{data.name}";
        Piece newPiece = pieceObject.GetComponent<Piece>();

        newPiece.Setup(data, isWhite, new Vector2Int(x, y));

        this.board[x, y] = newPiece;

        if (type == PieceType.King)
        {
            if (isWhite == true)
            {
                this.whiteKing = new Vector2Int(x, y);
            }
            else
            {
                this.blackKing = new Vector2Int(x, y);
            }
        }
    }

    // 타일을 생성하는 함수
    private void GenerateTiles()
    {
        GameObject boardObject = Instantiate(this.boardPrefab, Vector3.zero, Quaternion.identity);

        boardObject.name = "Board";

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                GameObject tileObject = Instantiate(this.tilePrefab, GetWorldPosition(x, y), Quaternion.identity, boardObject.transform);
                
                Tile tile = tileObject.GetComponent<Tile>();
                tile.Setup(x, y);
            }
        }
    }

    // x, y 값 기준 객체가 존재해야할 월드 포지션을 가져오는 함수
    public Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(a1Position.x + (x * tileSize), a1Position.y + (y * tileSize), 0.0f);
    }

    // 킹 좌표가 갱신되었을 때 작동하는 함수
    public void UpdateKingPosition(Vector2Int pos, bool isWhite)
    {
        if (isWhite == true)
        {
            this.whiteKing = pos;
        }
        else
        {
            this.blackKing = pos;
        }
    }

    // 킹의 현재 좌표를 확인하는 함수
    public Vector2Int GetKingPosition(bool isWhite)
    {
        return isWhite ? whiteKing : blackKing;
    }
}
