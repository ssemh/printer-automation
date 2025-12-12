using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PrinterAutomation.Models
{
    public enum JobStatus
    {
        Queued,      // Kuyrukta
        Printing,    // Yazdırılıyor
        Completed,   // Tamamlandı
        Failed,      // Başarısız
        Cancelled    // İptal edildi
    }

    public class PrintJob
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? MongoId { get; set; }
        
        [BsonElement("id")]
        public int Id { get; set; }
        
        [BsonElement("orderId")]
        public int OrderId { get; set; }
        
        [BsonElement("orderItemId")]
        public int OrderItemId { get; set; }
        
        [BsonElement("printerId")]
        public int PrinterId { get; set; }
        
        [BsonElement("modelFileName")]
        public string ModelFileName { get; set; } = string.Empty;
        
        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public JobStatus Status { get; set; } = JobStatus.Queued;
        
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        [BsonElement("startedAt")]
        public DateTime? StartedAt { get; set; }
        
        [BsonElement("completedAt")]
        public DateTime? CompletedAt { get; set; }
        
        [BsonElement("progress")]
        public double Progress { get; set; }
        
        [BsonElement("material")]
        public string Material { get; set; } = "PLA";
        
        [BsonElement("estimatedTime")]
        public double EstimatedTime { get; set; } // Dakika
    }
}


