namespace SmartSoftware.DeviceAgent.Interfaces
{
    public interface IBarcodeScaleItem
    {
        long BarcodeScalePkId { get; set; }

        long ItemPkId { get; set; }

        long RetailPkId { get; set; }

        string? PLU { get; set; }

        string? Barcode { get; set; }

        decimal LastSentPrice { get; set; }

        decimal NewPrice { get; set; }

        string? ItemName { get; set; }

        string? ItemId { get; set; }

        string? PrintName { get; set; }

        string? IsFraction { get; set; }

        string? IsChanged { get; set; }

        string? HotKey { get; set; }
        string? BarCodeNote { get; set; }

        int IngredientNo { get; set; }

        int? ValidDay { get; set; }

        int Index { get; set; }
    }
}
