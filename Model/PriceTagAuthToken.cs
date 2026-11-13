using System.Text.Json.Serialization;

namespace SmartSoftware.SmartAgent.Model
{
    public class PriceTagAuthToken
    {
        [JsonPropertyName("userName")]
        public string UserName { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;
    }
}
