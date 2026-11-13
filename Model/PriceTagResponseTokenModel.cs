using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace SmartSoftware.SmartAgent.Model
{
    [DataContract]
    public class PriceTagResponseTokenModel
    {
        [DataMember(Name = "code")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string? Code { get; set; }

        [DataMember(Name = "message")]
        public string? Message { get; set; }

        [DataMember(Name = "body")]
        public string? Body { get; set; }
    }
}
