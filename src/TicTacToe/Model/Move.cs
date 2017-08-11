using Newtonsoft.Json;

namespace TicTacToe.Model
{
    public class Move
    {
        [JsonProperty("playerId")]
        public string PlayerId { get; set; }

        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }
    }
}