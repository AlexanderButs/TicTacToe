using System;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using TicTacToe.Exceptions;
using TicTacToe.Model;
using TicTacToe.Repositories;

namespace TicTacToe.Controllers
{
    [Route("api/game")]
    public class GameController : Controller
    {
        private readonly IGameRepository _gameRepository;

        public GameController(IGameRepository gameRepository)
        {
            _gameRepository = gameRepository;
        }

        [HttpPost]
        [ProducesResponseType(typeof(GameCreated), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(SerializableError), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Conflict)]
        public async Task<IActionResult> CreateGameAsync([FromQuery] uint player1Id, [FromQuery] uint player2Id)
        {
            if (player1Id == 0 || player2Id == 0 || player1Id == player2Id)
            {
                return BadRequest();
            }

            var isGameStarted = await _gameRepository.IsThereStartedGame(player1Id, player2Id);
            if (isGameStarted)
            {
                return StatusCode((int)HttpStatusCode.Conflict);
            }

            var game = new Game { Id = Guid.NewGuid().ToString(), Player1Id = player1Id, Player2Id = player2Id };
            await _gameRepository.AddGameAsync(game);

            var response = new GameCreated { GameId = game.Id };
            return Ok(response);
        }

        [HttpGet("{gameId}")]
        [ProducesResponseType(typeof(Game), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetGameAsync(string gameId)
        {
            var game = await _gameRepository.GetGameByIdAsync(gameId);
            if (game == null)
            {
                return NotFound();
            }

            return Ok(game);
        }

        [HttpPut("{gameId}")]
        [ProducesResponseType(typeof(Game), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(SerializableError), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> MakeMoveAsync(string gameId, [FromBody] Move move)
        {
            var game = await _gameRepository.GetGameByIdAsync(gameId);
            if (game == null)
            {
                return NotFound();
            }

            try
            {
                game.MakeMove(move);
            }
            catch (IncorrectMoveException ex)
            {
                return BadRequest(ex.Message);
            }

            await _gameRepository.UpdateGameAsync(game);
            return Ok();
        }
    }
}