using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Nito.AsyncEx;

using TicTacToe.Model;

namespace TicTacToe.Repositories
{
    public interface IGameRepository
    {
        Task<Game> GetGameByIdAsync(string gameId);

        Task<bool> IsThereStartedGame(uint player1Id, uint player2Id);

        Task AddGameAsync(Game game);

        Task UpdateGameAsync(Game game);
    }

    public class GameRepository : IGameRepository
    {
        private readonly AsyncLock _mutex = new AsyncLock();

        private readonly Dictionary<string, Game> _gameById = new Dictionary<string, Game>();

        public async Task<Game> GetGameByIdAsync(string gameId)
        {
            using (await _mutex.LockAsync())
            {
                Game game;
                if (_gameById.TryGetValue(gameId, out game))
                {
                    return game.Clone();
                }

                return null;
            }
        }

        public async Task<bool> IsThereStartedGame(uint player1Id, uint player2Id)
        {
            using (await _mutex.LockAsync())
            {
                return IsThereStartedGameImpl(player1Id, player2Id);
            }
        }

        public async Task AddGameAsync(Game game)
        {
            using (await _mutex.LockAsync())
            {
                if (IsThereStartedGameImpl(game.Player1Id, game.Player2Id))
                {
                    throw new InvalidOperationException();
                }

                game.Cas++;
                _gameById[game.Id] = game;
            }
        }

        public async Task UpdateGameAsync(Game game)
        {
            using (await _mutex.LockAsync())
            {
                Game g;
                if (_gameById.TryGetValue(game.Id, out g))
                {
                    if (g.Cas != game.Cas)
                    {
                        throw new InvalidOperationException();
                    }
                }

                game.Cas++;
                _gameById[game.Id] = game;
            }
        }

        private bool IsThereStartedGameImpl(uint player1Id, uint player2Id) =>
            _gameById.Values.Any(g => g.IsGameAgainst(player1Id, player2Id) && !g.GameOver);
    }
}