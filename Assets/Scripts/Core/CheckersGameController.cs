using System;
using System.Collections.Generic;

namespace CheckersGame.Core
{
    public sealed class CheckersGameController
    {
        public event Action StateChanged;
        public event Action<string> MessageChanged;
        public event Action<EndState> GameEnded;

        public CheckersBoard Board { get; private set; }
        public PlayerSide CurrentTurn { get; private set; }
        public EndState EndState { get; private set; }
        public bool AwaitingCaptureContinuation { get; private set; }
        public BoardPos ForcedPiece { get; private set; }

        public CheckersGameController()
        {
            Board = new CheckersBoard();
            CurrentTurn = PlayerSide.Red;
            EndState = EndState.None;
            AwaitingCaptureContinuation = false;
            ForcedPiece = new BoardPos(-1, -1);
        }

        public void StartNewGame()
        {
            Board.Reset();
            CurrentTurn = PlayerSide.Red;
            EndState = EndState.None;
            AwaitingCaptureContinuation = false;
            ForcedPiece = new BoardPos(-1, -1);
            SetMessage("Red to move");
            if (StateChanged != null) StateChanged();
        }

        public List<Move> GetMovesForSelection(BoardPos from)
        {
            if (EndState != EndState.None)
            {
                return new List<Move>();
            }

            if (AwaitingCaptureContinuation && (from.X != ForcedPiece.X || from.Y != ForcedPiece.Y))
            {
                return new List<Move>();
            }

            List<Move> allMoves = Board.GetLegalMoves(CurrentTurn);
            bool anyCapture = false;
            int i;
            for (i = 0; i < allMoves.Count; i++)
            {
                Move move = allMoves[i];
                if (Math.Abs(move.To.X - move.From.X) == 2)
                {
                    anyCapture = true;
                    break;
                }
            }

            return Board.GetLegalMovesForPiece(from, CurrentTurn, AwaitingCaptureContinuation || anyCapture);
        }

        public bool TryPlay(BoardPos from, BoardPos to)
        {
            if (EndState != EndState.None)
            {
                return false;
            }

            if (AwaitingCaptureContinuation && (from.X != ForcedPiece.X || from.Y != ForcedPiece.Y))
            {
                SetMessage("You must continue the capture with the same piece.");
                return false;
            }

            bool wasCapture;
            bool becameKing;
            bool hasAnotherCapture;
            bool success = Board.TryApplyMove(new Move(from, to), CurrentTurn, AwaitingCaptureContinuation, out wasCapture, out becameKing, out hasAnotherCapture);
            if (!success)
            {
                SetMessage("Illegal move.");
                if (StateChanged != null) StateChanged();
                return false;
            }

            if (wasCapture && hasAnotherCapture)
            {
                AwaitingCaptureContinuation = true;
                ForcedPiece = to;
                SetMessage(CurrentTurn.ToString() + " must continue capturing.");
                if (StateChanged != null) StateChanged();
                return true;
            }

            AwaitingCaptureContinuation = false;
            ForcedPiece = new BoardPos(-1, -1);
            CurrentTurn = CurrentTurn == PlayerSide.Red ? PlayerSide.Black : PlayerSide.Red;
            EndState = Board.EvaluateEndState(CurrentTurn);

            if (EndState == EndState.None)
            {
                SetMessage(CurrentTurn.ToString() + " to move");
                if (StateChanged != null) StateChanged();
                return true;
            }

            string endMessage = "Draw!";
            if (EndState == EndState.RedWins) endMessage = "Red wins!";
            else if (EndState == EndState.BlackWins) endMessage = "Black wins!";

            SetMessage(endMessage);
            if (StateChanged != null) StateChanged();
            if (GameEnded != null) GameEnded(EndState);
            return true;
        }

        private void SetMessage(string text)
        {
            if (MessageChanged != null) MessageChanged(text);
        }
    }
}
