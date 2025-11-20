using System;
using System.Collections.Generic;
using System.Linq;
using PrinterAutomation.Models;
using System.ComponentModel;

namespace PrinterAutomation.Services
{
    public class PrinterService
    {
        private BindingList<Printer> _printers = new BindingList<Printer>();

        public PrinterService()
        {
            InitializePrinters();
        }

        private void InitializePrinters()
        {
            var random = new Random();
            var filamentTypes = new[] { "PLA", "ABS", "PETG", "TPU" };
            
            // 10 adet 3D yazıcı oluştur
            for (int i = 1; i <= 10; i++)
            {
                _printers.Add(new Printer
                {
                    Id = i,
                    Name = $"3D Yazıcı {i}",
                    Status = PrinterStatus.Idle,
                    FilamentRemaining = random.Next(20, 100), // %20-100 arası rastgele
                    FilamentType = filamentTypes[random.Next(filamentTypes.Length)],
                    TotalJobsCompleted = random.Next(0, 50),
                    TotalPrintTime = random.Next(0, 200)
                });
            }
        }

        public BindingList<Printer> GetAllPrinters() => _printers;

        public Printer? GetPrinter(int id) => _printers.FirstOrDefault(p => p.Id == id);

        public List<Printer> GetAvailablePrinters() => 
            _printers.Where(p => p.IsAvailable).ToList();

        public void UpdatePrinterStatus(int printerId, PrinterStatus status)
        {
            var printer = GetPrinter(printerId);
            if (printer != null)
            {
                printer.Status = status;
            }
        }

        public void AssignJobToPrinter(int printerId, string jobName, double estimatedTime)
        {
            var printer = GetPrinter(printerId);
            if (printer != null)
            {
                printer.Status = PrinterStatus.Printing;
                printer.CurrentJobName = jobName;
                printer.JobStartTime = DateTime.Now;
                printer.JobEndTime = DateTime.Now.AddMinutes(estimatedTime);
                printer.Progress = 0;
                
                // Filament tüketimi simülasyonu (iş başladığında %1-3 arası azalır)
                var random = new Random();
                var filamentUsage = random.Next(1, 4);
                printer.FilamentRemaining = Math.Max(0, printer.FilamentRemaining - filamentUsage);
            }
        }

        public void UpdateJobProgress(int printerId, double progress)
        {
            var printer = GetPrinter(printerId);
            if (printer != null)
            {
                printer.Progress = progress;
            }
        }

        public void CompleteJob(int printerId)
        {
            var printer = GetPrinter(printerId);
            if (printer != null)
            {
                printer.Status = PrinterStatus.Idle;
                printer.CurrentJobName = null;
                printer.JobStartTime = null;
                printer.JobEndTime = null;
                printer.Progress = 0;
                printer.TotalJobsCompleted++;
                
                // Yazdırma süresini hesapla ve ekle
                if (printer.JobStartTime.HasValue && printer.JobEndTime.HasValue)
                {
                    var duration = (printer.JobEndTime.Value - printer.JobStartTime.Value).TotalHours;
                    printer.TotalPrintTime += duration;
                }
            }
        }
    }
}


