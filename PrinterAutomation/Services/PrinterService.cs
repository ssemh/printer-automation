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
            const string printerModel = "Creality Ender 3";
            int printerId = 1;
            
            // Her zaman 10 tane Creality Ender 3 yazıcı oluştur
            const int printerCount = 10;
            
            for (int i = 0; i < printerCount; i++)
            {
                _printers.Add(new Printer
                {
                    Id = printerId++,
                    Name = $"{printerModel} #{i + 1}",
                    Status = PrinterStatus.Idle,
                    FilamentRemaining = random.Next(20, 100),
                    FilamentType = filamentTypes[random.Next(filamentTypes.Length)],
                    TotalJobsCompleted = random.Next(0, 50),
                    TotalPrintTime = random.Next(0, 200)
                });
            }
            
            System.Diagnostics.Debug.WriteLine($"Toplam oluşturulan yazıcı sayısı: {_printers.Count}");
        }

        public void ClearAndReinitializePrinters()
        {
            _printers.Clear();
            InitializePrinters();
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
                // Yazdırma süresini hesapla ve ekle (null yapmadan önce)
                if (printer.JobStartTime.HasValue && printer.JobEndTime.HasValue)
                {
                    var duration = (printer.JobEndTime.Value - printer.JobStartTime.Value).TotalHours;
                    printer.TotalPrintTime += duration;
                }
                
                printer.Status = PrinterStatus.Idle;
                printer.CurrentJobName = null;
                printer.JobStartTime = null;
                printer.JobEndTime = null;
                printer.Progress = 0;
                printer.TotalJobsCompleted++;
            }
        }

        public static List<string> GetAvailablePrinterModels()
        {
            // Yazıcı modelleri listesini döndür (3D modeller değil)
            return new List<string>
            {
                "Creality Ender 3",
                "Prusa i3 MK3S+",
                "Anycubic Kobra",
                "Bambu Lab X1 Carbon",
                "Ultimaker S5",
                "Artillery Sidewinder X2",
                "Elegoo Neptune 3",
                "Sovol SV06",
                "FlashForge Adventurer 4",
                "Qidi Tech X-Max 3"
            };
        }

        public static List<string> GetAvailableFilamentTypes()
        {
            return new List<string> { "PLA", "ABS", "PETG", "TPU", "NYLON", "WOOD", "METAL", "CARBON" };
        }

        public bool ChangeFilamentType(int printerId, string newFilamentType)
        {
            var printer = GetPrinter(printerId);
            if (printer != null)
            {
                // Yazıcı yazdırma yapıyorsa filament değiştirilemez
                if (printer.Status == PrinterStatus.Printing)
                {
                    return false;
                }
                printer.FilamentType = newFilamentType;
                return true;
            }
            return false;
        }

        public Printer AddNewPrinter(string printerModelName, string filamentType = null)
        {
            var random = new Random();
            var availableModels = GetAvailablePrinterModels();
            
            // Yeni ID oluştur
            int newId = _printers.Count > 0 ? _printers.Max(p => p.Id) + 1 : 1;
            
            // Yazıcı modeli adını belirle
            // Eğer printerModelName boşsa veya yazıcı modeli listesinde yoksa, ilk modeli kullan
            string printerModel;
            if (string.IsNullOrEmpty(printerModelName) || !availableModels.Contains(printerModelName))
            {
                // İlk yazıcı modelini varsayılan olarak kullan
                printerModel = availableModels.FirstOrDefault() ?? "Creality Ender 3";
            }
            else
            {
                // printerModelName bir yazıcı modeli adı, onu kullan
                printerModel = printerModelName;
            }
            
            // Aynı modelden kaç tane var say
            int sameModelCount = _printers.Count(p => p.Name.StartsWith(printerModel));
            
            // Filament tipini belirle
            string selectedFilamentType;
            if (!string.IsNullOrEmpty(filamentType) && GetAvailableFilamentTypes().Contains(filamentType))
            {
                selectedFilamentType = filamentType;
            }
            else
            {
                // Varsayılan filament tipi
                var defaultFilamentTypes = GetAvailableFilamentTypes();
                selectedFilamentType = defaultFilamentTypes.FirstOrDefault() ?? "PLA";
            }
            
            var newPrinter = new Printer
            {
                Id = newId,
                Name = $"{printerModel} #{sameModelCount + 1}",
                Status = PrinterStatus.Idle,
                FilamentRemaining = random.Next(20, 100),
                FilamentType = selectedFilamentType,
                TotalJobsCompleted = 0,
                TotalPrintTime = 0
            };
            
            _printers.Add(newPrinter);
            return newPrinter;
        }
    }
}
