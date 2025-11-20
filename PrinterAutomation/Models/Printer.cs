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
        public double FilamentRemaining { get; set; } = 100.0; // % olarak kalan filament
        public string FilamentType { get; set; } = "PLA"; // Filament tipi
        public int TotalJobsCompleted { get; set; } = 0; // Tamamlanan iş sayısı
        public double TotalPrintTime { get; set; } = 0; // Toplam yazdırma süresi (saat)
        public bool IsAvailable => Status == PrinterStatus.Idle;
    }
}


