using System.Collections.Generic;
using CheckersGame.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CheckersGame.View
{
    public sealed class RuntimeBoardView : MonoBehaviour
    {
        private readonly Dictionary<string, Button> _squareButtons = new Dictionary<string, Button>();
        private readonly Dictionary<string, Image> _pieceImages = new Dictionary<string, Image>();
        private readonly Dictionary<string, Text> _pieceLabels = new Dictionary<string, Text>();
        private readonly List<BoardPos> _currentTargets = new List<BoardPos>();

        private CheckersGameController _controller;
        private Text _statusText;
        private BoardPos? _selected;

        public void Initialize(CheckersGameController controller)
        {
            _controller = controller;
            _controller.StateChanged += Refresh;
            _controller.MessageChanged += SetStatus;
            BuildUi();
            Refresh();
        }

        private static string Key(int x, int y)
        {
            return x + ":" + y;
        }

        private void BuildUi()
        {
            GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            DontDestroyOnLoad(canvasGo);

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1400, 900);

            RectTransform root;
            CreatePanel(canvasGo.transform, out root, new Vector2(0.5f, 0.5f), new Vector2(1100, 820), new Color(0.12f, 0.12f, 0.14f, 1f));

            Text titleText = CreateText(root, "Title", "Checkers Local Prototype", 34, TextAnchor.MiddleCenter, FontStyle.Bold);
            SetRect(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -45), new Vector2(800, 60));

            _statusText = CreateText(root, "Status", "", 26, TextAnchor.MiddleCenter, FontStyle.Bold);
            SetRect(_statusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -95), new Vector2(800, 50));

            GameObject boardRoot = new GameObject("Board", typeof(RectTransform), typeof(GridLayoutGroup), typeof(Image));
            boardRoot.transform.SetParent(root, false);
            RectTransform boardRect = boardRoot.GetComponent<RectTransform>();
            SetRect(boardRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(70, 0), new Vector2(640, 640));
            boardRoot.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.09f, 1f);

            GridLayoutGroup grid = boardRoot.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 8;
            grid.cellSize = new Vector2(80, 80);
            grid.spacing = Vector2.zero;

            for (int y = 7; y >= 0; y--)
            {
                for (int x = 0; x < 8; x++)
                {
                    GameObject square = new GameObject("Square_" + x + "_" + y, typeof(RectTransform), typeof(Image), typeof(Button));
                    square.transform.SetParent(boardRoot.transform, false);
                    Image image = square.GetComponent<Image>();
                    image.color = CheckersBoard.IsDarkSquare(x, y)
                        ? new Color(0.36f, 0.22f, 0.14f, 1f)
                        : new Color(0.91f, 0.83f, 0.72f, 1f);

                    int cx = x;
                    int cy = y;
                    Button button = square.GetComponent<Button>();
                    button.onClick.AddListener(delegate { OnSquareClicked(new BoardPos(cx, cy)); });
                    _squareButtons[Key(x, y)] = button;

                    GameObject pieceGo = new GameObject("Piece", typeof(RectTransform), typeof(Image));
                    pieceGo.transform.SetParent(square.transform, false);
                    RectTransform pieceRect = pieceGo.GetComponent<RectTransform>();
                    SetRect(pieceRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56, 56));
                    Image pieceImage = pieceGo.GetComponent<Image>();
                    pieceImage.raycastTarget = false;
                    _pieceImages[Key(x, y)] = pieceImage;

                    Text label = CreateText(pieceRect, "Label", "", 22, TextAnchor.MiddleCenter, FontStyle.Bold);
                    SetRect(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(50, 50));
                    _pieceLabels[Key(x, y)] = label;
                }
            }

            GameObject sidePanel = new GameObject("SidePanel", typeof(RectTransform), typeof(Image));
            sidePanel.transform.SetParent(root, false);
            sidePanel.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.21f, 1f);
            RectTransform sideRect = sidePanel.GetComponent<RectTransform>();
            SetRect(sideRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-90, -20), new Vector2(280, 520));

            Text info = CreateText(sideRect, "Info", "Local two-player checkers.\n\nRules included:\n- forced captures\n- multi-jump turns\n- king promotion\n- win detection\n\nControls:\n1. Click a piece\n2. Click a highlighted square\n3. New Game to reset", 20, TextAnchor.UpperLeft, FontStyle.Normal);
            SetRect(info.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(240, 300));

            Button newGameButton = CreateButton(sideRect, "New Game", new Vector2(220, 58), new Vector2(0, -360));
            newGameButton.onClick.AddListener(delegate
            {
                _selected = null;
                _currentTargets.Clear();
                _controller.StartNewGame();
            });

            Button clearButton = CreateButton(sideRect, "Clear Selection", new Vector2(220, 58), new Vector2(0, -430));
            clearButton.onClick.AddListener(delegate
            {
                _selected = null;
                _currentTargets.Clear();
                Refresh();
            });
        }

        private void OnSquareClicked(BoardPos pos)
        {
            if (_controller == null || _controller.EndState != EndState.None)
            {
                return;
            }

            if (_selected.HasValue)
            {
                for (int i = 0; i < _currentTargets.Count; i++)
                {
                    BoardPos target = _currentTargets[i];
                    if (target.X == pos.X && target.Y == pos.Y)
                    {
                        BoardPos from = _selected.Value;
                        _selected = null;
                        _currentTargets.Clear();
                        _controller.TryPlay(from, pos);
                        return;
                    }
                }
            }

            PieceData piece = _controller.Board.GetPiece(pos);
            if (piece.Side != _controller.CurrentTurn)
            {
                _selected = null;
                _currentTargets.Clear();
                Refresh();
                return;
            }

            List<Move> moves = _controller.GetMovesForSelection(pos);
            _selected = pos;
            _currentTargets.Clear();
            for (int i = 0; i < moves.Count; i++)
            {
                _currentTargets.Add(moves[i].To);
            }
            Refresh();
        }

        private void Refresh()
        {
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    string key = Key(x, y);
                    Color baseColor = CheckersBoard.IsDarkSquare(x, y)
                        ? new Color(0.36f, 0.22f, 0.14f, 1f)
                        : new Color(0.91f, 0.83f, 0.72f, 1f);

                    Button btn = _squareButtons[key];
                    Image img = btn.GetComponent<Image>();
                    img.color = baseColor;

                    if (_selected.HasValue && _selected.Value.X == x && _selected.Value.Y == y)
                    {
                        img.color = new Color(0.87f, 0.77f, 0.2f, 1f);
                    }

                    for (int i = 0; i < _currentTargets.Count; i++)
                    {
                        BoardPos target = _currentTargets[i];
                        if (target.X == x && target.Y == y)
                        {
                            img.color = new Color(0.3f, 0.8f, 0.4f, 1f);
                            break;
                        }
                    }

                    PieceData piece = _controller.Board.GetPiece(new BoardPos(x, y));
                    Image pieceImage = _pieceImages[key];
                    Text label = _pieceLabels[key];

                    if (piece.IsEmpty)
                    {
                        pieceImage.enabled = false;
                        label.text = string.Empty;
                    }
                    else
                    {
                        pieceImage.enabled = true;
                        pieceImage.color = piece.Side == PlayerSide.Red
                            ? new Color(0.85f, 0.2f, 0.2f, 1f)
                            : new Color(0.12f, 0.12f, 0.12f, 1f);
                        label.text = piece.IsKing ? "K" : string.Empty;
                        label.color = piece.Side == PlayerSide.Red ? Color.white : new Color(0.96f, 0.92f, 0.75f, 1f);
                    }
                }
            }
        }

        private void SetStatus(string text)
        {
            if (_statusText != null)
            {
                _statusText.text = text;
            }
        }

        private static void CreatePanel(Transform parent, out RectTransform rect, Vector2 anchor, Vector2 size, Color color)
        {
            GameObject go = new GameObject("RootPanel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            go.GetComponent<Image>().color = color;
        }

        private static Button CreateButton(Transform parent, string label, Vector2 size, Vector2 anchoredPos)
        {
            GameObject go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), anchoredPos, size);
            go.GetComponent<Image>().color = new Color(0.25f, 0.45f, 0.78f, 1f);

            Text text = CreateText(rect, "Text", label, 24, TextAnchor.MiddleCenter, FontStyle.Bold);
            SetRect(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            return go.GetComponent<Button>();
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor anchor, FontStyle fontStyle)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.fontStyle = fontStyle;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = Color.white;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }
    }
}
