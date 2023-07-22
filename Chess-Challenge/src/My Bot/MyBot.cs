using ChessChallenge.API;

using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

public class MyBot : IChessBot
{
    private readonly int[] pieceValues = { 100, 300, 320, 500, 900, 0 /* Last is for king, for shorter material counting. */ };

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
        _board = board;
        _isWhite = board.IsWhiteToMove;

        return _board.GetLegalMoves().MaxBy(move =>
        {
            // Temporary. Should be something with minimax later.
            _board.MakeMove(move);
            int eval = Evaluate() * (_isWhite ? 1 : -1);
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
    int MiniMax(int depth, int alpha, int beta)
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

    /*
 function alphabeta(node, depth, α, β, maximizingPlayer) is
if depth == 0 or node is terminal then
    return the heuristic value of node
if maximizingPlayer then
    value := −∞
    for each child of node do
        value := max(value, alphabeta(child, depth − 1, α, β, FALSE))
        if value > β then
            break (* β cutoff *)
        α := max(α, value)
    return value
else
    value := +∞
    for each child of node do
        value := min(value, alphabeta(child, depth − 1, α, β, TRUE))
        if value < α then
            break (* α cutoff *)
        β := min(β, value)
    return value
*/

    /// <summary>
    /// Performs static evaluation of the current position.
    /// </summary>
    /// <returns>An integer score. Positive means better for white.</returns>
    int Evaluate()
    {
        int materialScore = _board.GetAllPieceLists()
            .Sum(list => list.Count
                * pieceValues[(int)list.TypeOfPieceInList - 1]
                * (list.IsWhitePieceList ? 1 : -1));


        return materialScore;
    }
}