using System.Text.Json.Serialization;

namespace SmartSoftware.DeviceAgent.Model
{
    public class RFIDResponse
    {
        [JsonPropertyName("taskid")]
        public string? TaskId { get; set; }

        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("fun")]
        public string? Fun { get; set; }

        [JsonPropertyName("msg")]
        public string? Msg { get; set; }

        [JsonPropertyName("data")]
        public string? Data { get; set; }
    }
}
