using System;

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
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int OrderItemId { get; set; }
        public int PrinterId { get; set; }
        public string ModelFileName { get; set; } = string.Empty;
        public JobStatus Status { get; set; } = JobStatus.Queued;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public double Progress { get; set; }
        public string Material { get; set; } = "PLA";
        public double EstimatedTime { get; set; } // Dakika
    }
}


