namespace SmartSoftware.DeviceAgent.Model
{
    public class PriceTagSendRequest
    {
        public string PriceTagURL { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string StoreId { get; set; } = string.Empty;
        public List<string> TagIdList { get; set; } = new();
    }
}
