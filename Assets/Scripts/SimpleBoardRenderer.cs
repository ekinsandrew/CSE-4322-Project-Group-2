using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
public class SimpleBoardRenderer : MonoBehaviour
{
    public int size = 8;
    public float squareSize = 1f;
    
    public Sprite first, firstKing, second, secondKing;
    public GameObject victoryScreen;
    private GameObject[,] squares;
    private GameObject[,] pieceObjects;
    private int[,] board;

    private int currentPlayer = 1;
    private Vector2Int? selectedPiece = null;
    private List<Vector2Int> validMoves = new List<Vector2Int>();

    void Start()
    {
        GenerateBoard();
        InitializeBoardState();
        DrawBoard();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
        if(CheckVictory() == 1 || CheckVictory() == 2)
        {
            victoryScreen.SetActive(true);
            TextMeshProUGUI theText = victoryScreen.transform.GetChild(0).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>();
            theText.SetText("Player " + CheckVictory() + " has won!");
        }
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void Quit()
    {
        Application.Quit();
    }
    int CheckVictory()
    {
        bool player1Win = true;
        bool player2Win = true;
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                int piece = board[x, y];
                if(BelongsToPlayer(piece, 1))
                {
                   player2Win = false;
                //    Debug.Log("player 1 piece at" + x + " " + y);
                }
                if (BelongsToPlayer(piece, 2))
                {
                   player1Win = false;
                }
            }
        }
        if(player1Win)
        {
            return 1;
        }
        else if(player2Win)
        {
            return 2;
        }
        else
        {
            return 0;
        }
    }
    void HandleClick()
    {
        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        int x = Mathf.RoundToInt(worldPoint.x);
        int y = Mathf.RoundToInt(worldPoint.y);

        if (!IsInsideBoard(x, y)) return;

        Vector2Int clicked = new Vector2Int(x, y);
        int piece = board[x, y];

        if (selectedPiece.HasValue)
        {
            if (validMoves.Contains(clicked))
            {
                bool wasCapture = MovePiece(selectedPiece.Value, clicked);

                // Multi-jump logic
                if (wasCapture)
                {
                    selectedPiece = clicked;
                    validMoves = GetCaptureMoves(clicked);

                    if (validMoves.Count > 0)
                    {
                        DrawBoard();
                        return;
                    }
                }

                selectedPiece = null;
                validMoves.Clear();
                currentPlayer = currentPlayer == 1 ? 2 : 1;
                DrawBoard();
                return;
            }
        }

        if (BelongsToCurrentPlayer(piece))
        {
            selectedPiece = clicked;

            // Enforce mandatory capture
            if (PlayerHasCapture(currentPlayer))
                validMoves = GetCaptureMoves(clicked);
            else
                validMoves = GetValidMoves(clicked);

            DrawBoard();
        }
    }

    bool MovePiece(Vector2Int from, Vector2Int to)
    {
        int piece = board[from.x, from.y];
        board[to.x, to.y] = piece;
        board[from.x, from.y] = 0;

        int dx = to.x - from.x;
        int dy = to.y - from.y;

        bool isCapture = Mathf.Abs(dx) == 2;

        if (isCapture)
        {
            int jumpedX = from.x + dx / 2;
            int jumpedY = from.y + dy / 2;
            board[jumpedX, jumpedY] = 0;
        }

        if (piece == 1 && to.y == 7) board[to.x, to.y] = 3;
        if (piece == 2 && to.y == 0) board[to.x, to.y] = 4;

        return isCapture;
    }

    bool PlayerHasCapture(int player)
    {
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                int piece = board[x, y];
                if (BelongsToPlayer(piece, player))
                {
                    if (GetCaptureMoves(new Vector2Int(x, y)).Count > 0)
                        return true;
                }
            }
        }
        return false;
    }

    List<Vector2Int> GetCaptureMoves(Vector2Int pos)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        int piece = board[pos.x, pos.y];

        foreach (var dir in GetDirections(piece))
        {
            int midX = pos.x + dir.x;
            int midY = pos.y + dir.y;
            int jumpX = pos.x + dir.x * 2;
            int jumpY = pos.y + dir.y * 2;

            if (IsInsideBoard(jumpX, jumpY) && board[jumpX, jumpY] == 0)
            {
                int midPiece = board[midX, midY];
                if (midPiece != 0 && IsOpponentPiece(piece, midPiece))
                {
                    moves.Add(new Vector2Int(jumpX, jumpY));
                }
            }
        }

        return moves;
    }

    List<Vector2Int> GetValidMoves(Vector2Int pos)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        int piece = board[pos.x, pos.y];

        foreach (var dir in GetDirections(piece))
        {
            int nx = pos.x + dir.x;
            int ny = pos.y + dir.y;

            if (IsInsideBoard(nx, ny) && board[nx, ny] == 0)
                moves.Add(new Vector2Int(nx, ny));
        }

        return moves;
    }

    List<Vector2Int> GetDirections(int piece)
    {
        List<Vector2Int> dirs = new List<Vector2Int>();

        if (piece == 1)
        {
            dirs.Add(new Vector2Int(-1, 1));
            dirs.Add(new Vector2Int(1, 1));
        }
        else if (piece == 2)
        {
            dirs.Add(new Vector2Int(-1, -1));
            dirs.Add(new Vector2Int(1, -1));
        }
        else
        {
            dirs.Add(new Vector2Int(-1, 1));
            dirs.Add(new Vector2Int(1, 1));
            dirs.Add(new Vector2Int(-1, -1));
            dirs.Add(new Vector2Int(1, -1));
        }

        return dirs;
    }

    bool BelongsToCurrentPlayer(int piece)
    {
        return BelongsToPlayer(piece, currentPlayer);
    }

    bool BelongsToPlayer(int piece, int player)
    {
        if (player == 1)
            return piece == 1 || piece == 3;
        else
            return piece == 2 || piece == 4;
    }

    bool IsOpponentPiece(int piece, int other)
    {
        bool a = piece == 1 || piece == 3;
        bool b = other == 1 || other == 3;
        return a != b;
    }

    bool IsInsideBoard(int x, int y)
    {
        return x >= 0 && x < size && y >= 0 && y < size;
    }

    void GenerateBoard()
    {
        squares = new GameObject[size, size];

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                GameObject square = GameObject.CreatePrimitive(PrimitiveType.Quad);
                square.transform.parent = transform;
                square.transform.position = new Vector3(x, y, 0);
                square.transform.localScale = Vector3.one * squareSize;

                squares[x, y] = square;
            }
        }

        Camera.main.transform.position = new Vector3(size / 2f - 0.5f, size / 2f - 0.5f, -10);
        Camera.main.orthographic = true;
        Camera.main.orthographicSize = size / 2f + 1;
    }

    void InitializeBoardState()
    {
        board = new int[size, size];
        pieceObjects = new GameObject[size, size];

        for (int y = 5; y < 8; y++)
            for (int x = 0; x < size; x++)
                if ((x + y) % 2 == 1)
                    board[x, y] = 2;

        for (int y = 0; y < 3; y++)
            for (int x = 0; x < size; x++)
                if ((x + y) % 2 == 1)
                    board[x, y] = 1;
    }

    void DrawBoard()
    {
        DrawSquares();
        DrawPieces();
    }

    void DrawSquares()
    {
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Renderer r = squares[x, y].GetComponent<Renderer>();

                Color baseColor = ((x + y) % 2 == 0)
                    ? new Color(0.9f, 0.9f, 0.9f)
                    : new Color(0.25f, 0.25f, 0.25f);

                r.material.color = baseColor;

                if (selectedPiece.HasValue && selectedPiece.Value.x == x && selectedPiece.Value.y == y)
                    r.material.color = Color.yellow;
                else if (validMoves.Contains(new Vector2Int(x, y)))
                    r.material.color = Color.green;
            }
        }
    }

    void DrawPieces()
    {
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (pieceObjects[x, y] != null)
                    Destroy(pieceObjects[x, y]);

                if (board[x, y] != 0)
                {
                    GameObject piece = new GameObject();
                    piece.AddComponent<SpriteRenderer>();
                    piece.transform.parent = transform;
                    piece.transform.position = new Vector3(x, y, -0.2f);

                    SpriteRenderer r = piece.GetComponent<SpriteRenderer>();
                    
                    if (board[x, y] == 1) r.sprite = first;
                    if (board[x, y] == 2) r.sprite = second;
                    if (board[x, y] == 3) r.sprite = firstKing;
                    if (board[x, y] == 4) r.sprite = secondKing;

                    pieceObjects[x, y] = piece;
                }
            }
        }
    }
}
