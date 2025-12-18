using System;
using System.Collections.Generic;
using System.Linq;
using PrinterAutomation.Models;
using System.ComponentModel;
using MongoDB.Driver;

namespace PrinterAutomation.Services
{
    public class PrinterService
    {
        private BindingList<Printer> _printers = new BindingList<Printer>();
        private readonly MongoDbService _mongoDbService;
        private readonly IMongoCollection<Printer> _printersCollection;
        private DateTime _lastProgressUpdate = DateTime.MinValue;
        private const int ProgressUpdateIntervalSeconds = 5; // Her 5 saniyede bir MongoDB'ye yaz

        public PrinterService(MongoDbService mongoDbService = null)
        {
            _mongoDbService = mongoDbService;
            if (_mongoDbService != null)
            {
                _printersCollection = _mongoDbService.GetCollection<Printer>("printers");
                LoadPrintersFromDatabase();
                
                // Eğer veritabanında yazıcı yoksa, varsayılan yazıcıları oluştur
                if (_printers.Count == 0)
                {
                    InitializePrinters();
                }
            }
            else
            {
                InitializePrinters();
            }
        }

        private void LoadPrintersFromDatabase()
        {
            try
            {
                var printers = _printersCollection.Find(_ => true).ToList();
                foreach (var printer in printers)
                {
                    // FilamentRemaining ve FilamentType değerlerini MongoDB'den koru
                    // Sadece aktif iş durumlarını sıfırla (uygulama yeniden başlatıldığında)
                    var savedFilamentRemaining = printer.FilamentRemaining;
                    var savedFilamentType = printer.FilamentType;
                    
                    printer.Status = PrinterStatus.Idle;
                    printer.CurrentJobName = null;
                    printer.JobStartTime = null;
                    printer.JobEndTime = null;
                    printer.Progress = 0;
                    
                    // Filament durumlarını geri yükle
                    printer.FilamentRemaining = savedFilamentRemaining;
                    printer.FilamentType = savedFilamentType;
                    
                    _printers.Add(printer);
                    
                    // MongoDB'de güncelle (sadece iş durumlarını, filament değerlerini koru)
                    if (_mongoDbService != null)
                    {
                        try
                        {
                            var filter = Builders<Printer>.Filter.Eq(p => p.Id, printer.Id);
                            var update = Builders<Printer>.Update
                                .Set(p => p.Status, PrinterStatus.Idle)
                                .Set(p => p.CurrentJobName, (string)null)
                                .Set(p => p.JobStartTime, (DateTime?)null)
                                .Set(p => p.JobEndTime, (DateTime?)null)
                                .Set(p => p.Progress, 0);
                            // FilamentRemaining ve FilamentType güncellenmez (korunur)
                            _printersCollection.UpdateOne(filter, update);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MongoDB] Yazıcı durumu güncellenirken hata: {ex.Message}");
                        }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[MongoDB] {printers.Count} yazıcı yüklendi ve Idle durumuna getirildi");
                System.Console.WriteLine($"[MongoDB] {printers.Count} yazıcı yüklendi ve Idle durumuna getirildi");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MongoDB] Yazıcılar yüklenirken hata: {ex.Message}");
                System.Console.WriteLine($"[MongoDB] Yazıcılar yüklenirken hata: {ex.Message}");
            }
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
                var printer = new Printer
                {
                    Id = printerId++,
                    Name = $"{printerModel} #{i + 1}",
                    Status = PrinterStatus.Idle,
                    FilamentRemaining = random.Next(20, 100),
                    FilamentType = filamentTypes[random.Next(filamentTypes.Length)],
                    TotalJobsCompleted = random.Next(0, 50),
                    TotalPrintTime = random.Next(0, 200)
                };
                
                _printers.Add(printer);
                
                // MongoDB'ye kaydet
                if (_mongoDbService != null)
                {
                    SavePrinterToDatabase(printer);
                }
            }
            
                System.Diagnostics.Debug.WriteLine($"[MongoDB] Toplam oluşturulan yazıcı sayısı: {_printers.Count}");
                if (_mongoDbService != null)
                {
                    System.Console.WriteLine($"[MongoDB] {_printers.Count} yazıcı MongoDB'ye kaydedildi");
                }
        }

        public void ClearAndReinitializePrinters()
        {
            _printers.Clear();
            
            // MongoDB'den de temizle
            if (_mongoDbService != null)
            {
                try
                {
                    _printersCollection.DeleteMany(_ => true);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"MongoDB'den yazıcılar silinirken hata: {ex.Message}");
                }
            }
            
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
                
                // MongoDB'de güncelle
                if (_mongoDbService != null && _printersCollection != null)
                {
                    try
                    {
                        var filter = Builders<Printer>.Filter.Eq(p => p.Id, printerId);
                        var update = Builders<Printer>.Update.Set(p => p.Status, status);
                        _printersCollection.UpdateOne(filter, update);
                        System.Diagnostics.Debug.WriteLine($"[MongoDB] Yazıcı durumu güncellendi: {printer.Name} -> {status}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MongoDB] Yazıcı durumu güncellenirken hata: {ex.Message}");
                    }
                }
            }
        }

        public void AssignJobToPrinter(int printerId, string jobName, double estimatedTime, double filamentUsage = 3)
        {
            var printer = GetPrinter(printerId);
            if (printer != null)
            {
                printer.Status = PrinterStatus.Printing;
                printer.CurrentJobName = jobName;
                printer.JobStartTime = DateTime.Now;
                printer.JobEndTime = DateTime.Now.AddMinutes(estimatedTime);
                printer.Progress = 0;
                
                // Filament tüketimi ModelInfo'dan alınır
                printer.FilamentRemaining = Math.Max(0, printer.FilamentRemaining - filamentUsage);
                
                // MongoDB'de güncelle
                if (_mongoDbService != null && _printersCollection != null)
                {
                    try
                    {
                        var filter = Builders<Printer>.Filter.Eq(p => p.Id, printerId);
                        var update = Builders<Printer>.Update
                            .Set(p => p.Status, printer.Status)
                            .Set(p => p.CurrentJobName, printer.CurrentJobName)
                            .Set(p => p.JobStartTime, printer.JobStartTime)
                            .Set(p => p.JobEndTime, printer.JobEndTime)
                            .Set(p => p.Progress, printer.Progress)
                            .Set(p => p.FilamentRemaining, printer.FilamentRemaining);
                        _printersCollection.UpdateOne(filter, update);
                        System.Diagnostics.Debug.WriteLine($"[MongoDB] Yazıcı iş ataması güncellendi: {printer.Name}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MongoDB] Yazıcı iş ataması güncellenirken hata: {ex.Message}");
                    }
                }
            }
        }

        public void UpdateJobProgress(int printerId, double progress)
        {
            var printer = GetPrinter(printerId);
            if (printer != null)
            {
                printer.Progress = progress;
                
                // MongoDB'de güncelle (performans için belirli aralıklarla)
                if (_mongoDbService != null && _printersCollection != null && (DateTime.Now - _lastProgressUpdate).TotalSeconds >= ProgressUpdateIntervalSeconds)
                {
                    try
                    {
                        var filter = Builders<Printer>.Filter.Eq(p => p.Id, printerId);
                        var update = Builders<Printer>.Update.Set(p => p.Progress, progress);
                        _printersCollection.UpdateOne(filter, update);
                        _lastProgressUpdate = DateTime.Now;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MongoDB] Yazıcı ilerlemesi güncellenirken hata: {ex.Message}");
                    }
                }
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
                
                // MongoDB'de güncelle
                if (_mongoDbService != null && _printersCollection != null)
                {
                    try
                    {
                        var filter = Builders<Printer>.Filter.Eq(p => p.Id, printerId);
                        var update = Builders<Printer>.Update
                            .Set(p => p.Status, printer.Status)
                            .Set(p => p.CurrentJobName, printer.CurrentJobName)
                            .Set(p => p.JobStartTime, printer.JobStartTime)
                            .Set(p => p.JobEndTime, printer.JobEndTime)
                            .Set(p => p.Progress, printer.Progress)
                            .Set(p => p.TotalJobsCompleted, printer.TotalJobsCompleted)
                            .Set(p => p.TotalPrintTime, printer.TotalPrintTime);
                        _printersCollection.UpdateOne(filter, update);
                        System.Diagnostics.Debug.WriteLine($"[MongoDB] Yazıcı iş tamamlama güncellendi: {printer.Name}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MongoDB] Yazıcı iş tamamlama güncellenirken hata: {ex.Message}");
                    }
                }
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
                // Yazıcı yazdırma yapıyorsa veya ilerleme varsa filament değiştirilemez
                if (printer.Status == PrinterStatus.Printing || printer.Progress > 0)
                {
                    return false;
                }
                printer.FilamentType = newFilamentType;
                
                // MongoDB'de güncelle
                if (_mongoDbService != null && _printersCollection != null)
                {
                    try
                    {
                        var filter = Builders<Printer>.Filter.Eq(p => p.Id, printerId);
                        var update = Builders<Printer>.Update.Set(p => p.FilamentType, newFilamentType);
                        _printersCollection.UpdateOne(filter, update);
                        System.Diagnostics.Debug.WriteLine($"[MongoDB] Yazıcı filament tipi güncellendi: {printer.Name} -> {newFilamentType}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MongoDB] Filament tipi güncellenirken hata: {ex.Message}");
                    }
                }
                
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
            
            // MongoDB'ye kaydet
            if (_mongoDbService != null && _printersCollection != null)
            {
                try
                {
                    _printersCollection.InsertOne(newPrinter);
                    System.Diagnostics.Debug.WriteLine($"[MongoDB] Yeni yazıcı kaydedildi: {newPrinter.Name} (ID: {newPrinter.Id})");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MongoDB] Yeni yazıcı kaydedilirken hata: {ex.Message}");
                }
            }
            
            return newPrinter;
        }
        
        private void SavePrinterToDatabase(Printer printer)
        {
            if (_mongoDbService != null && _printersCollection != null)
            {
                try
                {
                    var filter = Builders<Printer>.Filter.Eq(p => p.Id, printer.Id);
                    var existing = _printersCollection.Find(filter).FirstOrDefault();
                    
                    if (existing == null)
                    {
                        _printersCollection.InsertOne(printer);
                        System.Diagnostics.Debug.WriteLine($"[MongoDB] Yazıcı kaydedildi: {printer.Name} (ID: {printer.Id})");
                    }
                    else
                    {
                        _printersCollection.ReplaceOne(filter, printer);
                        System.Diagnostics.Debug.WriteLine($"[MongoDB] Yazıcı güncellendi: {printer.Name} (ID: {printer.Id})");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MongoDB] Yazıcı kaydedilirken hata: {ex.Message}");
                    System.Console.WriteLine($"[MongoDB] Yazıcı kaydedilirken hata: {ex.Message}");
                }
            }
        }
    }
}
