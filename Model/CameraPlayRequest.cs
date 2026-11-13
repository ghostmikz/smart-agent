namespace SmartSoftware.SmartAgent.Model
{
    public class CameraPlayRequest
    {
        public string DeviceType { get; set; } = string.Empty;
        public string Server { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public long StartMs { get; set; }
        public long DurationMs { get; set; }
        public string VideoName { get; set; } = string.Empty;
    }

    public class CameraDeviceListRequest
    {
        public string DeviceType { get; set; } = string.Empty;
        public string Server { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
