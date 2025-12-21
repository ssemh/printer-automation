using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PrinterAutomation.Models
{
    public class ModelInfo
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? MongoId { get; set; }
        
        [BsonElement("modelFileName")]
        public string ModelFileName { get; set; } = string.Empty;
        
        [BsonElement("estimatedTime")]
        public double EstimatedTime { get; set; } // Dakika cinsinden
        
        [BsonElement("filamentUsage")]
        public double FilamentUsage { get; set; } // % olarak filament kullanımı
    }
}




