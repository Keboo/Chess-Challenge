using ChessChallenge.API;
using Raylib_cs;
using System;
using System.Linq;

public class MyBot : IChessBot
{
    private static Random random = new();
    private static int[] pieceValues = { 0, 10, 30, 31, 50, 90, 100 };

    public Move Think(Board board, Timer timer)
    {
        Console.WriteLine("---");
        
        //byte[] bytes = "Foo".SelectMany(x => new[] { (byte)(x >> 8 & 0xFF), (byte)(x & 0xFF) }).ToArray()
        return (from move in board.GetLegalMoves()
               let rating = RateMove(board, move)
               orderby rating descending, random.Next()
               select move).First();

    }

    private static double? RateMove(Board board, Move move)
    {
        int before = EvalMaterial(board);
        board.MakeMove(move);
        int after = EvalMaterial(board);
        board.UndoMove(move);
        if (before == after)
        {
            return 0;
        }
        if (before == 0)
        {
            return after;
        }
        return after / before;
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
        

        if (board.TrySkipTurn())
        {
            try
            {
                Move[] nextMoves = board.GetLegalMoves();
                int movedPieceValue = pieceValues[(int)move.MovePieceType];

                if (move.IsCapture)
                {
                    value += pieceValues[(int)move.MovePieceType];
                }
            }
            finally
            {
                board.UndoSkipTurn();
            }
        }

        Console.WriteLine($"Move {move} => {value}");
        return value;
    }

    private static int EvalMaterial(Board board)
    {
        var allList = board.GetAllPieceLists();
        int myMaterial, theirMaterial;
        if (board.IsWhiteToMove)
        {
            myMaterial = allList[0..6].Select(ValueList).Sum();
            theirMaterial = allList[6..].Select(ValueList).Sum();
        }
        else
        {
            myMaterial = allList[6..].Select(ValueList).Sum();
            theirMaterial = allList[0..6].Select(ValueList).Sum();
        }

        return myMaterial - theirMaterial;

        static int ValueList(PieceList list)
        {
            return pieceValues[(int)list.TypeOfPieceInList] * list.Count;
        }
    }
}