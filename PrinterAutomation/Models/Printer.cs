using System;

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
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public PrinterStatus Status { get; set; } = PrinterStatus.Idle;
        public string? CurrentJobName { get; set; }
        public DateTime? JobStartTime { get; set; }
        public DateTime? JobEndTime { get; set; }
        public double Progress { get; set; } // 0-100 arası
        public bool IsAvailable => Status == PrinterStatus.Idle;
    }
}


