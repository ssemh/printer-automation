using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PrinterAutomation.Models
{
    public enum PrinterStatus
    {
        Idle,        // Boşta
        Printing,    // Yazdırıyor
        Paused,      // Duraklatıldı
        Error,       // Hata
        Maintenance  // Bakımda
    }

    public class Printer
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? MongoId { get; set; }
        
        [BsonElement("id")]
        public int Id { get; set; }
        
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;
        
        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public PrinterStatus Status { get; set; } = PrinterStatus.Idle;
        
        [BsonElement("currentJobName")]
        public string? CurrentJobName { get; set; }
        
        [BsonElement("jobStartTime")]
        public DateTime? JobStartTime { get; set; }
        
        [BsonElement("jobEndTime")]
        public DateTime? JobEndTime { get; set; }
        
        [BsonElement("progress")]
        public double Progress { get; set; } // 0-100 arası
        
        [BsonElement("filamentRemaining")]
        public double FilamentRemaining { get; set; } = 100.0; // % olarak kalan filament
        
        [BsonElement("filamentType")]
        public string FilamentType { get; set; } = "PLA"; // Filament tipi
        
        [BsonElement("totalJobsCompleted")]
        public int TotalJobsCompleted { get; set; } = 0; // Tamamlanan iş sayısı
        
        [BsonElement("totalPrintTime")]
        public double TotalPrintTime { get; set; } = 0; // Toplam yazdırma süresi (saat)
        
        [BsonIgnore]
        public bool IsAvailable => Status == PrinterStatus.Idle;
    }
}


