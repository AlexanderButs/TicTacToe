using System;
using System.Linq;
using Newtonsoft.Json;

using TicTacToe.Exceptions;

namespace TicTacToe.Model
{
    public class Game
    {
        [JsonIgnore]
        public string Id { get; set; }

        [JsonIgnore]
        public int Cas { get; set; }

        [JsonIgnore]
        public uint Player1Id { get; set; }

        [JsonIgnore]
        public uint Player2Id { get; set; }

        [JsonProperty("winnerId")]
        public uint? WinnerId { get; set; }

        [JsonProperty("gameOver")]
        public bool GameOver { get; set; }

        [JsonIgnore]
        private uint?[,] _moves = new uint?[3, 3];

        public bool IsGameAgainst(uint player1Id, uint player2Id) =>
            (Player1Id == player1Id && Player2Id == player2Id) || (Player1Id == player2Id && Player2Id == player1Id);

        public bool IsParticipant(uint player1Id) =>
            Player1Id == player1Id || Player2Id == player1Id;

        public Game Clone()
        {
            var moves = new uint?[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    moves[i, j] = _moves[i, j];
                }
            }

            return new Game
            {
                Id = Id,
                Cas = Cas,
                Player1Id = Player1Id,
                Player2Id = Player2Id,
                _moves = moves,
                WinnerId = WinnerId,
                GameOver = GameOver
            };
        }

        public void MakeMove(Move move)
        {
            CheckGameIsFinished();
            CheckCoordinates(move);
            CheckParticipants(move);
            CheckMoveIsPossible(move);
            CheckMoveIsRightOrder(move);

            _moves[move.X, move.Y] = move.PlayerId;

            TryEndGame();
        }

        private void TryEndGame()
        {
            for (int i = 0; i < 3 && !GameOver; i++)
            {
                CheckLine(new Tuple<int, int>(i, 0), new Tuple<int, int>(i, 1), new Tuple<int, int>(i, 2));
            }

            for (int i = 0; i < 3 && !GameOver; i++)
            {
                CheckLine(new Tuple<int, int>(0, i), new Tuple<int, int>(1, i), new Tuple<int, int>(2, i));
            }

            CheckLine(new Tuple<int, int>(0, 0), new Tuple<int, int>(1, 1), new Tuple<int, int>(2, 2));
            CheckLine(new Tuple<int, int>(2, 0), new Tuple<int, int>(1, 1), new Tuple<int, int>(0, 2));

            CheckDraw();
        }

        private void CheckLine(Tuple<int, int> point1, Tuple<int, int> point2, Tuple<int, int> point3)
        {
            if (_moves[point1.Item1, point1.Item2] != null &&
                _moves[point1.Item1, point1.Item2] == _moves[point2.Item1, point2.Item2] &&
                _moves[point2.Item1, point2.Item2] == _moves[point3.Item1, point3.Item2])
            {
                GameOver = true;
                WinnerId = _moves[point1.Item1, point1.Item2].Value;
            }
        }

        private void CheckDraw()
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (_moves[i, j] == null)
                    {
                        return;
                    }
                }
            }

            GameOver = true;
            WinnerId = null;
        }

        private void CheckMoveIsRightOrder(Move move)
        {
            var moves = 0;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (_moves[i, j] != null)
                    {
                        moves++;
                    }
                }
            }

            if ((move.PlayerId == Player1Id && moves % 2 != 0) || 
                (move.PlayerId == Player2Id && moves % 2 == 0))
            {
                throw new IncorrectMoveException("It's not your move.");
            }
        }

        private void CheckMoveIsPossible(Move move)
        {
            if (_moves[move.X, move.Y] != null)
            {
                throw new IncorrectMoveException("Can't make a move for one point twice.");
            }
        }

        private void CheckGameIsFinished()
        {
            if (GameOver)
            {
                throw new IncorrectMoveException("This game is already finished.");
            }
        }

        private void CheckParticipants(Move move)
        {
            if (!IsParticipant(move.PlayerId))
            {
                throw new IncorrectMoveException("You are not a participant of this game.");
            }
        }

        private static void CheckCoordinates(Move move)
        {
            if (move.X < 0 || move.X > 2 || move.Y < 0 || move.Y > 2)
            {
                throw new IncorrectMoveException("Incorrect coordinates.");
            }
        }
    }
}