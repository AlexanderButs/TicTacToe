using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;

using AutoFixture.NUnit3;

using FluentAssertions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

using NUnit.Framework;

using TicTacToe.Model;

namespace TicTacToe.Test.Controllers
{
    [TestFixture]
    public class GameControllerTest
    {
        private TestServer m_server;
        private HttpClient m_client;

        [SetUp]
        public void SetUp()
        {
            m_server = CreateTestServer();
            m_client = m_server.CreateClient();
        }

        [TearDown]
        public void TearDown()
        {
            m_client?.Dispose();
            m_server?.Dispose();
        }

        [Test, AutoData]
        public async Task ShouldReturnNotFoundIfThereIsNoGame(uint usetId)
        {
            var postResponse = await m_client.GetAsync($"api/game/{usetId}");
            postResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Test, AutoData]
        public async Task ShouldCreateNewGame(uint player1Id, uint player2Id)
        {
            var postResponse = await m_client.PostAsync(
                $"api/game?player1Id={player1Id}&player2Id={player2Id}", null);

            postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var gameCreated = await postResponse.Content.ReadAsAsync<GameCreated>();
            gameCreated.GameId.Should().NotBeNullOrEmpty();

            var getResponse = await m_client.GetAsync($"api/game/{gameCreated.GameId}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var getData = await getResponse.Content.ReadAsStringAsync();
            var game = await getResponse.Content.ReadAsAsync<Game>();
            game.Should().NotBeNull();
        }

        [Test, AutoData]
        public async Task ShouldNotAllowCreateGameWithStringPlayerId(string stringPlayerId, uint playerId)
        {
            var postResponse = await m_client.PostAsync(
                $"api/game?player1Id={stringPlayerId}&player2Id={playerId}", null);
            postResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            postResponse = await m_client.PostAsync(
                $"api/game?player1Id={playerId}&player2Id={stringPlayerId}", null);
            postResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Test, AutoData]
        public async Task ShouldNotAllowCreateGameWithTheSamePlayer(uint player1Id)
        {
            var postResponse = await m_client.PostAsync(
                $"api/game?player1Id={player1Id}&player2Id={player1Id}", null);

            postResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Test, AutoData]
        public async Task ShouldNotAllowCreateTwoConcurrentGamesWithTheSamePlayers(
            uint player1Id, uint player2Id)
        {
            var postResponse = await m_client.PostAsync(
                $"api/game?player1Id={player1Id}&player2Id={player2Id}", null);
            postResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            postResponse = await m_client.PostAsync(
                $"api/game?player1Id={player1Id}&player2Id={player2Id}", null);
            postResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Test, AutoData]
        public async Task ShouldAllowCreateNextGameWithTheSamePlayersAfterCurrentGameIsFinished(
            uint player1Id, uint player2Id)
        {
            var postResponse = await m_client.PostAsync(
                $"api/game?player1Id={player1Id}&player2Id={player2Id}", null);
            postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var gameCreated = await postResponse.Content.ReadAsAsync<GameCreated>();

            await MakeMove(m_client, gameCreated.GameId, player1Id, 0, 0);
            await MakeMove(m_client, gameCreated.GameId, player2Id, 1, 0);
            await MakeMove(m_client, gameCreated.GameId, player1Id, 0, 1);
            await MakeMove(m_client, gameCreated.GameId, player2Id, 2, 0);
            await MakeMove(m_client, gameCreated.GameId, player1Id, 0, 2);

            postResponse = await m_client.PostAsync(
                "api/game?player1Id=1&player2Id=2", null);
            postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Test, AutoData]
        public async Task ShouldNotAllowMakeMoveForNotFoundGame(uint playerId, Guid mapId)
        {
            var move = new Move { PlayerId = playerId, X = 0, Y = 0 };
            var putResponse = await m_client.PutAsJsonAsync<Move>($"api/game/{mapId}", move);
            putResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Test, AutoData]
        public async Task ShouldNotAllowMakeMoveForAnotherGame(
            uint player1Id, uint player2Id, uint wrongPlayerId)
        {
            var postResponse = await m_client.PostAsync(
                $"api/game?player1Id={player1Id}&player2Id={player2Id}", null);
            postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var gameCreated = await postResponse.Content.ReadAsAsync<GameCreated>();

            var putResponse = await MakeMove(m_client, gameCreated.GameId, wrongPlayerId, 0, 0);
            putResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Test, AutoData]
        public async Task ShouldNotAllowMakeMoveWithIncorrectCoordinates(uint player1Id, uint player2Id)
        {
            var postResponse = await m_client.PostAsync(
                $"api/game?player1Id={player1Id}&player2Id={player2Id}", null);
            postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var gameCreated = await postResponse.Content.ReadAsAsync<GameCreated>();

            var putResponse = await MakeMove(m_client, gameCreated.GameId, player1Id, -1, -1);
            putResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            putResponse = await MakeMove(m_client, gameCreated.GameId, player2Id, 3, 3);
            putResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Test, AutoData]
        public async Task ShouldNotAllowMakeMoveForOnePointTwice(uint player1Id, uint player2Id)
        {
            var postResponse = await m_client.PostAsync(
                $"api/game?player1Id={player1Id}&player2Id={player2Id}", null);
            postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var gameCreated = await postResponse.Content.ReadAsAsync<GameCreated>();

            var putResponse1 = await MakeMove(m_client, gameCreated.GameId, player1Id, 1, 1);
            putResponse1.StatusCode.Should().Be(HttpStatusCode.OK);

            var putResponse2 = await MakeMove(m_client, gameCreated.GameId, player1Id, 1, 1);
            putResponse2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Test, AutoData]
        public async Task ShouldNotAllowMakeMoveInIncorrectOrder(uint player1Id, uint player2Id)
        {
            var postResponse = await m_client.PostAsync(
                $"api/game?player1Id={player1Id}&player2Id={player2Id}", null);
            postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var gameCreated = await postResponse.Content.ReadAsAsync<GameCreated>();

            var putResponse1 = await MakeMove(m_client, gameCreated.GameId, player2Id, 1, 1);
            putResponse1.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var putResponse2 = await MakeMove(m_client, gameCreated.GameId, player1Id, 1, 1);
            putResponse2.StatusCode.Should().Be(HttpStatusCode.OK);

            var putResponse3 = await MakeMove(m_client, gameCreated.GameId, player1Id, 2, 2);
            putResponse3.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var putResponse4 = await MakeMove(m_client, gameCreated.GameId, player2Id, 2, 2);
            putResponse4.StatusCode.Should().Be(HttpStatusCode.OK);

            var putResponse5 = await MakeMove(m_client, gameCreated.GameId, player2Id, 2, 2);
            putResponse5.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Test, AutoData]
        public async Task ShouldFirstPlayerWin(uint player1Id, uint player2Id)
        {
            var postResponse = await m_client.PostAsync(
                $"api/game?player1Id={player1Id}&player2Id={player2Id}", null);
            postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var gameCreated = await postResponse.Content.ReadAsAsync<GameCreated>();

            await MakeMove(m_client, gameCreated.GameId, player1Id, 0, 0);
            await MakeMove(m_client, gameCreated.GameId, player2Id, 1, 0);
            await MakeMove(m_client, gameCreated.GameId, player1Id, 0, 1);
            await MakeMove(m_client, gameCreated.GameId, player2Id, 2, 0);
            await MakeMove(m_client, gameCreated.GameId, player1Id, 0, 2);

            var getResponse = await m_client.GetAsync($"api/game/{gameCreated.GameId}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var getData = await getResponse.Content.ReadAsStringAsync();
            var game = await getResponse.Content.ReadAsAsync<Game>();
            game.GameOver.Should().BeTrue();
            game.WinnerId.Should().Be(player1Id);
        }

        [Test, AutoData]
        public async Task ShouldSecondPlayerWin(uint player1Id, uint player2Id)
        {
            var postResponse = await m_client.PostAsync(
                $"api/game?player1Id={player1Id}&player2Id={player2Id}", null);
            postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var gameCreated = await postResponse.Content.ReadAsAsync<GameCreated>();

            await MakeMove(m_client, gameCreated.GameId, player1Id, 0, 0);
            await MakeMove(m_client, gameCreated.GameId, player2Id, 1, 0);
            await MakeMove(m_client, gameCreated.GameId, player1Id, 0, 1);
            await MakeMove(m_client, gameCreated.GameId, player2Id, 1, 1);
            await MakeMove(m_client, gameCreated.GameId, player1Id, 2, 2);
            await MakeMove(m_client, gameCreated.GameId, player2Id, 1, 2);

            var getResponse = await m_client.GetAsync($"api/game/{gameCreated.GameId}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var game = await getResponse.Content.ReadAsAsync<Game>();
            game.GameOver.Should().BeTrue();
            game.WinnerId.Should().Be(player2Id);
        }

        [Test, AutoData]
        public async Task ShouldBeDraw(uint player1Id, uint player2Id)
        {
            var postResponse = await m_client.PostAsync(
                $"api/game?player1Id={player1Id}&player2Id={player2Id}", null);
            postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var gameCreated = await postResponse.Content.ReadAsAsync<GameCreated>();

            await MakeMove(m_client, gameCreated.GameId, player1Id, 0, 0);
            await MakeMove(m_client, gameCreated.GameId, player2Id, 0, 1);
            await MakeMove(m_client, gameCreated.GameId, player1Id, 0, 2);
            await MakeMove(m_client, gameCreated.GameId, player2Id, 1, 1);
            await MakeMove(m_client, gameCreated.GameId, player1Id, 1, 0);
            await MakeMove(m_client, gameCreated.GameId, player2Id, 1, 2);
            await MakeMove(m_client, gameCreated.GameId, player1Id, 2, 1);
            await MakeMove(m_client, gameCreated.GameId, player2Id, 2, 0);
            await MakeMove(m_client, gameCreated.GameId, player1Id, 2, 2);

            var getResponse = await m_client.GetAsync($"api/game/{gameCreated.GameId}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var game = await getResponse.Content.ReadAsAsync<Game>();
            game.GameOver.Should().BeTrue();
            game.WinnerId.Should().BeNull();
        }

        [Test, AutoData]
        public async Task ShouldNotAllowMakeMoveForFinishedGame(uint player1Id, uint player2Id)
        {
            var postResponse = await m_client.PostAsync(
                $"api/game?player1Id={player1Id}&player2Id={player2Id}", null);
            postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var gameCreated = await postResponse.Content.ReadAsAsync<GameCreated>();

            await MakeMove(m_client, gameCreated.GameId, player1Id, 0, 0);
            await MakeMove(m_client, gameCreated.GameId, player2Id, 1, 0);
            await MakeMove(m_client, gameCreated.GameId, player1Id, 0, 1);
            await MakeMove(m_client, gameCreated.GameId, player2Id, 2, 0);
            await MakeMove(m_client, gameCreated.GameId, player1Id, 0, 2);

            var putResponse = await MakeMove(m_client, gameCreated.GameId, player2Id, 2, 2);
            putResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        private static TestServer CreateTestServer()
        {
            var webHostBuilder = new WebHostBuilder()
                .UseStartup<Startup>();
            return new TestServer(webHostBuilder);
        }

        private static Task<HttpResponseMessage> MakeMove(
            HttpClient client, string gameId, uint playerId, int x, int y)
        {
            var move = new Move { PlayerId = playerId, X = x, Y = y };
            return client.PutAsJsonAsync<Move>($"api/game/{gameId}", move);
        }
    }
}