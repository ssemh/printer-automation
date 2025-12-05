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
            
            // Gerçek yazıcı markaları listesi
            var printerBrands = new[] 
            { 
                "Ender 3", 
                "Prusa i3 MK3S+", 
                "Ultimaker 3", 
                "Creality CR-10", 
                "Anycubic i3 Mega", 
                "FlashForge Creator Pro", 
                "Monoprice Maker Select", 
                "Qidi Tech X-One", 
                "Artillery Sidewinder", 
                "Voxelab Aquila" 
            };
            
            // 10 adet 3D yazıcı oluştur - hepsi aynı marka (Ender 3)
            for (int i = 1; i <= 10; i++)
            {
                _printers.Add(new Printer
                {
                    Id = i,
                    Name = $"Ender 3 #{i}",
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

        public Printer AddNewPrinter(string printerModel)
        {
            var random = new Random();
            var filamentTypes = new[] { "PLA", "ABS", "PETG", "TPU" };
            
            // Yeni ID'yi belirle
            int newId = _printers.Count > 0 ? _printers.Max(p => p.Id) + 1 : 1;
            
            // Aynı modelden kaç tane var say
            int modelCount = _printers.Count(p => p.Name.StartsWith(printerModel));
            
            var newPrinter = new Printer
            {
                Id = newId,
                Name = modelCount > 0 ? $"{printerModel} #{modelCount + 1}" : $"{printerModel} #1",
                Status = PrinterStatus.Idle,
                FilamentRemaining = random.Next(20, 100),
                FilamentType = filamentTypes[random.Next(filamentTypes.Length)],
                TotalJobsCompleted = 0,
                TotalPrintTime = 0
            };
            
            _printers.Add(newPrinter);
            return newPrinter;
        }

        public static string[] GetAvailablePrinterModels()
        {
            return new[]
            {
                "Ender 3",
                "Ender 3 Pro",
                "Ender 3 V2",
                "Ender 3 S1",
                "Ender 5",
                "Ender 5 Plus",
                "Ender 6",
                "Prusa i3 MK3S+",
                "Prusa i3 MK3S",
                "Prusa MINI+",
                "Prusa XL",
                "Ultimaker 3",
                "Ultimaker 3 Extended",
                "Ultimaker S3",
                "Ultimaker S5",
                "Creality CR-10",
                "Creality CR-10 V3",
                "Creality CR-10S",
                "Creality CR-10S Pro",
                "Creality CR-6 SE",
                "Creality CR-20 Pro",
                "Anycubic i3 Mega",
                "Anycubic i3 Mega S",
                "Anycubic Kobra",
                "Anycubic Kobra Max",
                "Anycubic Vyper",
                "Anycubic Photon",
                "FlashForge Creator Pro",
                "FlashForge Creator Pro 2",
                "FlashForge Adventurer 3",
                "FlashForge Guider 2",
                "Monoprice Maker Select",
                "Monoprice Maker Select Plus",
                "Monoprice Voxel",
                "Qidi Tech X-One",
                "Qidi Tech X-Plus",
                "Qidi Tech X-Max",
                "Qidi Tech X-Pro",
                "Artillery Sidewinder X1",
                "Artillery Sidewinder X2",
                "Artillery Genius",
                "Artillery Hornet",
                "Voxelab Aquila",
                "Voxelab Aquila X2",
                "Voxelab Aquila S2",
                "Bambu Lab X1 Carbon",
                "Bambu Lab P1P",
                "Bambu Lab P1S",
                "Elegoo Neptune 3",
                "Elegoo Neptune 3 Pro",
                "Elegoo Neptune 4",
                "Sovol SV01",
                "Sovol SV04",
                "Sovol SV06",
                "Kingroon KP3S",
                "Kingroon KP3S Pro",
                "Flsun Q5",
                "Flsun Super Racer",
                "Flsun V400",
                "Ratrig V-Core 3",
                "Ratrig V-Minion",
                "Voron 2.4",
                "Voron Trident",
                "Voron Switchwire",
                "RatOS",
                "HevORT",
                "BLV Cube",
                "Hypercube Evolution",
                "Tevo Tarantula",
                "Tevo Tornado",
                "Anet A8",
                "Anet ET4",
                "Geeetech A10",
                "Geeetech A20",
                "Geeetech A30",
                "Wanhao Duplicator i3",
                "Wanhao Duplicator 6",
                "Tronxy X5SA",
                "Tronxy XY-2 Pro",
                "Ender 3 Max",
                "Ender 3 S1 Pro",
                "Ender 3 S1 Plus",
                "Ender 7",
                "Kossel",
                "Delta 3D Printer",
                "Rostock Max",
                "SeeMeCNC Orion",
                "SeeMeCNC Rostock MAX V3"
            };
        }
    }
}


