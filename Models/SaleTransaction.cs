using System;
using System.Collections.Generic;
using System.Linq;

namespace RetailInventorySales.Models
{
    public class SaleTransaction
    {
        public int Id { get; set; }

        /// <summary>
        /// Human friendly receipt number, e.g. INV-000001.
        /// </summary>
        public string TransactionNumber { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public List<SaleLineItem> Items { get; set; } = new();

        public decimal DiscountPercent { get; set; }

        public decimal TaxPercent { get; set; }

        public string PaymentMethod { get; set; } = "Cash";

        public int ItemCount => Items.Sum(i => i.Quantity);

        public decimal Subtotal => Items.Sum(i => i.LineTotal);

        public decimal DiscountAmount => Math.Round(Subtotal * (DiscountPercent / 100m), 2);

        public decimal TaxableAmount => Subtotal - DiscountAmount;

        public decimal TaxAmount => Math.Round(TaxableAmount * (TaxPercent / 100m), 2);

        public decimal Total => TaxableAmount + TaxAmount;
    }
}