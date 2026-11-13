namespace SmartSoftware.DeviceAgent.Model
{
    public class BarcodeScaleSendRequest
    {
        public string ScaleType { get; set; }
        public string IpAddress { get; set; }
        public int Port { get; set; }
        public List<BarcodeScaleItemModel> Items { get; set; }
        public string? ScaleBarCodeType { get; set; }
    }
}
