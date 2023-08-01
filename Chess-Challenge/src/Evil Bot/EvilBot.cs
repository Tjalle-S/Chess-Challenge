using ChessChallenge.API;

using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using static System.Math;
using static ChessChallenge.API.BitboardHelper;

namespace ChessChallenge.Example;

public class EvilBot : IChessBot
{
    //private const ulong FileA = 0x0101010101010101;
    //private const ulong FileH = 0x8080808080808080;

    /// <summary>
    /// Lookup tables for piece value on different squares.
    /// </summary>
    private readonly ulong[,] positionalLookups =
    {
        {
            0,
            42221890761523350,
            36592262375669870,
            35184844542115945,
            36592176475668580,
            28147884224348265,
            22518470590464105,
            0
        },
        {
            81628988804956430,
            90073366956605720,
            94295534558249250,
            95702930916966690,
            95702930916639010,
            94295534558576930,
            91480741840159000,
            81628988804956430
        },
        {
            90073366957916470,
            92888159675351360,
            95702930917294400,
            95702930917622080,
            95702952392130880,
            95702952392786240,
            92888159675679040,
            90073366957916470
        },
        {
            140739635871744500,
            143554428589179385,
            140739635871744495,
            140739635871744495,
            140739635871744495,
            140739635871744495,
            140739635871744495,
            142147010755297780
        },
        {
            251923926735258480,
            253331344569140090,
            254738740927529850,
            254738740927529855,
            254738740927529860,
            254738740927857530,
            253331366043976570,
            251923926735258480
        },
        {
            239257423932293990,
            239257423932293990,
            239257423932293990,
            239257423932293990,
            242072216649728880,
            247701759134270330,
            253331344570450840,
            253331387520779160
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
        //LookupCompactor.Compact();
        _board = board;
        _isWhite = board.IsWhiteToMove;
        _startPly = board.PlyCount;
        _normalNodes = 0; // #DEBUG
        _quiescentNodes = 0; // #DEBUG

        _ = NegaMax(4, -int.MaxValue, int.MaxValue); // Discard can be removed to save tokens.
        //Console.WriteLine($"Standard search: {_normalNodes} nodes. Quiescent search: {_quiescentNodes} nodes");
        return _bestMove;
    }

    /// <summary>
    /// Performs a minimax search with alpha-beta pruning.
    /// </summary>
    /// <param name="depth">The maximum search depth. A deprh of 0 or less means quiescent search.</param>
    /// <param name="alpha">The alpha value for pruning.</param>
    /// <param name="beta">The beta value for pruning.</param>
    /// <returns>The evaluation of the current position.</returns>
    int NegaMax(int depth, int alpha, int beta)
    {
        // For debugging purposes only.
        if (depth <= 0) _quiescentNodes++; // #DEBUG
        else _normalNodes++;    // #DEBUG

        // Can only occur during search, not at root, so setting _bestMove not required.
        if (_board.IsDraw()) return 0;
        if (_board.IsInCheckmate()) return int.MinValue + _board.PlyCount;

        Span<Move> legalMoves = stackalloc Move[218];
        _board.GetLegalMovesNonAlloc(ref legalMoves, depth <= 0);

        int numMoves = legalMoves.Length;

        Span<int> evalGuesses = stackalloc int[218];
        for (int i = 0; i < numMoves; i++)
        {
            Move move = legalMoves[i];
            evalGuesses[i] = -((int)move.PromotionPieceType + (int)move.CapturePieceType - (int)move.MovePieceType);
        }
        evalGuesses = evalGuesses[..numMoves];
        evalGuesses.Sort(legalMoves);

        int evaluation = depth <= 0 ? Evaluate() : -int.MaxValue;

        if (evaluation >= beta) return beta;

        alpha = Max(alpha, evaluation);

        for (int i = 0; i < numMoves; i++)
        {
            Move move = legalMoves[i];

            _board.MakeMove(move);
            evaluation = -NegaMax(depth - 1, -beta, -alpha);
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

    int _normalNodes = 0;
    int _quiescentNodes = 0;

    /// <summary>
    /// Performs static evaluation of the current position.
    /// </summary>
    /// <returns>An integer score. Positive means better for the player to move.</returns>
    int Evaluate()
    {
        int score = _board.GetAllPieceLists()
            .Sum(list => (list.IsWhitePieceList ? 1 : -1) *
                list.Sum(LookupPieceValue));

        //score += KingSafetyDefendingPawns(true) - KingSafetyDefendingPawns(false);
        //score += KingSafetyStormingPawns(true) - KingSafetyStormingPawns(false);

        return score * (_board.IsWhiteToMove ? 1 : -1);
    }

    int LookupPieceValue(Piece piece)
    {
        int index = piece.Square.Index;
        var (rank, file) = DivRem(piece.IsWhite ? index ^ 56 : index, 8);
        int offset = Min(file, 7 - file) * 16;

        return (int)((positionalLookups[(int)piece.PieceType - 1, rank] & 0xFFFFul << offset) >> offset);
    }

    int KingSafetyDefendingPawns(bool white)
    {
        ulong kingAttacks = GetKingAttacks(_board.GetKingSquare(white));

        return GetNumberOfSetBits((white ? kingAttacks << 8 : kingAttacks >> 8) & _board.GetPieceBitboard(PieceType.Pawn, white)) * 30;
        // This should probably have higher value later, if we have something to counteract the fact that this gives pushing center pawns in the opening negatively affects score.
    }

    int KingSafetyStormingPawns(bool white)
    {
        ulong kingAttacks = GetKingAttacks(_board.GetKingSquare(white));

        return GetNumberOfSetBits(((white ? kingAttacks << 16 : kingAttacks >> 16) | kingAttacks) & _board.GetPieceBitboard(PieceType.Pawn, !white)) * -20;
    }
    // TODO: make sure to use also the king's square itself.
}
