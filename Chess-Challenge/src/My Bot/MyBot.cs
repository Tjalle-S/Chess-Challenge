using ChessChallenge.API;

using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

public class MyBot : IChessBot
{
    private readonly int[][] positionalLookups =
    {
        new int[] // Pawns
        {
            0,   0,   0,   0,
            150, 150, 150, 150,
            110, 110, 120, 130,
            105, 105, 110, 125,
            100, 100, 100, 130,
            105, 95,  90,  100,
            105, 110, 110, 80,
            0,   0,   0,   0
        },
        new int[] // Knights.
        {
            270, 280, 290, 290,
            280, 300, 320, 320,
            290, 320, 320, 335,
            290, 325, 335, 340,
            290, 320, 335, 340,
            290, 325, 325, 335,
            280, 300, 320, 320,
            270, 280, 290, 290
        },
        new int[] // Bishops.
        {
            310, 320, 320, 320,
            320, 330, 330, 330,
            320, 330, 335, 340,
            320, 335, 335, 340,
            320, 330, 340, 340,
            320, 340, 340, 340,
            320, 335, 330, 330,
            310, 320, 320, 320
        },
        new int[] // Rooks.
        {
            500, 500, 500, 500,
            505, 510, 510, 510,
            495, 500, 500, 500,
            495, 500, 500, 500,
            495, 500, 500, 500,
            495, 500, 500, 500,
            495, 500, 500, 500,
            500, 500, 500, 505,
        },
        new int[] // Queen.
        {
            880, 890, 890, 895,
            890, 900, 900, 900,
            890, 900, 905, 905,
            895, 900, 905, 905,
            900, 900, 905, 905,
            890, 905, 905, 905,
            890, 900, 905, 900,
            880, 890, 890, 895
        },
        new int[] // King.
        {
            -30, -40, -40, -50,
            -30, -40, -40, -50,
            -30, -40, -40, -50,
            -30, -40, -40, -50,
            -20, -30, -30, -40,
            -10, -20, -20, -20,
             20,  20,  0,   0,
             20,  30,  10,  0,
        }
    };

    private Board _board;
    private bool _isWhite;

    /// <summary>
    /// Make a move.
    /// </summary>
    /// <param name="board">The current board state.</param>
    /// <param name="timer">The current timer.</param>
    /// <returns>The best move acoording to the bot.</returns>
    public Move Think(Board board, Timer timer)
    {
        //var list = positionalLookups[4].Select(i => i + 900).ToList();
        //for (int i = 0; i < 31; i += 4)
        //{
        //    Console.WriteLine($"{list[i]}, {list[i + 1]}, {list[i + 2]}, {list[i + 3]},");
        //}

        //positionalLookups[0].Select(i => i + 100).ToList().ForEach(i => Console.WriteLine($"{i}, "));

        _board = board;
        _isWhite = board.IsWhiteToMove;

        //Evaluate();

        return _board.GetLegalMoves().MinBy(move =>
        {
            // Temporary. Should be something with negamax later.
            _board.MakeMove(move);
            int eval = Evaluate();// * (_isWhite ? 1 : -1);
            _board.UndoMove(move);
            return eval;
        });
    }

    /// <summary>
    /// Performs a minimax search with alpha-beta pruning.
    /// </summary>
    /// <param name="depth">The maximum search depth.</param>
    /// <param name="alpha">The alpha value for pruning.</param>
    /// <param name="beta">The beta value for pruning.</param>
    /// <returns>The evaluation of the current position.</returns>
    int NegaMax(int depth, int alpha, int beta)
    {
        if (_board.IsDraw()) return 0;

        if (_board.IsInCheckmate()) return _board.IsWhiteToMove ? int.MinValue : int.MaxValue;

        if (depth == 0) return Evaluate();

        foreach (Move move in _board.GetLegalMoves())
        {
            _board.MakeMove(move);

            // Recursive stuff.

            _board.UndoMove(move);
        }

        return 0;
    }

    /// <summary>
    /// Performs static evaluation of the current position.
    /// </summary>
    /// <returns>An integer score. Positive means better for white.</returns>
    int Evaluate()
    {
        int score = _board.GetAllPieceLists()
            .Sum(list => (list.IsWhitePieceList == _board.IsWhiteToMove ? 1 : -1) * list
                .Sum(piece => positionalLookups[(int)piece.PieceType - 1][GetLookupIndex(piece.IsWhite ? new(piece.Square.Index ^ 56): piece.Square)]));

        //int materialScore = _board.GetAllPieceLists()
        //    .Sum(list => list.Count
        //        * pieceValues[(int)list.TypeOfPieceInList - 1]
        //        * (list.IsWhitePieceList ? 1 : -1));

        //for (int i = 0; i < 64; i++)
        //{
        //    Square square = new(i);
        //    Console.WriteLine($"{square.Index}: {GetLookupIndex(square)}");
        //}

        return score;
    }

    int GetLookupIndex(Square square) => 4 * square.Rank + Math.Min(square.File, 7 - square.File);
}