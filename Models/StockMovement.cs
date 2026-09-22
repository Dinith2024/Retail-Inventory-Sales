using System;

namespace RetailInventorySales.Models
{
    public enum StockMovementType
    {
        InitialStock,
        Restock,
        Sale,
        ManualAdjustment,
        Return
    }

    /// <summary>
    /// An audit record of every change made to a product's stock level.
    /// Positive QuantityChange increases stock, negative decreases it.
    /// </summary>
    public class StockMovement
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string SKU { get; set; } = string.Empty;

        public StockMovementType MovementType { get; set; }

        public int QuantityChange { get; set; }

        public int ResultingStock { get; set; }

        public string Notes { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}