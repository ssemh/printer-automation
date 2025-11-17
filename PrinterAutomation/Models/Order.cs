using System;
using System.Collections.Generic;

namespace PrinterAutomation.Models
{
    public enum OrderStatus
    {
        Pending,      // Beklemede
        Processing,  // İşleniyor
        Completed,   // Tamamlandı
        Cancelled    // İptal edildi
    }

    public class Order
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
        public string CustomerName { get; set; } = string.Empty;
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public string ModelFileName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Material { get; set; } = "PLA"; // PLA, ABS, PETG vb.
        public double EstimatedTime { get; set; } // Dakika cinsinden
    }
}


