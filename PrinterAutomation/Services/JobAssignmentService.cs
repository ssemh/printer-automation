using System;
using System.Collections.Generic;
using System.Linq;
using PrinterAutomation.Models;
using System.ComponentModel;
using MongoDB.Driver;

namespace PrinterAutomation.Services
{
    public class JobAssignmentService
    {
        private readonly PrinterService _printerService;
        private readonly OrderService _orderService;
        private readonly MongoDbService _mongoDbService;
        private readonly IMongoCollection<PrintJob> _jobsCollection;
        private BindingList<PrintJob> _printJobs = new BindingList<PrintJob>();
        private int _nextJobId = 1;
        private System.Windows.Forms.Timer _progressTimer;

        public event System.EventHandler<PrintJobEventArgs> JobAssigned;
        public event System.EventHandler<PrintJobEventArgs> JobCompleted;
        public event System.EventHandler<FilamentDepletedEventArgs> FilamentDepleted;
        public event System.EventHandler PrintersUpdated;

        public JobAssignmentService(PrinterService printerService, OrderService orderService, MongoDbService mongoDbService = null)
        {
            _printerService = printerService;
            _orderService = orderService;
            _mongoDbService = mongoDbService;
            
            if (_mongoDbService != null)
            {
                _jobsCollection = _mongoDbService.GetCollection<PrintJob>("printJobs");
                LoadJobsFromDatabase();
                // LoadJobsFromDatabase tamamlandıktan sonra timer'ı başlat
                // Bu sayede yazıcılar önce güncellenir, sonra timer kontrol eder
                InitializeProgressTimer();
            }
            else
            {
                // MongoDB yoksa timer'ı hemen başlat
                InitializeProgressTimer();
            }
        }

        private void LoadJobsFromDatabase()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[JobAssignment] LoadJobsFromDatabase() başladı");
                System.Console.WriteLine($"[JobAssignment] LoadJobsFromDatabase() başladı");
                System.Console.WriteLine($"[JobAssignment] MongoDB servisi: {(_mongoDbService != null ? "MEVCUT" : "NULL")}");
                System.Console.WriteLine($"[JobAssignment] Jobs collection: {(_jobsCollection != null ? "MEVCUT" : "NULL")}");
                
                if (_jobsCollection == null)
                {
                    System.Console.WriteLine($"[JobAssignment] ⚠ Jobs collection NULL! İşler yüklenemiyor.");
                    return;
                }
                
                var jobs = _jobsCollection.Find(_ => true).ToList();
                System.Console.WriteLine($"[JobAssignment] MongoDB'den {jobs.Count} iş bulundu");
                int printingJobsCount = 0;
                int completedJobsCount = 0;
                int queuedJobsCount = 0;
                
                // ÖNEMLİ: Yazıcılarda iş bilgisi varsa ama job bulunamazsa, yazıcıları koru
                // Önce tüm yazıcıları kontrol et
                var allPrinters = _printerService.GetAllPrinters();
                foreach (var printer in allPrinters)
                {
                    // Eğer yazıcı Printing durumunda ve iş bilgisi varsa
                    if (printer.Status == PrinterStatus.Printing && !string.IsNullOrEmpty(printer.CurrentJobName))
                    {
                        // Bu yazıcı için Printing durumunda bir job var mı kontrol et
                        var printerJob = jobs.FirstOrDefault(j => j.PrinterId == printer.Id && j.Status == JobStatus.Printing);
                        if (printerJob == null)
                        {
                            // Job bulunamadı ama yazıcıda iş bilgisi var
                            // Bu durumda yazıcıyı koru - job tamamlanmış olabilir veya daha sonra yüklenecek
                            System.Diagnostics.Debug.WriteLine($"[JobAssignment] ⚠ Yazıcı #{printer.Id} Printing durumunda ve iş bilgisi var (Job: {printer.CurrentJobName}) ama job bulunamadı. Yazıcı korunuyor.");
                            System.Console.WriteLine($"[JobAssignment] ⚠ Yazıcı #{printer.Id} Printing durumunda ve iş bilgisi var (Job: {printer.CurrentJobName}) ama job bulunamadı. Yazıcı korunuyor.");
                            // Yazıcıyı koru - hiçbir şey yapma
                        }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[JobAssignment] Toplam {jobs.Count} iş bulundu, durumları kontrol ediliyor...");
                System.Console.WriteLine($"[JobAssignment] Toplam {jobs.Count} iş bulundu, durumları kontrol ediliyor...");
                
                foreach (var job in jobs)
                {
                    System.Diagnostics.Debug.WriteLine($"[JobAssignment] İş #{job.Id} - Durum: {job.Status}, Progress: {job.Progress}%, PrinterId: {job.PrinterId}");
                    System.Console.WriteLine($"[JobAssignment] İş #{job.Id} - Durum: {job.Status}, Progress: {job.Progress}%, PrinterId: {job.PrinterId}");
                    
                    // Devam eden işleri (Printing) koru - uygulama yeniden başlatıldığında
                    // StartedAt ve EstimatedEndTime zamanlarına göre ilerlemeyi hesapla ve yazıcıya geri ata
                    if (job.Status == JobStatus.Printing)
                    {
                        // Eğer StartedAt veya EstimatedEndTime yoksa, bu eski/tutarsız bir veri
                        // Bu durumda işi Queued yap (tutarlılık için)
                        if (!job.StartedAt.HasValue || !job.EstimatedEndTime.HasValue)
                        {
                            System.Diagnostics.Debug.WriteLine($"[JobAssignment] ⚠ İş #{job.Id} Printing durumunda ama StartedAt veya EstimatedEndTime yok (eski veri), Queued yapılıyor");
                            System.Console.WriteLine($"[JobAssignment] ⚠ İş #{job.Id} Printing durumunda ama StartedAt veya EstimatedEndTime yok (eski veri), Queued yapılıyor");
                            
                            job.Status = JobStatus.Queued;
                            job.PrinterId = 0;
                            job.Progress = 0;
                            job.StartedAt = null;
                            job.EstimatedEndTime = null;
                            
                            // MongoDB'de güncelle
                            if (_mongoDbService != null)
                            {
                                try
                                {
                                    var filter = Builders<PrintJob>.Filter.Eq(j => j.Id, job.Id);
                                    var update = Builders<PrintJob>.Update
                                        .Set(j => j.Status, JobStatus.Queued)
                                        .Set(j => j.PrinterId, 0)
                                        .Set(j => j.Progress, 0)
                                        .Set(j => j.StartedAt, (DateTime?)null)
                                        .Set(j => j.EstimatedEndTime, (DateTime?)null);
                                    _jobsCollection.UpdateOne(filter, update);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[JobAssignment] Eski iş düzeltilirken hata: {ex.Message}");
                                }
                            }
                            
                            queuedJobsCount++;
                            continue; // Bu işi atla, bir sonrakine geç
                        }
                        
                        // Basit zaman bazlı progress hesaplama
                        double calculatedProgress = 0;
                        bool shouldComplete = false;
                        
                        if (job.StartedAt.HasValue && job.EstimatedEndTime.HasValue)
                        {
                            var now = DateTime.Now;
                            var elapsed = now - job.StartedAt.Value;
                            var total = job.EstimatedEndTime.Value - job.StartedAt.Value;
                            
                            if (total.TotalSeconds > 0)
                            {
                                // Progress = (Geçen süre / Toplam süre) * 100
                                calculatedProgress = Math.Min(100, Math.Max(0, (elapsed.TotalSeconds / total.TotalSeconds) * 100));
                                
                                // Eğer EstimatedEndTime geçmişse progress %100 yap
                                if (now >= job.EstimatedEndTime.Value)
                                {
                                    calculatedProgress = 100;
                                    shouldComplete = true;
                                }
                            }
                            else
                            {
                                // Toplam süre 0 veya negatifse, mevcut progress'i kullan
                                calculatedProgress = job.Progress;
                                if (calculatedProgress >= 100)
                                {
                                    shouldComplete = true;
                                }
                            }
                            
                            var remainingSeconds = Math.Max(0, (job.EstimatedEndTime.Value - now).TotalSeconds);
                            System.Diagnostics.Debug.WriteLine($"[JobAssignment] İş #{job.Id} devam ediyor (Progress: {calculatedProgress:F1}%, Kalan süre: {remainingSeconds:F0} saniye)");
                        }
                        else
                        {
                            // StartedAt veya EstimatedEndTime yoksa, mevcut progress'i kullan
                            calculatedProgress = job.Progress;
                            if (calculatedProgress >= 100)
                            {
                                shouldComplete = true;
                            }
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[MongoDB] Printing iş bulundu: Job #{job.Id}, Hesaplanan Progress: {calculatedProgress:F1}% (StartedAt: {job.StartedAt}, EstimatedEndTime: {job.EstimatedEndTime}, PrinterId: {job.PrinterId})");
                        
                        // Eğer tamamlanmışsa direkt CompleteJob çağır
                        if (shouldComplete)
                        {
                            job.Progress = 100;
                            job.Status = JobStatus.Completed;
                            job.CompletedAt = job.EstimatedEndTime ?? DateTime.Now;
                            
                            // MongoDB'de güncelle
                            if (_mongoDbService != null)
                            {
                                try
                                {
                                    var filter = Builders<PrintJob>.Filter.Eq(j => j.Id, job.Id);
                                    var update = Builders<PrintJob>.Update
                                        .Set(j => j.Status, JobStatus.Completed)
                                        .Set(j => j.Progress, 100)
                                        .Set(j => j.CompletedAt, job.CompletedAt);
                                    _jobsCollection.UpdateOne(filter, update);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[MongoDB] İş tamamlanırken hata: {ex.Message}");
                                }
                            }
                            
                            // Yazıcıyı Idle yap
                            if (job.PrinterId > 0)
                            {
                                var printer = _printerService.GetPrinter(job.PrinterId);
                                if (printer != null)
                                {
                                    _printerService.CompleteJob(job.PrinterId);
                                }
                            }
                            
                            System.Diagnostics.Debug.WriteLine($"[MongoDB] ✓ İş tamamlandı: Job #{job.Id} (Süre dolmuş)");
                            completedJobsCount++;
                            continue; // Bu işi atla, bir sonrakine geç
                        }
                        
                        // İlerlemeyi güncelle (tamamlanmamış işler için)
                        job.Progress = calculatedProgress;
                        job.Status = JobStatus.Printing; // Printing olarak koru
                        // PrinterId korunur, sıfırlanmaz
                        printingJobsCount++;
                        
                        // Yazıcıyı Printing durumuna getir ve zamanları senkronize et
                        if (job.PrinterId > 0)
                        {
                            var printer = _printerService.GetPrinter(job.PrinterId);
                            if (printer != null)
                            {
                                System.Console.WriteLine($"[JobAssignment] Yazıcı #{printer.Id} güncelleniyor: Mevcut Status={printer.Status}, Job={printer.CurrentJobName ?? "(null)"}, Progress={printer.Progress:F1}%");
                                
                                // FilamentUsage'ı job'dan al, yoksa ModelInfo'dan al
                                double filamentUsage = job.FilamentUsage > 0 ? job.FilamentUsage : (_orderService?.GetModelInfo(job.ModelFileName)?.FilamentUsage ?? 3);
                                
                                // Eğer filament kullanımı kaydedilmemişse, job'a kaydet
                                if (job.FilamentUsage <= 0)
                                {
                                    job.FilamentUsage = filamentUsage;
                                }
                                
                                // ÖNEMLİ: Yazıcının mevcut durumunu koru, sadece gerekli alanları güncelle
                                // AssignJobToPrinter çağrılmasın - bu yazıcıyı sıfırlayabilir
                                // Bunun yerine sadece gerekli alanları manuel olarak güncelle
                                // Eğer yazıcıda zaten iş bilgisi varsa ve job ile eşleşiyorsa, mevcut değerleri koru
                                if (!string.IsNullOrEmpty(printer.CurrentJobName) && 
                                    printer.CurrentJobName == job.ModelFileName &&
                                    printer.Status == PrinterStatus.Printing)
                                {
                                    // Yazıcıda zaten doğru iş bilgisi var, sadece progress ve zamanları güncelle
                                    System.Console.WriteLine($"[JobAssignment] Yazıcı #{printer.Id} zaten doğru iş bilgisine sahip, sadece progress güncelleniyor");
                                }
                                
                                printer.Status = PrinterStatus.Printing;
                                printer.CurrentJobName = job.ModelFileName;
                                printer.JobStartTime = job.StartedAt;
                                printer.JobEndTime = job.EstimatedEndTime;
                                printer.Progress = calculatedProgress;
                                
                                // Filament güncellemesi
                                if (!printer.JobStartFilament.HasValue)
                                {
                                    double usedFilament = filamentUsage * (calculatedProgress / 100.0);
                                    printer.JobStartFilament = printer.FilamentRemaining + usedFilament;
                                }
                                
                                if (printer.JobStartFilament.HasValue)
                                {
                                    double usedFilament = filamentUsage * (calculatedProgress / 100.0);
                                    printer.FilamentRemaining = Math.Max(0, printer.JobStartFilament.Value - usedFilament);
                                }
                                
                                System.Console.WriteLine($"[JobAssignment] ✓ Yazıcı #{printer.Id} güncellendi: Status=Printing, Job={printer.CurrentJobName}, Progress={calculatedProgress:F1}%");
                                
                                // Program kapalıyken filament azaltma: İlerlemeye göre filament güncelle
                                // Eğer JobStartFilament yoksa (eski veriler için), mevcut filament'i kullan
                                if (!printer.JobStartFilament.HasValue)
                                {
                                    // Eski veriler için, mevcut filament'i başlangıç olarak kabul et
                                    // Başlangıç filament'ini hesapla: Mevcut filament + kullanılan filament
                                    // Kullanılan filament = FilamentUsage * Progress / 100
                                    double usedFilament = filamentUsage * (calculatedProgress / 100.0);
                                    printer.JobStartFilament = printer.FilamentRemaining + usedFilament;
                                    
                                    System.Diagnostics.Debug.WriteLine($"[JobAssignment] Yazıcı #{printer.Id} için JobStartFilament hesaplandı: {printer.JobStartFilament:F1}% (Mevcut: {printer.FilamentRemaining:F1}%, Kullanılan: {usedFilament:F1}%)");
                                }
                                
                                // İlerlemeye göre filament güncelle
                                if (printer.JobStartFilament.HasValue)
                                {
                                    // Filament = Başlangıç - (Kullanım * İlerleme / 100)
                                    double usedFilament = filamentUsage * (calculatedProgress / 100.0);
                                    printer.FilamentRemaining = Math.Max(0, printer.JobStartFilament.Value - usedFilament);
                                    
                                    System.Diagnostics.Debug.WriteLine($"[JobAssignment] Yazıcı #{printer.Id} filament güncellendi: {printer.FilamentRemaining:F1}% (Başlangıç: {printer.JobStartFilament:F1}%, Kullanılan: {usedFilament:F1}%, İlerleme: {calculatedProgress:F1}%)");
                                }
                                
                                System.Diagnostics.Debug.WriteLine($"[JobAssignment] ✓ Yazıcı #{printer.Id} Printing durumuna getirildi (Job #{job.Id}, Progress: {calculatedProgress:F1}%, JobName: {printer.CurrentJobName})");
                                System.Console.WriteLine($"[JobAssignment] ✓ Yazıcı #{printer.Id} Printing durumuna getirildi (Job #{job.Id}, Progress: {calculatedProgress:F1}%, JobName: {printer.CurrentJobName})");
                                
                                // MongoDB'de yazıcıyı güncelle
                                if (_mongoDbService != null)
                                {
                                    try
                                    {
                                        var printerCollection = _mongoDbService.GetCollection<Printer>("printers");
                                        var filter = Builders<Printer>.Filter.Eq(p => p.Id, printer.Id);
                                        var update = Builders<Printer>.Update
                                            .Set(p => p.Status, printer.Status)
                                            .Set(p => p.CurrentJobName, printer.CurrentJobName)
                                            .Set(p => p.JobStartTime, printer.JobStartTime)
                                            .Set(p => p.JobEndTime, printer.JobEndTime)
                                            .Set(p => p.Progress, printer.Progress)
                                            .Set(p => p.FilamentRemaining, printer.FilamentRemaining)
                                            .Set(p => p.JobStartFilament, printer.JobStartFilament);
                                        var result = printerCollection.UpdateOne(filter, update);
                                        if (result.ModifiedCount > 0)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"[JobAssignment] ✓ MongoDB'de yazıcı #{printer.Id} güncellendi");
                                        }
                                        else
                                        {
                                            System.Diagnostics.Debug.WriteLine($"[JobAssignment] ⚠ MongoDB'de yazıcı #{printer.Id} güncellenemedi (ModifiedCount=0)");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[JobAssignment] ✗ Yazıcı güncellenirken hata: {ex.Message}");
                                    }
                                }
                                
                                // Yazıcının güncel durumunu kontrol et
                                var verifyPrinter = _printerService.GetPrinter(job.PrinterId);
                                if (verifyPrinter != null)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[JobAssignment] Doğrulama: Yazıcı #{verifyPrinter.Id} Status={verifyPrinter.Status}, Job={verifyPrinter.CurrentJobName}, Progress={verifyPrinter.Progress:F1}%");
                                }
                            }
                            else
                            {
                                // Yazıcı bulunamadıysa işi Queued yap
                                System.Diagnostics.Debug.WriteLine($"[JobAssignment] ⚠ Yazıcı #{job.PrinterId} bulunamadı, iş #{job.Id} Queued yapılıyor");
                                job.Status = JobStatus.Queued;
                                job.PrinterId = 0;
                            }
                        }
                        else
                        {
                            // PrinterId yoksa Queued yap
                            System.Diagnostics.Debug.WriteLine($"[MongoDB] ⚠ İş #{job.Id} için PrinterId yok, Queued yapılıyor");
                            job.Status = JobStatus.Queued;
                        }
                        
                        // MongoDB'de güncelle (StartedAt ve EstimatedEndTime korunur, Progress güncellenir)
                        if (_mongoDbService != null)
                        {
                            try
                            {
                                var filter = Builders<PrintJob>.Filter.Eq(j => j.Id, job.Id);
                                var update = Builders<PrintJob>.Update
                                    .Set(j => j.Status, job.Status)
                                    .Set(j => j.Progress, calculatedProgress)
                                    .Set(j => j.EstimatedEndTime, job.EstimatedEndTime);
                                // StartedAt'ı da güncelle (EstimatedEndTime güncellendiğinde)
                                if (job.StartedAt.HasValue)
                                {
                                    update = update.Set(j => j.StartedAt, job.StartedAt);
                                }
                                // PrinterId korunur (güncellenmez)
                                if (job.PrinterId > 0)
                                {
                                    update = update.Set(j => j.PrinterId, job.PrinterId);
                                }
                                var result = _jobsCollection.UpdateOne(filter, update);
                                if (result.ModifiedCount > 0)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[MongoDB] ✓ İş devam ediyor: Job #{job.Id} (Printing, Progress: {calculatedProgress:F1}%, PrinterId: {job.PrinterId})");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"[MongoDB] ⚠ İş güncellenirken MongoDB güncellemesi başarısız: Job #{job.Id}");
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[MongoDB] ✗ İş güncellenirken hata: {ex.Message}");
                            }
                        }
                    }
                    else if (job.Status == JobStatus.Completed)
                    {
                        completedJobsCount++;
                        System.Diagnostics.Debug.WriteLine($"[MongoDB] Completed iş: Job #{job.Id} (zaten tamamlanmış)");
                    }
                    else if (job.Status == JobStatus.Queued)
                    {
                        queuedJobsCount++;
                    }
                    
                    _printJobs.Add(job);
                    if (job.Id >= _nextJobId)
                    {
                        _nextJobId = job.Id + 1;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[JobAssignment] Özet: {jobs.Count} iş yüklendi - {printingJobsCount} Printing (devam ediyor), {queuedJobsCount} Queued, {completedJobsCount} Completed");
                System.Console.WriteLine($"[JobAssignment] {jobs.Count} iş veritabanından yüklendi ({printingJobsCount} devam eden iş korundu)");
                
                // Yazıcıların durumlarını doğrula ve tutarsızlıkları düzelt
                // Yazıcılar veritabanından yüklenirken durumları korunur
                // LoadJobsFromDatabase'de yazıcılar güncellenir (Printing işler varsa)
                // Ancak eğer yazıcıda iş bilgisi varsa ama job bulunamazsa, yazıcıyı koru
                var printersForValidation = _printerService.GetAllPrinters();
                foreach (var printer in printersForValidation)
                {
                    System.Diagnostics.Debug.WriteLine($"[JobAssignment] Doğrulama: Yazıcı #{printer.Id} Status={printer.Status}, Job={printer.CurrentJobName ?? "(null)"}, Progress={printer.Progress:F1}%");
                    System.Console.WriteLine($"[JobAssignment] Doğrulama: Yazıcı #{printer.Id} Status={printer.Status}, Job={printer.CurrentJobName ?? "(null)"}, Progress={printer.Progress:F1}%");
                    
                    // Eğer yazıcı Printing durumunda ve iş bilgisi varsa ama job bulunamazsa,
                    // yazıcıyı koru (job daha sonra yüklenebilir veya tamamlanmış olabilir)
                    if (printer.Status == PrinterStatus.Printing && !string.IsNullOrEmpty(printer.CurrentJobName))
                    {
                        // Bu yazıcı için Printing durumunda bir job var mı kontrol et
                        var printerJob = _printJobs.FirstOrDefault(j => j.PrinterId == printer.Id && j.Status == JobStatus.Printing);
                        if (printerJob == null)
                        {
                            // Job bulunamadı ama yazıcıda iş bilgisi var
                            // Bu durumda yazıcıyı koru - job tamamlanmış olabilir veya daha sonra yüklenecek
                            System.Diagnostics.Debug.WriteLine($"[JobAssignment] ⚠ Yazıcı #{printer.Id} Printing durumunda ve iş bilgisi var ama job bulunamadı. Yazıcı korunuyor.");
                            System.Console.WriteLine($"[JobAssignment] ⚠ Yazıcı #{printer.Id} Printing durumunda ve iş bilgisi var ama job bulunamadı. Yazıcı korunuyor.");
                        }
                    }
                }
                
                if (printingJobsCount == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[JobAssignment] ⚠ Printing durumunda iş bulunamadı! Ancak yazıcılarda iş bilgisi varsa korunacak.");
                    System.Console.WriteLine($"[JobAssignment] ⚠ Printing durumunda iş bulunamadı! Ancak yazıcılarda iş bilgisi varsa korunacak.");
                }
                
                // Yazıcılar güncellendi, MainForm'a bildir (event handler'lar kurulduktan sonra tetiklenmesi için timer kullan)
                // Her zaman tetikle (yazıcı durumları güncellendiği için)
                System.Diagnostics.Debug.WriteLine($"[JobAssignment] Yazıcılar güncellendi, PrintersUpdated event'i timer ile tetiklenecek");
                var eventTimer = new System.Windows.Forms.Timer();
                eventTimer.Interval = 1000; // 1 saniye bekle (event handler'ların kurulması için)
                eventTimer.Tick += (s, e) =>
                {
                    eventTimer.Stop();
                    eventTimer.Dispose();
                    if (PrintersUpdated != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[JobAssignment] PrintersUpdated event tetikleniyor (yazıcılar güncellendi)");
                        PrintersUpdated(this, EventArgs.Empty);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[JobAssignment] ⚠ PrintersUpdated event handler'ı henüz kurulmamış");
                    }
                };
                eventTimer.Start();
                
                // Sadece kuyruktaki işleri işle (Printing işler zaten devam ediyor)
                if (queuedJobsCount > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[MongoDB] {queuedJobsCount} kuyruktaki iş işlenecek (Timer ile)");
                    // Timer ile çağır - yazıcıların tam yüklenmesi için bekle
                    var processTimer = new System.Windows.Forms.Timer();
                    processTimer.Interval = 2000; // 2 saniye bekle
                    processTimer.Tick += (s, e) =>
                    {
                        processTimer.Stop();
                        processTimer.Dispose();
                        System.Diagnostics.Debug.WriteLine($"[MongoDB] ProcessQueuedJobs çağrılıyor...");
                        ProcessQueuedJobs();
                    };
                    processTimer.Start();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MongoDB] İşler yüklenirken hata: {ex.Message}");
                System.Console.WriteLine($"[MongoDB] İşler yüklenirken hata: {ex.Message}");
            }
        }

        private void InitializeProgressTimer()
        {
            _progressTimer = new System.Windows.Forms.Timer();
            _progressTimer.Interval = 1000; // Her saniye güncelle
            _progressTimer.Tick += ProgressTimer_Tick;
            // Timer'ı başlat - LoadJobsFromDatabase tamamlandıktan sonra çağrıldığı için
            // yazıcılar zaten güncellenmiş olacak
            _progressTimer.Start();
            System.Diagnostics.Debug.WriteLine($"[MongoDB] ProgressTimer başlatıldı");
        }

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            var activeJobs = _printJobs.Where(j => j.Status == JobStatus.Printing).ToList();
            
            foreach (var job in activeJobs)
            {
                var printer = _printerService.GetPrinter(job.PrinterId);
                
                // Eğer yazıcı yoksa, işi Queued yap
                if (printer == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[MongoDB] ⚠ İş #{job.Id} Printing durumunda ama yazıcı bulunamadı -> Queued yapılıyor");
                    job.Status = JobStatus.Queued;
                    job.Progress = 0;
                    job.StartedAt = null;
                    
                    // MongoDB'de güncelle
                    if (_mongoDbService != null)
                    {
                        try
                        {
                            var filter = Builders<PrintJob>.Filter.Eq(j => j.Id, job.Id);
                            var update = Builders<PrintJob>.Update
                                .Set(j => j.Status, JobStatus.Queued)
                                .Set(j => j.Progress, 0)
                                .Set(j => j.StartedAt, (DateTime?)null);
                            _jobsCollection.UpdateOne(filter, update);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MongoDB] İş durumu düzeltilirken hata: {ex.Message}");
                        }
                    }
                    continue;
                }
                
                // Eğer yazıcı Printing durumunda değilse ama job Printing durumundaysa,
                // yazıcıyı güncellemeyi dene (LoadJobsFromDatabase henüz çalışmamış olabilir)
                if (printer.Status != PrinterStatus.Printing)
                {
                    // Eğer yazıcıda iş bilgisi varsa (MongoDB'den yüklenmiş), yazıcıyı Printing durumuna getir
                    if (!string.IsNullOrEmpty(printer.CurrentJobName) && printer.CurrentJobName == job.ModelFileName)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MongoDB] ⚠ Yazıcı #{printer.Id} Printing durumunda değil ama iş bilgisi var, yazıcı güncelleniyor...");
                        printer.Status = PrinterStatus.Printing;
                        printer.CurrentJobName = job.ModelFileName;
                        printer.JobStartTime = job.StartedAt;
                        printer.JobEndTime = job.EstimatedEndTime;
                        
                        // İlerlemeyi hesapla
                        if (job.StartedAt.HasValue && job.EstimatedEndTime.HasValue)
                        {
                            var now = DateTime.Now;
                            var elapsed = now - job.StartedAt.Value;
                            var total = job.EstimatedEndTime.Value - job.StartedAt.Value;
                            if (total.TotalSeconds > 0)
                            {
                                printer.Progress = Math.Min(100, Math.Max(0, (elapsed.TotalSeconds / total.TotalSeconds) * 100));
                            }
                        }
                        else
                        {
                            printer.Progress = job.Progress;
                        }
                        
                        // MongoDB'de yazıcıyı güncelle
                        if (_mongoDbService != null)
                        {
                            try
                            {
                                var printerCollection = _mongoDbService.GetCollection<Printer>("printers");
                                var filter = Builders<Printer>.Filter.Eq(p => p.Id, printer.Id);
                                var update = Builders<Printer>.Update
                                    .Set(p => p.Status, printer.Status)
                                    .Set(p => p.CurrentJobName, printer.CurrentJobName)
                                    .Set(p => p.JobStartTime, printer.JobStartTime)
                                    .Set(p => p.JobEndTime, printer.JobEndTime)
                                    .Set(p => p.Progress, printer.Progress);
                                printerCollection.UpdateOne(filter, update);
                                System.Diagnostics.Debug.WriteLine($"[MongoDB] ✓ Yazıcı #{printer.Id} Printing durumuna getirildi (ProgressTimer_Tick)");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[MongoDB] Yazıcı güncellenirken hata: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        // Yazıcıda iş bilgisi yoksa, işi Queued yap
                        System.Diagnostics.Debug.WriteLine($"[MongoDB] ⚠ İş #{job.Id} Printing durumunda ama yazıcı aktif değil ve iş bilgisi yok -> Queued yapılıyor");
                        job.Status = JobStatus.Queued;
                        job.Progress = 0;
                        job.StartedAt = null;
                        
                        // MongoDB'de güncelle
                        if (_mongoDbService != null)
                        {
                            try
                            {
                                var filter = Builders<PrintJob>.Filter.Eq(j => j.Id, job.Id);
                                var update = Builders<PrintJob>.Update
                                    .Set(j => j.Status, JobStatus.Queued)
                                    .Set(j => j.Progress, 0)
                                    .Set(j => j.StartedAt, (DateTime?)null);
                                _jobsCollection.UpdateOne(filter, update);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[MongoDB] İş durumu düzeltilirken hata: {ex.Message}");
                            }
                        }
                        continue;
                    }
                }
                
                // StartedAt ve EstimatedEndTime zamanlarına göre ilerlemeyi hesapla
                // Basit zaman bazlı hesaplama: (Geçen süre / Toplam süre) * 100
                if (job.StartedAt.HasValue && job.EstimatedEndTime.HasValue)
                {
                    var now = DateTime.Now;
                    var elapsed = now - job.StartedAt.Value;
                    var total = job.EstimatedEndTime.Value - job.StartedAt.Value;
                    
                    if (total.TotalSeconds > 0)
                    {
                        // Progress = (Geçen süre / Toplam süre) * 100
                        var progress = Math.Min(100, Math.Max(0, (elapsed.TotalSeconds / total.TotalSeconds) * 100));
                        
                        // Eğer süre dolmuşsa (EstimatedEndTime geçmişse) progress %100 yap
                        if (now >= job.EstimatedEndTime.Value)
                        {
                            progress = 100;
                        }
                        
                        job.Progress = progress;
                        
                        // Yazıcının zamanlarını da güncelle (senkronizasyon için)
                        printer.JobStartTime = job.StartedAt;
                        printer.JobEndTime = job.EstimatedEndTime;
                        
                        // İlerlemeye göre filament azalt
                        // FilamentUsage'ı job'dan al, yoksa ModelInfo'dan al
                        double filamentUsage = job.FilamentUsage > 0 ? job.FilamentUsage : (_orderService?.GetModelInfo(job.ModelFileName)?.FilamentUsage ?? 3);
                        
                        // Eğer filament kullanımı kaydedilmemişse, job'a kaydet
                        if (job.FilamentUsage <= 0)
                        {
                            job.FilamentUsage = filamentUsage;
                        }
                        
                        // Eğer JobStartFilament yoksa (yeni başlatılan iş), mevcut filament'i başlangıç olarak kaydet
                        if (!printer.JobStartFilament.HasValue)
                        {
                            printer.JobStartFilament = printer.FilamentRemaining;
                            System.Diagnostics.Debug.WriteLine($"[JobAssignment] Yazıcı #{printer.Id} için JobStartFilament kaydedildi: {printer.JobStartFilament:F1}%");
                        }
                        
                        // Filament = Başlangıç - (Kullanım * İlerleme / 100)
                        if (printer.JobStartFilament.HasValue)
                        {
                            double usedFilament = filamentUsage * (progress / 100.0);
                            printer.FilamentRemaining = Math.Max(0, printer.JobStartFilament.Value - usedFilament);
                            
                            System.Diagnostics.Debug.WriteLine($"[JobAssignment] Yazıcı #{printer.Id} filament güncellendi: {printer.FilamentRemaining:F1}% (Başlangıç: {printer.JobStartFilament:F1}%, Kullanılan: {usedFilament:F1}%, İlerleme: {progress:F1}%)");
                        }
                        
                        bool progressUpdated = _printerService.UpdateJobProgress(job.PrinterId, progress);
                        
                        // Yazıcı güncellendi, MainForm'a bildir (grid güncellenmesi için)
                        if (progressUpdated && PrintersUpdated != null)
                        {
                            // Event'i asenkron olarak tetikle (UI thread'de çalışması için)
                            System.Windows.Forms.Application.DoEvents();
                            PrintersUpdated(this, EventArgs.Empty);
                        }
                        
                        // Filament bittiğinde işi durdur
                        if (!progressUpdated)
                        {
                            // Filament bitti - işi durdur ve kuyruğa ekle
                            job.Status = JobStatus.Queued;
                            job.PrinterId = 0;
                            job.Progress = progress; // Mevcut ilerlemeyi koru
                            
                            // MongoDB'de güncelle
                            if (_mongoDbService != null)
                            {
                                try
                                {
                                    var filter = Builders<PrintJob>.Filter.Eq(j => j.Id, job.Id);
                                    var update = Builders<PrintJob>.Update
                                        .Set(j => j.Status, job.Status)
                                        .Set(j => j.PrinterId, 0)
                                        .Set(j => j.Progress, job.Progress);
                                    _jobsCollection.UpdateOne(filter, update);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[MongoDB] İş durdurulurken hata: {ex.Message}");
                                }
                            }
                            
                            System.Diagnostics.Debug.WriteLine($"[JobAssignment] Filament bitti! İş #{job.Id} durduruldu ve kuyruğa eklendi.");
                            
                            // Filament bitti uyarısı için event tetikle
                            if (FilamentDepleted != null)
                                FilamentDepleted(this, new FilamentDepletedEventArgs(printer, job));
                            
                            continue; // Bu işi atla, bir sonrakine geç
                        }
                        
                        // MongoDB'de ilerlemeyi güncelle (her saniye güncelleniyor)
                        // StartedAt ve EstimatedEndTime değişmez, sadece progress güncellenir
                        if (_mongoDbService != null)
                        {
                            try
                            {
                                var filter = Builders<PrintJob>.Filter.Eq(j => j.Id, job.Id);
                                var update = Builders<PrintJob>.Update
                                    .Set(j => j.Progress, progress);
                                var result = _jobsCollection.UpdateOne(filter, update);
                                if (result.ModifiedCount > 0)
                                {
                                    var remainingSeconds = Math.Max(0, (job.EstimatedEndTime.Value - now).TotalSeconds);
                                    System.Diagnostics.Debug.WriteLine($"[JobAssignment] İş #{job.Id} progress güncellendi: {progress:F1}% (Kalan süre: {remainingSeconds:F0} saniye)");
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[JobAssignment] Progress güncellenirken hata: {ex.Message}");
                            }
                        }

                        if (progress >= 100)
                        {
                            CompleteJob(job);
                        }
                    }
                }
            }
            
            // Kuyruktaki işleri kontrol et ve atanmamış işleri yazıcılara ata
            ProcessQueuedJobs();
        }

        /// <summary>
        /// Model dosya adına göre gerekli filament tipini belirler
        /// </summary>
        private string GetRequiredFilamentType(string modelFileName)
        {
            if (string.IsNullOrEmpty(modelFileName))
                return "PLA";

            string modelLower = modelFileName.ToLower();
            
            // cable modeli -> TPU
            if (modelLower.Contains("cable"))
                return "TPU";
            
            // whist modeli -> PETG
            if (modelLower.Contains("whist"))
                return "PETG";
            
            // octo ve shark modelleri -> PLA
            if (modelLower.Contains("octo") || modelLower.Contains("shark"))
                return "PLA";
            
            // Varsayılan olarak PLA
            return "PLA";
        }

        public void ProcessNewOrder(Order order)
        {
            foreach (var item in order.Items)
            {
                for (int i = 0; i < item.Quantity; i++)
                {
                    AssignJobToAvailablePrinter(order, item);
                }
            }

            _orderService.UpdateOrderStatus(order.Id, OrderStatus.Processing);
        }

        private void AssignJobToAvailablePrinter(Order order, OrderItem item)
        {
            var availablePrinters = _printerService.GetAvailablePrinters();
            
            if (availablePrinters.Count == 0)
            {
                // Boşta yazıcı yoksa kuyruğa ekle
                var queuedJob = new PrintJob
                {
                    Id = _nextJobId++,
                    OrderId = order.Id,
                    OrderItemId = item.Id,
                    PrinterId = 0, // Henüz atanmadı
                    ModelFileName = item.ModelFileName,
                    Status = JobStatus.Queued,
                    Material = item.Material,
                    EstimatedTime = item.EstimatedTime
                };
                _printJobs.Add(queuedJob);
                
                // MongoDB'ye kaydet
                if (_mongoDbService != null)
                {
                    try
                    {
                        _jobsCollection.InsertOne(queuedJob);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MongoDB] İş kaydedilirken hata: {ex.Message}");
                        System.Console.WriteLine($"[MongoDB] İş kaydedilirken hata: {ex.Message}");
                    }
                }
                
                return;
            }

            // Model için gerekli filament tipini belirle
            string requiredFilamentType = GetRequiredFilamentType(item.ModelFileName);
            
            // Model bilgisini al ve filament tüketimini hesapla (önce kontrol için)
            var modelInfo = _orderService.GetModelInfo(item.ModelFileName);
            double filamentUsage = modelInfo?.FilamentUsage ?? 3; // Varsayılan %3
            
            // Önce uygun filament tipine sahip bir yazıcı ara
            var suitablePrinter = availablePrinters.FirstOrDefault(p => p.FilamentType == requiredFilamentType);
            
            // Uygun filament tipine sahip yazıcı yoksa, ilk boşta olan yazıcının filament tipini değiştir
            if (suitablePrinter == null)
            {
                suitablePrinter = availablePrinters.First();
                // Yazıcının filament tipini değiştir
                _printerService.ChangeFilamentType(suitablePrinter.Id, requiredFilamentType);
                System.Diagnostics.Debug.WriteLine($"[JobAssignment] Yazıcı #{suitablePrinter.Id} filament tipi {requiredFilamentType} olarak değiştirildi (Model: {item.ModelFileName})");
            }
            
            // Zamanları önce hesapla (yeni iş için)
            var startTime = DateTime.Now;
            var estimatedEndTime = startTime.AddMinutes(item.EstimatedTime);
            
            // İş ataması yap - filament kontrolü yapılır (ÖNCE KONTROL, SONRA JOB OLUŞTUR)
            bool jobAssigned = _printerService.AssignJobToPrinter(suitablePrinter.Id, item.ModelFileName, item.EstimatedTime, filamentUsage, startTime, estimatedEndTime);
            
            if (!jobAssigned)
            {
                // Filament yetersiz - işi kuyruğa ekle
                var queuedJob = new PrintJob
                {
                    Id = _nextJobId++,
                    OrderId = order.Id,
                    OrderItemId = item.Id,
                    PrinterId = 0, // Henüz atanmadı
                    ModelFileName = item.ModelFileName,
                    Status = JobStatus.Queued,
                    Material = requiredFilamentType,
                    EstimatedTime = item.EstimatedTime
                };
                _printJobs.Add(queuedJob);
                
                // MongoDB'ye kaydet
                if (_mongoDbService != null)
                {
                    try
                    {
                        _jobsCollection.InsertOne(queuedJob);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MongoDB] İş kaydedilirken hata: {ex.Message}");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[JobAssignment] Filament yetersiz! İş #{queuedJob.Id} kuyruğa eklendi.");
                return;
            }
            
            // İş ataması başarılı - job oluştur
            
            var job = new PrintJob
            {
                Id = _nextJobId++,
                OrderId = order.Id,
                OrderItemId = item.Id,
                PrinterId = suitablePrinter.Id,
                ModelFileName = item.ModelFileName,
                Status = JobStatus.Printing,
                StartedAt = startTime,
                EstimatedEndTime = estimatedEndTime,
                Material = requiredFilamentType, // Model için gerekli filament tipini kullan
                EstimatedTime = item.EstimatedTime,
                FilamentUsage = filamentUsage, // Filament kullanımını kaydet
                Progress = 0
            };

            _printJobs.Add(job);
            
            // MongoDB'ye kaydet
            if (_mongoDbService != null)
            {
                try
                {
                    _jobsCollection.InsertOne(job);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MongoDB] İş kaydedilirken hata: {ex.Message}");
                    System.Console.WriteLine($"[MongoDB] İş kaydedilirken hata: {ex.Message}");
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[MongoDB] İş kaydedildi: {job.ModelFileName} (ID: {job.Id}, Filament: {requiredFilamentType}, Tüketim: {filamentUsage}%)");
            
            if (JobAssigned != null)
                JobAssigned(this, new PrintJobEventArgs(job));
        }

        public void ProcessQueuedJobs()
        {
            var queuedJobs = _printJobs.Where(j => j.Status == JobStatus.Queued).ToList();
            var availablePrinters = _printerService.GetAvailablePrinters();

            foreach (var job in queuedJobs)
            {
                if (availablePrinters.Count > 0)
                {
                    // Model için gerekli filament tipini belirle
                    string requiredFilamentType = GetRequiredFilamentType(job.ModelFileName);
                    
                    // Önce uygun filament tipine sahip bir yazıcı ara
                    var suitablePrinter = availablePrinters.FirstOrDefault(p => p.FilamentType == requiredFilamentType);
                    
                    // Uygun filament tipine sahip yazıcı yoksa, ilk boşta olan yazıcının filament tipini değiştir
                    if (suitablePrinter == null)
                    {
                        suitablePrinter = availablePrinters.First();
                        // Yazıcının filament tipini değiştir
                        _printerService.ChangeFilamentType(suitablePrinter.Id, requiredFilamentType);
                        System.Diagnostics.Debug.WriteLine($"[JobAssignment] Yazıcı #{suitablePrinter.Id} filament tipi {requiredFilamentType} olarak değiştirildi (Model: {job.ModelFileName})");
                    }
                    
                    job.PrinterId = suitablePrinter.Id;
                    job.Status = JobStatus.Printing;
                    
                    // Eğer daha önce başlatılmışsa (devam eden iş), StartedAt'ı koru
                    // Yoksa yeni başlatılıyorsa şimdiki zamanı kullan
                    if (!job.StartedAt.HasValue)
                    {
                    job.StartedAt = DateTime.Now;
                    }
                    
                    // EstimatedEndTime'ı güncelle (StartedAt + EstimatedTime)
                    if (job.StartedAt.HasValue)
                    {
                        // Eğer EstimatedEndTime zaten varsa ve geçmişse, progress'e göre yeniden hesapla
                        if (job.EstimatedEndTime.HasValue && DateTime.Now >= job.EstimatedEndTime.Value && job.Progress < 100)
                        {
                            // Kalan süreyi hesapla
                            var remainingProgress = 100 - job.Progress;
                            if (job.Progress > 0)
                            {
                                var elapsedTime = DateTime.Now - job.StartedAt.Value;
                                var estimatedTotalTime = elapsedTime.TotalMinutes * (100.0 / job.Progress);
                                var remainingTime = estimatedTotalTime - elapsedTime.TotalMinutes;
                                
                                // EstimatedEndTime'ı şimdiki zamandan kalan süre kadar ileriye al
                                job.EstimatedEndTime = DateTime.Now.AddMinutes(Math.Max(1, remainingTime));
                                System.Diagnostics.Debug.WriteLine($"[JobAssignment] İş #{job.Id} EstimatedEndTime güncellendi: {job.EstimatedEndTime} (Progress: {job.Progress:F1}%, Kalan: {remainingTime:F1} dk)");
                            }
                            else
                            {
                                // Progress 0 ise, EstimatedTime kadar ileriye al
                                job.EstimatedEndTime = DateTime.Now.AddMinutes(job.EstimatedTime);
                            }
                        }
                        else if (!job.EstimatedEndTime.HasValue)
                        {
                            // EstimatedEndTime yoksa, StartedAt + EstimatedTime olarak hesapla
                            job.EstimatedEndTime = job.StartedAt.Value.AddMinutes(job.EstimatedTime);
                        }
                        // Eğer EstimatedEndTime varsa ve geçmemişse, olduğu gibi bırak
                    }
                    else
                    {
                        job.EstimatedEndTime = DateTime.Now.AddMinutes(job.EstimatedTime);
                    }
                    
                    job.Material = requiredFilamentType; // Model için gerekli filament tipini kullan

                    // MongoDB'de güncelle
                    if (_mongoDbService != null)
                    {
                        try
                        {
                            var filter = Builders<PrintJob>.Filter.Eq(j => j.Id, job.Id);
                            var update = Builders<PrintJob>.Update
                                .Set(j => j.PrinterId, job.PrinterId)
                                .Set(j => j.Status, job.Status)
                                .Set(j => j.StartedAt, job.StartedAt)
                                .Set(j => j.EstimatedEndTime, job.EstimatedEndTime)
                                .Set(j => j.Material, job.Material)
                                .Set(j => j.FilamentUsage, job.FilamentUsage);
                            _jobsCollection.UpdateOne(filter, update);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MongoDB] İş güncellenirken hata: {ex.Message}");
                            System.Console.WriteLine($"[MongoDB] İş güncellenirken hata: {ex.Message}");
                        }
                    }

                    var order = _orderService.GetOrder(job.OrderId);
                    if (order != null)
                    {
                        var item = order.Items.FirstOrDefault(i => i.Id == job.OrderItemId);
                        if (item != null)
                        {
                            // Model bilgisini al ve filament tüketimini hesapla
                            var modelInfo = _orderService.GetModelInfo(item.ModelFileName);
                            double filamentUsage = modelInfo?.FilamentUsage ?? 3; // Varsayılan %3
                            
                            // FilamentUsage'ı job'a kaydet
                            job.FilamentUsage = filamentUsage;
                            
                            // İş ataması yap - filament kontrolü yapılır (devam eden iş için zamanları geçir)
                            bool jobAssigned = _printerService.AssignJobToPrinter(suitablePrinter.Id, item.ModelFileName, item.EstimatedTime, filamentUsage, job.StartedAt, job.EstimatedEndTime);
                            
                            if (!jobAssigned)
                            {
                                // Filament yetersiz - işi kuyruğa ekle
                                job.Status = JobStatus.Queued;
                                job.PrinterId = 0;
                                System.Diagnostics.Debug.WriteLine($"[JobAssignment] Filament yetersiz! İş #{job.Id} kuyruğa eklendi.");
                                continue;
                            }
                            
                            availablePrinters.Remove(suitablePrinter);
                            if (JobAssigned != null)
                                JobAssigned(this, new PrintJobEventArgs(job));
                        }
                    }
                }
            }
        }

        private void CompleteJob(PrintJob job)
        {
            job.Status = JobStatus.Completed;
            job.CompletedAt = DateTime.Now;
            job.Progress = 100;
            
            // MongoDB'de güncelle
            if (_mongoDbService != null)
            {
                try
                {
                    var filter = Builders<PrintJob>.Filter.Eq(j => j.Id, job.Id);
                    var update = Builders<PrintJob>.Update
                        .Set(j => j.Status, job.Status)
                        .Set(j => j.CompletedAt, job.CompletedAt)
                        .Set(j => j.Progress, job.Progress);
                    _jobsCollection.UpdateOne(filter, update);
                }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MongoDB] İş tamamlama güncellenirken hata: {ex.Message}");
                        System.Console.WriteLine($"[MongoDB] İş tamamlama güncellenirken hata: {ex.Message}");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[MongoDB] İş tamamlandı: Job #{job.Id}");
            
            _printerService.CompleteJob(job.PrinterId);
            
            // Siparişin tüm işleri tamamlandı mı kontrol et
            var order = _orderService.GetOrder(job.OrderId);
            if (order != null)
            {
                var orderJobs = _printJobs.Where(j => j.OrderId == order.Id).ToList();
                if (orderJobs.All(j => j.Status == JobStatus.Completed))
                {
                    _orderService.UpdateOrderStatus(order.Id, OrderStatus.Completed);
                }
            }

            if (JobCompleted != null)
                JobCompleted(this, new PrintJobEventArgs(job));
            
            // Kuyruktaki işleri kontrol et
            ProcessQueuedJobs();
        }

        public BindingList<PrintJob> GetAllJobs() => _printJobs;
        public List<PrintJob> GetActiveJobs() => _printJobs.Where(j => j.Status == JobStatus.Printing || j.Status == JobStatus.Queued).ToList();

        public int DeleteCompletedJobs()
        {
            int deletedCount = 0;
            
            try
            {
                var completedJobs = _printJobs.Where(j => j.Status == JobStatus.Completed).ToList();
                deletedCount = completedJobs.Count;
                
                foreach (var job in completedJobs)
                {
                    _printJobs.Remove(job);
                    
                    // MongoDB'den sil
                    if (_mongoDbService != null && _jobsCollection != null)
                    {
                        try
                        {
                            var filter = Builders<PrintJob>.Filter.Eq(j => j.Id, job.Id);
                            _jobsCollection.DeleteOne(filter);
                            System.Diagnostics.Debug.WriteLine($"[MongoDB] Tamamlanan iş silindi: Job #{job.Id}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MongoDB] İş silinirken hata: {ex.Message}");
                        }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[JobAssignmentService] {deletedCount} tamamlanan iş silindi");
                System.Console.WriteLine($"[JobAssignmentService] {deletedCount} tamamlanan iş silindi");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JobAssignmentService] Tamamlanan işler silinirken hata: {ex.Message}");
                System.Console.WriteLine($"[JobAssignmentService] Tamamlanan işler silinirken hata: {ex.Message}");
            }
            
            return deletedCount;
        }

        public bool DeleteJob(int jobId)
        {
            try
            {
                var job = _printJobs.FirstOrDefault(j => j.Id == jobId);
                if (job == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[JobAssignmentService] İş bulunamadı: {jobId}");
                    return false;
                }

                // Sadece tamamlanan işler silinebilir
                if (job.Status != JobStatus.Completed)
                {
                    System.Diagnostics.Debug.WriteLine($"[JobAssignmentService] Sadece tamamlanan işler silinebilir: Job #{jobId} (Durum: {job.Status})");
                    return false;
                }

                _printJobs.Remove(job);

                // MongoDB'den sil
                if (_mongoDbService != null && _jobsCollection != null)
                {
                    try
                    {
                        var filter = Builders<PrintJob>.Filter.Eq(j => j.Id, jobId);
                        _jobsCollection.DeleteOne(filter);
                        System.Diagnostics.Debug.WriteLine($"[MongoDB] İş silindi: Job #{jobId}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MongoDB] İş silinirken hata: {ex.Message}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[JobAssignmentService] İş silindi: Job #{jobId}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JobAssignmentService] İş silinirken hata: {ex.Message}");
                return false;
            }
        }
    }

    public class PrintJobEventArgs : System.EventArgs
    {
        public PrintJob Job { get; }

        public PrintJobEventArgs(PrintJob job)
        {
            Job = job;
        }
    }

    public class FilamentDepletedEventArgs : System.EventArgs
    {
        public Printer Printer { get; }
        public PrintJob Job { get; }

        public FilamentDepletedEventArgs(Printer printer, PrintJob job)
        {
            Printer = printer;
            Job = job;
        }
    }
}


