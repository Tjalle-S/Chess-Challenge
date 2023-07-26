using ChessChallenge.API;

using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

namespace ChessChallenge.Example;

public class EvilBot : IChessBot
{
    /// <summary>
    /// Lookup tables for piece value on different squares.
    /// </summary>
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

    private int _startPly;
    private Move _bestMove;

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

        _board = board;
        _isWhite = board.IsWhiteToMove;
        _startPly = board.PlyCount;
        _totalNodes = 0;

        _ = NegaMax(6, int.MinValue + 1, int.MaxValue - 1); // Discard can be removed to save tokens.
        //Console.WriteLine(_totalNodes);
        return _bestMove;
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
        _totalNodes++; // For debugging purposes only.

        // Can only occur during search, not at root, so setting _bestMove not required.
        if (_board.IsDraw()) return 0;
        if (_board.IsInCheckmate()) return int.MinValue + _board.PlyCount;

        if (depth == 0) return Evaluate(); // TODO: quiescence search.

        Span<Move> legalMoves = stackalloc Move[218];
        _board.GetLegalMovesNonAlloc(ref legalMoves);

        int numMoves = legalMoves.Length;

        Span<int> evalGuesses = stackalloc int[218];
        for (int i = 0; i < numMoves; i++)
        {
            Move move = legalMoves[i];
            evalGuesses[i] = -((int)move.PromotionPieceType + (int)move.CapturePieceType - (int)move.MovePieceType);
        }
        evalGuesses = evalGuesses[..numMoves];
        evalGuesses.Sort(legalMoves);

        for (int i = 0; i < numMoves; i++)
        {
            Move move = legalMoves[i];

            _board.MakeMove(move);
            int evaluation = -NegaMax(depth - 1, -beta, -alpha);
            _board.UndoMove(move);

            if (evaluation >= beta) return beta;
            if (evaluation > alpha)
            {
                alpha = evaluation;

                if (_board.PlyCount == _startPly) _bestMove = move;
            }
        }

        return alpha;
    }

    int _totalNodes = 0;

    /// <summary>
    /// Performs static evaluation of the current position.
    /// </summary>
    /// <returns>An integer score. Positive means better for the player to move.</returns>
    int Evaluate()
    {
        int score = _board.GetAllPieceLists()
            .Sum(list => (list.IsWhitePieceList == _board.IsWhiteToMove ? 1 : -1) * list
                .Sum(piece => positionalLookups[(int)piece.PieceType - 1][GetLookupIndex(piece.IsWhite ? new(piece.Square.Index ^ 56) : piece.Square)]));


        return score;
    }

    int GetLookupIndex(Square square) => 4 * square.Rank + Math.Min(square.File, 7 - square.File);
}