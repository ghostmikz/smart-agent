namespace SmartSoftware.SmartAgent.Model
{
    public class RFIDPrintRequest
    {
        // "1000" = CP30, "1001" = ZT411
        public string PrinterType { get; set; } = string.Empty;
        public string RfidId { get; set; } = string.Empty;

        // Label image rendered by the caller (report engine), BMP bytes base64-encoded.
        public string ImageBase64 { get; set; } = string.Empty;
    }
}
