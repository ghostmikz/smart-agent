namespace SmartSoftware.DeviceAgent.Model
{
    public class PriceTagSendItemsRequest
    {
        public string PriceTagURL { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string StoreId { get; set; } = string.Empty;
        public List<PriceTagSendItem> Items { get; set; } = new();
    }

    public class PriceTagSendItem
    {
        public string RetailId { get; set; } = string.Empty;
        public string ItemId { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public string Note { get; set; } = string.Empty;
        public List<string> PriceTagIds { get; set; } = new();
    }

    public class PriceTagSendItemResult
    {
        public string PriceTagId { get; set; } = string.Empty;
        public string ItemId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
