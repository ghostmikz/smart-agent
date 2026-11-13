using SmartSoftware.DeviceAgent.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartSoftware.DeviceAgent.Model
{
    public class BarcodeScaleItemModel : IBarcodeScaleItem
    {
        public long BarcodeScalePkId { get; set; }

        public long ItemPkId { get; set; }

        public long RetailPkId { get; set; }

        public string? PLU { get; set; }

        public string? Barcode { get; set; }

        public decimal LastSentPrice { get; set; }

        public decimal NewPrice { get; set; }

        public string? ItemName { get; set; }

        public string? ItemId { get; set; }

        public string? PrintName { get; set; }

        public string? IsFraction { get; set; }

        public string? IsChanged { get; set; }

        public string? HotKey { get; set; }

        public string? BarCodeNote { get; set; }

        public int IngredientNo { get; set; }

        public int? ValidDay { get; set; }

        [NotMapped]
        public int Index { get; set; }
    }
}
