using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;

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
            m_client.Dispose();
            m_server.Dispose();
        }

        [Test]
        public async Task ShouldReturnNotFoundIfThereIsNoGame()
        {
            var postResponse = await m_client.GetAsync("api/game/123456");
            postResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Test]
        public async Task ShouldCreateNewGame()
        {
            var postResponse = await m_client.PostAsync(
                "api/game?player1Id=1&player2Id=2", null);

            postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var gameCreated = await postResponse.Content.ReadAsAsync<GameCreated>();
            gameCreated.GameId.Should().NotBeNullOrEmpty();

            var getResponse = await m_client.GetAsync($"api/game/{gameCreated.GameId}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var getData = await getResponse.Content.ReadAsStringAsync();
            var game = await getResponse.Content.ReadAsAsync<Game>();
            game.Should().NotBeNull();
        }

        [Test]
        public async Task ShouldNotAllowCreateGameWithTheSamePlayer()
        {
            var postResponse = await m_client.PostAsync(
                "api/game?player1Id=1&player2Id=1", null);

            postResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task ShouldNotAllowCreateTwoConcurrentGamesWithTheSamePlayers()
        {
            var postResponse = await m_client.PostAsync(
                "api/game?player1Id=1&player2Id=2", null);
            postResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            postResponse = await m_client.PostAsync(
                "api/game?player1Id=1&player2Id=2", null);
            postResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Test]
        public async Task ShouldAllowCreateNextGameWithTheSamePlayersAfterCurrentGameIsFinished()
        {
            var postResponse = await m_client.PostAsync(
                "api/game?player1Id=1&player2Id=2", null);
            postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var gameCreated = await postResponse.Content.ReadAsAsync<GameCreated>();

            await MakeMove(m_client, gameCreated.GameId, "1", 0, 0);
            await MakeMove(m_client, gameCreated.GameId, "2", 1, 0);
            await MakeMove(m_client, gameCreated.GameId, "1", 0, 1);
            await MakeMove(m_client, gameCreated.GameId, "2", 2, 0);
            await MakeMove(m_client, gameCreated.GameId, "1", 0, 2);

            postResponse = await m_client.PostAsync(
                "api/game?player1Id=1&player2Id=2", null);
            postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Test]
        public async Task ShouldNotAllowMakeMoveForNotFoundGame()
        {
            var move = new Move { PlayerId = "3", X = 0, Y = 0 };
            var putResponse = await m_client.PutAsJsonAsync<Move>(
                $"api/game/{Guid.NewGuid()}", move);
            putResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Test]
        public async Task ShouldNotAllowMakeMoveForAnotherGame()
        {
            var postResponse = await m_client.PostAsync(
                "api/game?player1Id=1&player2Id=2", null);
            postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var gameCreated = await postResponse.Content.ReadAsAsync<GameCreated>();

            var putResponse = await MakeMove(m_client, gameCreated.GameId, "3", 0, 0);
            putResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task ShouldNotAllowMakeMoveWithIncorrectCoordinates()
        {
            var postResponse = await m_client.PostAsync(
                "api/game?player1Id=1&player2Id=2", null);
            postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var gameCreated = await postResponse.Content.ReadAsAsync<GameCreated>();

            var putResponse = await MakeMove(m_client, gameCreated.GameId, "1", -1, -1);
            putResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            putResponse = await MakeMove(m_client, gameCreated.GameId, "2", 3, 3);
            putResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task ShouldNotAllowMakeMoveForOnePointTwice()
        {
            var postResponse = await m_client.PostAsync(
                "api/game?player1Id=1&player2Id=2", null);
            postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var gameCreated = await postResponse.Content.ReadAsAsync<GameCreated>();

            var putResponse1 = await MakeMove(m_client, gameCreated.GameId, "1", 1, 1);
            putResponse1.StatusCode.Should().Be(HttpStatusCode.OK);

            var putResponse2 = await MakeMove(m_client, gameCreated.GameId, "1", 1, 1);
            putResponse2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task ShouldNotAllowMakeMoveInIncorrectOrder()
        {
            var postResponse = await m_client.PostAsync(
                "api/game?player1Id=1&player2Id=2", null);
            postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var gameCreated = await postResponse.Content.ReadAsAsync<GameCreated>();

            var putResponse1 = await MakeMove(m_client, gameCreated.GameId, "2", 1, 1);
            putResponse1.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var putResponse2 = await MakeMove(m_client, gameCreated.GameId, "1", 1, 1);
            putResponse2.StatusCode.Should().Be(HttpStatusCode.OK);

            var putResponse3 = await MakeMove(m_client, gameCreated.GameId, "1", 2, 2);
            putResponse3.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var putResponse4 = await MakeMove(m_client, gameCreated.GameId, "2", 2, 2);
            putResponse4.StatusCode.Should().Be(HttpStatusCode.OK);

            var putResponse5 = await MakeMove(m_client, gameCreated.GameId, "2", 2, 2);
            putResponse5.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task ShouldFirstPlayerWin()
        {
            var postResponse = await m_client.PostAsync(
                "api/game?player1Id=1&player2Id=2", null);
            postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var gameCreated = await postResponse.Content.ReadAsAsync<GameCreated>();

            await MakeMove(m_client, gameCreated.GameId, "1", 0, 0);
            await MakeMove(m_client, gameCreated.GameId, "2", 1, 0);
            await MakeMove(m_client, gameCreated.GameId, "1", 0, 1);
            await MakeMove(m_client, gameCreated.GameId, "2", 2, 0);
            await MakeMove(m_client, gameCreated.GameId, "1", 0, 2);

            var getResponse = await m_client.GetAsync($"api/game/{gameCreated.GameId}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var getData = await getResponse.Content.ReadAsStringAsync();
            var game = await getResponse.Content.ReadAsAsync<Game>();
            game.GameOver.Should().BeTrue();
            game.WinnerId.Should().Be("1");
        }

        [Test]
        public async Task ShouldSecondPlayerWin()
        {
            var postResponse = await m_client.PostAsync(
                "api/game?player1Id=1&player2Id=2", null);
            postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var gameCreated = await postResponse.Content.ReadAsAsync<GameCreated>();

            await MakeMove(m_client, gameCreated.GameId, "1", 0, 0);
            await MakeMove(m_client, gameCreated.GameId, "2", 1, 0);
            await MakeMove(m_client, gameCreated.GameId, "1", 0, 1);
            await MakeMove(m_client, gameCreated.GameId, "2", 1, 1);
            await MakeMove(m_client, gameCreated.GameId, "1", 2, 2);
            await MakeMove(m_client, gameCreated.GameId, "2", 1, 2);

            var getResponse = await m_client.GetAsync($"api/game/{gameCreated.GameId}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var game = await getResponse.Content.ReadAsAsync<Game>();
            game.GameOver.Should().BeTrue();
            game.WinnerId.Should().Be("2");
        }

        [Test]
        public async Task ShouldBeDraw()
        {
            var postResponse = await m_client.PostAsync(
                "api/game?player1Id=1&player2Id=2", null);
            postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var gameCreated = await postResponse.Content.ReadAsAsync<GameCreated>();

            await MakeMove(m_client, gameCreated.GameId, "1", 0, 0);
            await MakeMove(m_client, gameCreated.GameId, "2", 0, 1);
            await MakeMove(m_client, gameCreated.GameId, "1", 0, 2);
            await MakeMove(m_client, gameCreated.GameId, "2", 1, 1);
            await MakeMove(m_client, gameCreated.GameId, "1", 1, 0);
            await MakeMove(m_client, gameCreated.GameId, "2", 1, 2);
            await MakeMove(m_client, gameCreated.GameId, "1", 2, 1);
            await MakeMove(m_client, gameCreated.GameId, "2", 2, 0);
            await MakeMove(m_client, gameCreated.GameId, "1", 2, 2);

            var getResponse = await m_client.GetAsync($"api/game/{gameCreated.GameId}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var game = await getResponse.Content.ReadAsAsync<Game>();
            game.GameOver.Should().BeTrue();
            game.WinnerId.Should().BeNull();
        }

        [Test]
        public async Task ShouldNotAllowMakeMoveForFinishedGame()
        {
            var postResponse = await m_client.PostAsync(
                "api/game?player1Id=1&player2Id=2", null);
            postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var gameCreated = await postResponse.Content.ReadAsAsync<GameCreated>();

            await MakeMove(m_client, gameCreated.GameId, "1", 0, 0);
            await MakeMove(m_client, gameCreated.GameId, "2", 1, 0);
            await MakeMove(m_client, gameCreated.GameId, "1", 0, 1);
            await MakeMove(m_client, gameCreated.GameId, "2", 2, 0);
            await MakeMove(m_client, gameCreated.GameId, "1", 0, 2);

            var putResponse = await MakeMove(m_client, gameCreated.GameId, "2", 2, 2);
            putResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        private static TestServer CreateTestServer()
        {
            var webHostBuilder = new WebHostBuilder()
                .UseStartup<Startup>();
            return new TestServer(webHostBuilder);
        }

        private static Task<HttpResponseMessage> MakeMove(HttpClient client, string gameId, string playerId, int x, int y)
        {
            var move = new Move { PlayerId = playerId, X = x, Y = y };
            return client.PutAsJsonAsync<Move>($"api/game/{gameId}", move);
        }
    }
}