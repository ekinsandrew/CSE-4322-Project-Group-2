namespace CheckersGame.Core
{
    public enum PlayerSide
    {
        None = 0,
        Red = 1,
        Black = 2
    }

    public enum EndState
    {
        None = 0,
        RedWins = 1,
        BlackWins = 2,
        Draw = 3
    }

    public struct BoardPos
    {
        public int X;
        public int Y;

        public BoardPos(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool IsValid
        {
            get { return X >= 0 && X < 8 && Y >= 0 && Y < 8; }
        }

        public override string ToString()
        {
            return "(" + X + "," + Y + ")";
        }
    }

    public struct Move
    {
        public BoardPos From;
        public BoardPos To;

        public Move(BoardPos from, BoardPos to)
        {
            From = from;
            To = to;
        }
    }

    public struct PieceData
    {
        public PlayerSide Side;
        public bool IsKing;

        public PieceData(PlayerSide side, bool isKing)
        {
            Side = side;
            IsKing = isKing;
        }

        public bool IsEmpty
        {
            get { return Side == PlayerSide.None; }
        }
    }
}
