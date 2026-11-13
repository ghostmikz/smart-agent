namespace SmartSoftware.SmartAgent.Model
{
    public class RFIDPrinterConfig
    {
        public string Model { get; set; } = string.Empty;
        public string PrinterID { get; set; } = string.Empty;
        public string Interface { get; set; } = string.Empty;
        public List<RFIDPrinter> Printers { get; set; } = new();
    }

    public class RFIDPrinter
    {
        public List<RFIDItem> Items { get; set; } = new();
    }

    public class RFIDItem
    {
        public string ItemType { get; set; } = string.Empty;
        public string? Format { get; set; }
        public int? Begin { get; set; }
        public int? Size { get; set; }
        public string? MemoryBlock { get; set; }
        public string? Text { get; set; }
        public int? XPos { get; set; }
        public int? YPos { get; set; }
        public string? ImgName { get; set; }
    }
}
