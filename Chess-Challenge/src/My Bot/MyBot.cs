using ChessChallenge.API;
using System;
using System.Linq;

public class MyBot : IChessBot
{
    private static Random random = new();
    private static int[] pieceValues = { 0, 10, 30, 31, 50, 90, 100 };

    public Move Think(Board board, Timer timer)
    {
        //byte[] bytes = "Foo".SelectMany(x => new[] { (byte)(x >> 8 & 0xFF), (byte)(x & 0xFF) }).ToArray()
        return (from move in board.GetLegalMoves()
               let rating = RateMove(board, move)
               orderby rating descending, random.Next()
               select move).First();
    }

    //Return a value -100 to 100 for how good this move is
    private static double? RateMove(Board board, Move move)
    {

        board.MakeMove(move);
        double value = 0;

        try
        {
            if (board.IsInCheckmate())
            {
                return 100;
            }

            Move[] nextMoves = board.GetLegalMoves();
            
            int movedPieceValue = pieceValues[(int)move.MovePieceType];

            //Check if the move captures a piece
            if (move.IsCapture)
            {
                value += pieceValues[(int)move.CapturePieceType];
            }

            //Check if the move allows for a piece to be targeted
            if (nextMoves.Any(x => x.TargetSquare == move.TargetSquare))
            {
                value -= movedPieceValue;
            }
        }
        finally
        {
            board.UndoMove(move);
        }

        return value;
    }
}