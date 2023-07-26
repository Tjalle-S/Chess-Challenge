using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ChessChallenge.API;
using ChessChallenge.Application;
using ChessChallenge.Chess;
using ChessChallenge.Example;

namespace ChessChallenge;

public static class FastMatches
{
    private static readonly object MyBotLock    = new();
    private static readonly object EvilBotLock  = new();
    private static readonly object DrawsLock    = new();
    private static readonly object ProgressLock = new();

    private static int MyBotWins   = 0;
    private static int EvilBotWins = 0;
    private static int Draws       = 0;
    private static int Progress    = 0;

    public static void Play(int milliseconds, int numGames)
    {
        Console.WriteLine($"Started {numGames} matches.");
        Console.Write("0%");

        var fens = FileHelper.ReadResourceFile("Fens.txt").Split('\n').Where(fen => fen.Length > 0).ToArray();

        Parallel.For(0, numGames, new() { MaxDegreeOfParallelism = 1 }, i =>
        {
            Stopwatch stopwatch = new();
            Chess.Board board = new();
            board.LoadPosition(fens[i / 2]);

            IChessBot whitePlayer = new MyBot();
            IChessBot blackPlayer = new EvilBot();

            int whiteMS = milliseconds;
            int blackMS = milliseconds;

            if (i % 2 == 0) (whitePlayer, blackPlayer) = (blackPlayer, whitePlayer);

            while (true)
            {
                if (board.IsWhiteToMove)
                {
                    MakeMove(board, whitePlayer, stopwatch, ref whiteMS);
                }
                else
                {
                    MakeMove(board, blackPlayer, stopwatch, ref blackMS);
                }

                var state = Arbiter.GetGameState(board);

                if (state != GameResult.InProgress)
                    lock (ProgressLock)
                    {
                        Progress++;
                        Console.Write($"\r{Progress * 100 / numGames}%");
                    }

                if (Arbiter.IsWhiteWinsResult(state) && whitePlayer is MyBot)
                {
                    if (whitePlayer is MyBot)
                        lock (MyBotLock)
                        {
                            MyBotWins++;
                        }
                    else
                        lock (EvilBotLock)
                        {
                            EvilBotWins++;
                        }

                    return;
                }
                else if (Arbiter.IsBlackWinsResult(state))
                {
                    if (blackPlayer is MyBot)
                        lock (MyBotLock)
                        {
                            MyBotWins++;
                        }
                    else
                        lock (EvilBotLock)
                        {
                            EvilBotWins++;
                        }

                    return;
                }
                else if (Arbiter.IsDrawResult(state))
                {
                    lock (DrawsLock)
                    {
                        Draws++;
                    }

                    return;
                }
            }
        });

        Console.WriteLine($"\rMyBot wins: {MyBotWins}");
        Console.WriteLine($"EvilBot wins: {EvilBotWins}");
        Console.WriteLine($"Draws: {Draws}");

        MyBotWins = 0;
        EvilBotWins = 0;
        Draws = 0;
        Progress = 0;
    }

    private static void MakeMove(Chess.Board board, IChessBot bot, Stopwatch stopwatch, ref int milliseconds)
    {
        API.Board botBoard = new(board);
        Timer timer = new Timer(milliseconds);
        //Span<API.Move> legalMoves = stackalloc API.Move[218];
        //botBoard.GetLegalMovesNonAlloc(ref legalMoves);

        stopwatch.Start();
        API.Move move1 = bot.Think(botBoard, timer);

        //if (!legalMoves.Contains(move1)) return false;

        Chess.Move move = new(move1.RawValue);

        milliseconds -= (int)stopwatch.ElapsedMilliseconds;
        stopwatch.Reset();

        board.MakeMove(move, false);

        //return true;
    }
}
