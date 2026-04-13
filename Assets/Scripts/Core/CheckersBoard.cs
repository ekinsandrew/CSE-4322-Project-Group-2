using System.Collections.Generic;
using UnityEngine;

namespace CheckersGame.Core
{
    public sealed class CheckersBoard
    {
        private readonly PieceData[,] _grid = new PieceData[8, 8];

        public PieceData GetPiece(BoardPos pos)
        {
            return _grid[pos.X, pos.Y];
        }

        public void SetPiece(BoardPos pos, PieceData piece)
        {
            _grid[pos.X, pos.Y] = piece;
        }

        public void Reset()
        {
            int x;
            int y;
            for (x = 0; x < 8; x++)
            {
                for (y = 0; y < 8; y++)
                {
                    _grid[x, y] = new PieceData(PlayerSide.None, false);
                }
            }

            for (y = 0; y < 3; y++)
            {
                for (x = 0; x < 8; x++)
                {
                    if (IsDarkSquare(x, y))
                    {
                        _grid[x, y] = new PieceData(PlayerSide.Black, false);
                    }
                }
            }

            for (y = 5; y < 8; y++)
            {
                for (x = 0; x < 8; x++)
                {
                    if (IsDarkSquare(x, y))
                    {
                        _grid[x, y] = new PieceData(PlayerSide.Red, false);
                    }
                }
            }
        }

        public static bool IsDarkSquare(int x, int y)
        {
            return ((x + y) % 2) == 1;
        }

        public List<Move> GetLegalMoves(PlayerSide side)
        {
            List<Move> captures = new List<Move>();
            List<Move> normals = new List<Move>();
            int x;
            int y;

            for (x = 0; x < 8; x++)
            {
                for (y = 0; y < 8; y++)
                {
                    BoardPos pos = new BoardPos(x, y);
                    PieceData piece = GetPiece(pos);
                    if (piece.Side != side)
                    {
                        continue;
                    }
                    GetMovesForPiece(pos, captures, normals);
                }
            }

            return captures.Count > 0 ? captures : normals;
        }

        public List<Move> GetLegalMovesForPiece(BoardPos from, PlayerSide side, bool forceCaptureOnly)
        {
            PieceData piece = GetPiece(from);
            List<Move> captures = new List<Move>();
            List<Move> normals = new List<Move>();

            if (piece.Side != side)
            {
                return captures;
            }

            GetMovesForPiece(from, captures, normals);
            if (forceCaptureOnly)
            {
                return captures;
            }
            return captures.Count > 0 ? captures : normals;
        }

        private void GetMovesForPiece(BoardPos from, List<Move> captures, List<Move> normals)
        {
            PieceData piece = GetPiece(from);
            if (piece.IsEmpty)
            {
                return;
            }

            Vector2Int[] dirs = GetDirections(piece);
            int i;
            for (i = 0; i < dirs.Length; i++)
            {
                Vector2Int dir = dirs[i];
                BoardPos step = new BoardPos(from.X + dir.x, from.Y + dir.y);
                if (!step.IsValid)
                {
                    continue;
                }

                PieceData stepPiece = GetPiece(step);
                if (stepPiece.IsEmpty)
                {
                    normals.Add(new Move(from, step));
                }
                else if (stepPiece.Side != piece.Side)
                {
                    BoardPos landing = new BoardPos(from.X + (dir.x * 2), from.Y + (dir.y * 2));
                    if (landing.IsValid && GetPiece(landing).IsEmpty)
                    {
                        captures.Add(new Move(from, landing));
                    }
                }
            }
        }

        private static Vector2Int[] GetDirections(PieceData piece)
        {
            if (piece.IsKing)
            {
                return new Vector2Int[]
                {
                    new Vector2Int(-1, -1),
                    new Vector2Int(1, -1),
                    new Vector2Int(-1, 1),
                    new Vector2Int(1, 1)
                };
            }

            if (piece.Side == PlayerSide.Red)
            {
                return new Vector2Int[] { new Vector2Int(-1, -1), new Vector2Int(1, -1) };
            }
            return new Vector2Int[] { new Vector2Int(-1, 1), new Vector2Int(1, 1) };
        }

        public bool TryApplyMove(Move move, PlayerSide side, bool mustContinueCapture, out bool wasCapture, out bool becameKing, out bool hasAnotherCapture)
        {
            wasCapture = false;
            becameKing = false;
            hasAnotherCapture = false;

            if (!move.From.IsValid || !move.To.IsValid)
            {
                return false;
            }

            PieceData piece = GetPiece(move.From);
            if (piece.Side != side)
            {
                return false;
            }

            int dx = move.To.X - move.From.X;
            int dy = move.To.Y - move.From.Y;
            int absDx = Mathf.Abs(dx);
            int absDy = Mathf.Abs(dy);

            if (GetPiece(move.To).Side != PlayerSide.None)
            {
                return false;
            }

            List<Move> legalMoves = GetLegalMoves(side);
            bool exactMatch = false;
            int i;
            for (i = 0; i < legalMoves.Count; i++)
            {
                Move legal = legalMoves[i];
                if (legal.From.X == move.From.X && legal.From.Y == move.From.Y && legal.To.X == move.To.X && legal.To.Y == move.To.Y)
                {
                    exactMatch = true;
                    break;
                }
            }
            if (!exactMatch)
            {
                return false;
            }

            if (mustContinueCapture && (absDx != 2 || absDy != 2))
            {
                return false;
            }

            SetPiece(move.From, new PieceData(PlayerSide.None, false));

            if (absDx == 2 && absDy == 2)
            {
                wasCapture = true;
                BoardPos mid = new BoardPos((move.From.X + move.To.X) / 2, (move.From.Y + move.To.Y) / 2);
                SetPiece(mid, new PieceData(PlayerSide.None, false));
            }

            if (!piece.IsKing)
            {
                if ((piece.Side == PlayerSide.Red && move.To.Y == 0) || (piece.Side == PlayerSide.Black && move.To.Y == 7))
                {
                    piece.IsKing = true;
                    becameKing = true;
                }
            }

            SetPiece(move.To, piece);

            if (wasCapture)
            {
                List<Move> followUps = GetLegalMovesForPiece(move.To, side, true);
                hasAnotherCapture = followUps.Count > 0;
            }

            return true;
        }

        public EndState EvaluateEndState(PlayerSide currentTurn)
        {
            int redCount = 0;
            int blackCount = 0;
            int x;
            int y;

            for (x = 0; x < 8; x++)
            {
                for (y = 0; y < 8; y++)
                {
                    PieceData piece = _grid[x, y];
                    if (piece.Side == PlayerSide.Red) redCount++;
                    if (piece.Side == PlayerSide.Black) blackCount++;
                }
            }

            if (redCount == 0) return EndState.BlackWins;
            if (blackCount == 0) return EndState.RedWins;

            List<Move> moves = GetLegalMoves(currentTurn);
            if (moves.Count == 0)
            {
                return currentTurn == PlayerSide.Red ? EndState.BlackWins : EndState.RedWins;
            }

            return EndState.None;
        }
    }
}
