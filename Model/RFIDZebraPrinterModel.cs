using System.Text.Json.Serialization;

namespace SmartSoftware.SmartAgent.Model
{
    public class RFIDZebraPrinterModel
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("uid")]
        public string? Uid { get; set; }

        [JsonPropertyName("connection")]
        public string? Connection { get; set; }

        [JsonPropertyName("deviceType")]
        public string? DeviceType { get; set; }

        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("provider")]
        public string? Provider { get; set; }

        [JsonPropertyName("manufacturer")]
        public string? Manufacturer { get; set; }
    }
}
