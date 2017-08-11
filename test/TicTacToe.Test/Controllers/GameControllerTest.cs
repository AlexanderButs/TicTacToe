using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

using Newtonsoft.Json;

using NUnit.Framework;

using TicTacToe.Model;

namespace TicTacToe.Test.Controllers
{
    [TestFixture]
    public class GameControllerTest
    {
        [Test]
        public async Task ShouldCreateNewGame()
        {
            using (var server = CreateTestServer())
            {
                using (var client = server.CreateClient())
                {
                    var postResponse = await client.PostAsync(
                        "api/game?player1Id=1&player2Id=2", null);

                    postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
                    var postData = await postResponse.Content.ReadAsStringAsync();
                    var gameCreated = JsonConvert.DeserializeObject<GameCreated>(postData);
                    gameCreated.GameId.Should().NotBeNullOrEmpty();

                    var getResponse = await client.GetAsync($"api/game/{gameCreated.GameId}");
                    getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
                    var getData = await getResponse.Content.ReadAsStringAsync();
                    var game = JsonConvert.DeserializeObject<Game>(getData);
                    game.Should().NotBeNull();
                }
            }
        }

        [Test]
        public async Task ShouldNotAllowCreateGameWithTheSamePlayer()
        {
            using (var server = CreateTestServer())
            {
                using (var client = server.CreateClient())
                {
                    var postResponse = await client.PostAsync(
                        "api/game?player1Id=1&player2Id=1", null);

                    postResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
                }
            }
        }

        [Test]
        public async Task ShouldNotAllowCreateTwoConcurrentGamesWithTheSamePlayers()
        {
            using (var server = CreateTestServer())
            {
                using (var client = server.CreateClient())
                {
                    var postResponse = await client.PostAsync(
                        "api/game?player1Id=1&player2Id=2", null);
                    postResponse.StatusCode.Should().Be(HttpStatusCode.OK);

                    postResponse = await client.PostAsync(
                        "api/game?player1Id=1&player2Id=2", null);
                    postResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
                }
            }
        }

        [Test]
        public async Task ShouldAllowCreateNextGameWithTheSamePlayersAfterCurrentGameIsFinished()
        {
            using (var server = CreateTestServer())
            {
                using (var client = server.CreateClient())
                {
                    var postResponse = await client.PostAsync(
                        "api/game?player1Id=1&player2Id=2", null);
                    var postData = await postResponse.Content.ReadAsStringAsync();
                    var gameCreated = JsonConvert.DeserializeObject<GameCreated>(postData);

                    await MakeMove(client, gameCreated.GameId, "1", 0, 0);
                    await MakeMove(client, gameCreated.GameId, "2", 1, 0);
                    await MakeMove(client, gameCreated.GameId, "1", 0, 1);
                    await MakeMove(client, gameCreated.GameId, "2", 2, 0);
                    await MakeMove(client, gameCreated.GameId, "1", 0, 2);

                    postResponse = await client.PostAsync(
                        "api/game?player1Id=1&player2Id=2", null);
                    postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
                }
            }
        }

        [Test]
        public async Task ShouldNotAllowMakeMoveForNotFoundGame()
        {
            using (var server = CreateTestServer())
            {
                using (var client = server.CreateClient())
                {
                    var move = new Move { PlayerId = "3", X = 0, Y = 0 };
                    var content = new StringContent(JsonConvert.SerializeObject(move), Encoding.UTF8, "application/json");
                    var putResponse = await client.PutAsync(
                        $"api/game/{Guid.NewGuid()}", content);
                    putResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
                }
            }
        }

        [Test]
        public async Task ShouldNotAllowMakeMoveForAnotherGame()
        {
            using (var server = CreateTestServer())
            {
                using (var client = server.CreateClient())
                {
                    var postResponse = await client.PostAsync(
                        "api/game?player1Id=1&player2Id=2", null);
                    var postData = await postResponse.Content.ReadAsStringAsync();
                    var gameCreated = JsonConvert.DeserializeObject<GameCreated>(postData);

                    var putResponse = await MakeMove(client, gameCreated.GameId, "3", 0, 0);
                    putResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
                }
            }
        }

        [Test]
        public async Task ShouldNotAllowMakeMoveWithIncorrectCoordinates()
        {
            using (var server = CreateTestServer())
            {
                using (var client = server.CreateClient())
                {
                    var postResponse = await client.PostAsync(
                        "api/game?player1Id=1&player2Id=2", null);
                    var postData = await postResponse.Content.ReadAsStringAsync();
                    var gameCreated = JsonConvert.DeserializeObject<GameCreated>(postData);

                    var putResponse = await MakeMove(client, gameCreated.GameId, "1", -1, -1);
                    putResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                    putResponse = await MakeMove(client, gameCreated.GameId, "2", 3, 3);
                    putResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
                }
            }
        }

        [Test]
        public async Task ShouldNotAllowMakeMoveForOnePointTwice()
        {
            using (var server = CreateTestServer())
            {
                using (var client = server.CreateClient())
                {
                    var postResponse = await client.PostAsync(
                        "api/game?player1Id=1&player2Id=2", null);
                    var postData = await postResponse.Content.ReadAsStringAsync();
                    var gameCreated = JsonConvert.DeserializeObject<GameCreated>(postData);

                    var putResponse1 = await MakeMove(client, gameCreated.GameId, "1", 1, 1);
                    putResponse1.StatusCode.Should().Be(HttpStatusCode.OK);

                    var putResponse2 = await MakeMove(client, gameCreated.GameId, "1", 1, 1);
                    putResponse2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
                }
            }
        }

        [Test]
        public async Task ShouldNotAllowMakeMoveInIncorrectOrder()
        {
            using (var server = CreateTestServer())
            {
                using (var client = server.CreateClient())
                {
                    var postResponse = await client.PostAsync(
                        "api/game?player1Id=1&player2Id=2", null);
                    var postData = await postResponse.Content.ReadAsStringAsync();
                    var gameCreated = JsonConvert.DeserializeObject<GameCreated>(postData);

                    var putResponse1 = await MakeMove(client, gameCreated.GameId, "2", 1, 1);
                    putResponse1.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                    var putResponse2 = await MakeMove(client, gameCreated.GameId, "1", 1, 1);
                    putResponse2.StatusCode.Should().Be(HttpStatusCode.OK);

                    var putResponse3 = await MakeMove(client, gameCreated.GameId, "1", 2, 2);
                    putResponse3.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                    var putResponse4 = await MakeMove(client, gameCreated.GameId, "2", 2, 2);
                    putResponse4.StatusCode.Should().Be(HttpStatusCode.OK);

                    var putResponse5 = await MakeMove(client, gameCreated.GameId, "2", 2, 2);
                    putResponse5.StatusCode.Should().Be(HttpStatusCode.BadRequest);
                }
            }
        }

        [Test]
        public async Task ShouldFirstPlayerWin()
        {
            using (var server = CreateTestServer())
            {
                using (var client = server.CreateClient())
                {
                    var postResponse = await client.PostAsync(
                        "api/game?player1Id=1&player2Id=2", null);
                    var postData = await postResponse.Content.ReadAsStringAsync();
                    var gameCreated = JsonConvert.DeserializeObject<GameCreated>(postData);

                    await MakeMove(client, gameCreated.GameId, "1", 0, 0);
                    await MakeMove(client, gameCreated.GameId, "2", 1, 0);
                    await MakeMove(client, gameCreated.GameId, "1", 0, 1);
                    await MakeMove(client, gameCreated.GameId, "2", 2, 0);
                    await MakeMove(client, gameCreated.GameId, "1", 0, 2);

                    var getResponse = await client.GetAsync($"api/game/{gameCreated.GameId}");
                    getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
                    var getData = await getResponse.Content.ReadAsStringAsync();
                    var game = JsonConvert.DeserializeObject<Game>(getData);
                    game.GameOver.Should().BeTrue();
                    game.WinnerId.Should().Be("1");
                }
            }
        }

        [Test]
        public async Task ShouldSecondPlayerWin()
        {
            using (var server = CreateTestServer())
            {
                using (var client = server.CreateClient())
                {
                    var postResponse = await client.PostAsync(
                        "api/game?player1Id=1&player2Id=2", null);
                    var postData = await postResponse.Content.ReadAsStringAsync();
                    var gameCreated = JsonConvert.DeserializeObject<GameCreated>(postData);

                    await MakeMove(client, gameCreated.GameId, "1", 0, 0);
                    await MakeMove(client, gameCreated.GameId, "2", 1, 0);
                    await MakeMove(client, gameCreated.GameId, "1", 0, 1);
                    await MakeMove(client, gameCreated.GameId, "2", 1, 1);
                    await MakeMove(client, gameCreated.GameId, "1", 2, 2);
                    await MakeMove(client, gameCreated.GameId, "2", 1, 2);

                    var getResponse = await client.GetAsync($"api/game/{gameCreated.GameId}");
                    getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
                    var getData = await getResponse.Content.ReadAsStringAsync();
                    var game = JsonConvert.DeserializeObject<Game>(getData);
                    game.GameOver.Should().BeTrue();
                    game.WinnerId.Should().Be("2");
                }
            }
        }

        [Test]
        public async Task ShouldBeDraw()
        {
            using (var server = CreateTestServer())
            {
                using (var client = server.CreateClient())
                {
                    var postResponse = await client.PostAsync(
                        "api/game?player1Id=1&player2Id=2", null);
                    var postData = await postResponse.Content.ReadAsStringAsync();
                    var gameCreated = JsonConvert.DeserializeObject<GameCreated>(postData);

                    await MakeMove(client, gameCreated.GameId, "1", 0, 0);
                    await MakeMove(client, gameCreated.GameId, "2", 0, 1);
                    await MakeMove(client, gameCreated.GameId, "1", 0, 2);
                    await MakeMove(client, gameCreated.GameId, "2", 1, 1);
                    await MakeMove(client, gameCreated.GameId, "1", 1, 0);
                    await MakeMove(client, gameCreated.GameId, "2", 1, 2);
                    await MakeMove(client, gameCreated.GameId, "1", 2, 1);
                    await MakeMove(client, gameCreated.GameId, "2", 2, 0);
                    await MakeMove(client, gameCreated.GameId, "1", 2, 2);

                    var getResponse = await client.GetAsync($"api/game/{gameCreated.GameId}");
                    getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
                    var getData = await getResponse.Content.ReadAsStringAsync();
                    var game = JsonConvert.DeserializeObject<Game>(getData);
                    game.GameOver.Should().BeTrue();
                    game.WinnerId.Should().BeNull();
                }
            }
        }

        [Test]
        public async Task ShouldNotAllowMakeMoveForFinishedGame()
        {
            using (var server = CreateTestServer())
            {
                using (var client = server.CreateClient())
                {
                    var postResponse = await client.PostAsync(
                        "api/game?player1Id=1&player2Id=2", null);
                    var postData = await postResponse.Content.ReadAsStringAsync();
                    var gameCreated = JsonConvert.DeserializeObject<GameCreated>(postData);

                    await MakeMove(client, gameCreated.GameId, "1", 0, 0);
                    await MakeMove(client, gameCreated.GameId, "2", 1, 0);
                    await MakeMove(client, gameCreated.GameId, "1", 0, 1);
                    await MakeMove(client, gameCreated.GameId, "2", 2, 0);
                    await MakeMove(client, gameCreated.GameId, "1", 0, 2);

                    var putResponse = await MakeMove(client, gameCreated.GameId, "2", 2, 2);
                    putResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
                }
            }
        }

        private static TestServer CreateTestServer()
        {
            var webHostBuilder = new WebHostBuilder()
                .UseStartup<Startup>();
            return new TestServer(webHostBuilder);
        }

        private static async Task<HttpResponseMessage> MakeMove(HttpClient client, string gameId, string playerId, int x, int y)
        {
            var move = new Move { PlayerId = playerId, X = x, Y = y };
            var content = new StringContent(JsonConvert.SerializeObject(move), Encoding.UTF8, "application/json");
            return await client.PutAsync(
                $"api/game/{gameId}", content);
        }
    }
}