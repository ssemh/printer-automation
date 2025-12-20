using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

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
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? MongoId { get; set; }
        
        [BsonElement("id")]
        public int Id { get; set; }
        
        [BsonElement("orderNumber")]
        public string OrderNumber { get; set; } = string.Empty;
        
        [BsonElement("orderDate")]
        public DateTime OrderDate { get; set; } = DateTime.Now;
        
        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        
        [BsonElement("items")]
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
        
        [BsonElement("customerName")]
        public string CustomerName { get; set; } = string.Empty;
        
        [BsonElement("totalPrice")]
        public decimal TotalPrice { get; set; } = 0; // Toplam fiyat (TL)
    }

    public class OrderItem
    {
        [BsonElement("id")]
        public int Id { get; set; }
        
        [BsonElement("modelFileName")]
        public string ModelFileName { get; set; } = string.Empty;
        
        [BsonElement("quantity")]
        public int Quantity { get; set; }
        
        [BsonElement("material")]
        public string Material { get; set; } = "PLA"; // PLA, ABS, PETG vb.
        
        [BsonElement("estimatedTime")]
        public double EstimatedTime { get; set; } // Dakika cinsinden
    }
}


