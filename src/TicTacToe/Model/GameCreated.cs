using Newtonsoft.Json;

namespace TicTacToe.Model
{
    public class GameCreated
    {
        [JsonProperty("gameId")]
        public string GameId { get; set; }
    }
}