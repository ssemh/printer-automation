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

        public JobAssignmentService(PrinterService printerService, OrderService orderService, MongoDbService mongoDbService = null)
        {
            _printerService = printerService;
            _orderService = orderService;
            _mongoDbService = mongoDbService;
            
            if (_mongoDbService != null)
            {
                _jobsCollection = _mongoDbService.GetCollection<PrintJob>("printJobs");
                LoadJobsFromDatabase();
            }
            
            InitializeProgressTimer();
        }

        private void LoadJobsFromDatabase()
        {
            try
            {
                var jobs = _jobsCollection.Find(_ => true).ToList();
                int printingJobsCount = 0;
                int completedJobsCount = 0;
                int queuedJobsCount = 0;
                
                System.Diagnostics.Debug.WriteLine($"[MongoDB] Toplam {jobs.Count} iş bulundu, durumları kontrol ediliyor...");
                
                foreach (var job in jobs)
                {
                    System.Diagnostics.Debug.WriteLine($"[MongoDB] İş #{job.Id} - Durum: {job.Status}, Progress: {job.Progress}%");
                    
                    // Devam eden işleri (Printing) durdur - uygulama yeniden başlatıldığında
                    // Tüm Printing durumundaki işleri Queued yap ama Progress'i koru
                    if (job.Status == JobStatus.Printing)
                    {
                        var savedProgress = job.Progress; // İlerlemeyi koru
                        System.Diagnostics.Debug.WriteLine($"[MongoDB] Printing iş bulundu: Job #{job.Id}, Progress: {job.Progress}% -> Queued yapılıyor (Progress korunuyor)");
                        
                        // Tüm Printing işlerini Queued yap ama Progress'i koru
                        job.Status = JobStatus.Queued;
                        job.Progress = savedProgress; // İlerlemeyi koru
                        job.StartedAt = null;
                        printingJobsCount++;
                        
                        // Yazıcıyı Idle yap
                        if (job.PrinterId > 0)
                        {
                            var printer = _printerService.GetPrinter(job.PrinterId);
                            if (printer != null)
                            {
                                printer.Status = PrinterStatus.Idle;
                                printer.CurrentJobName = null;
                                printer.JobStartTime = null;
                                printer.JobEndTime = null;
                                printer.Progress = 0; // Yazıcı progress'i sıfırla (iş progress'i korunur)
                                System.Diagnostics.Debug.WriteLine($"[MongoDB] Yazıcı #{printer.Id} Idle durumuna getirildi");
                            }
                        }
                        
                        // MongoDB'de güncelle (Progress korunur)
                        if (_mongoDbService != null)
                        {
                            try
                            {
                                var filter = Builders<PrintJob>.Filter.Eq(j => j.Id, job.Id);
                                var update = Builders<PrintJob>.Update
                                    .Set(j => j.Status, JobStatus.Queued)
                                    .Set(j => j.Progress, savedProgress) // İlerlemeyi koru
                                    .Set(j => j.StartedAt, (DateTime?)null);
                                var result = _jobsCollection.UpdateOne(filter, update);
                                if (result.ModifiedCount > 0)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[MongoDB] ✓ İş durduruldu: Job #{job.Id} (Printing -> Queued, Progress: {savedProgress}% korundu)");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"[MongoDB] ⚠ İş durdurulurken MongoDB güncellemesi başarısız: Job #{job.Id}");
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[MongoDB] ✗ İş durdurulurken hata: {ex.Message}");
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
                
                System.Diagnostics.Debug.WriteLine($"[MongoDB] Özet: {jobs.Count} iş yüklendi - {printingJobsCount} Printing->Queued, {queuedJobsCount} Queued, {completedJobsCount} Completed");
                System.Console.WriteLine($"[MongoDB] {jobs.Count} iş veritabanından yüklendi ({printingJobsCount} devam eden iş durduruldu)");
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
            // Timer'ı başlatmadan önce biraz bekle (yükleme işlemlerinin tamamlanması için)
            System.Threading.Thread.Sleep(100);
            _progressTimer.Start();
            System.Diagnostics.Debug.WriteLine($"[MongoDB] ProgressTimer başlatıldı");
        }

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            var activeJobs = _printJobs.Where(j => j.Status == JobStatus.Printing).ToList();
            
            foreach (var job in activeJobs)
            {
                var printer = _printerService.GetPrinter(job.PrinterId);
                
                // Eğer yazıcı yoksa veya Printing durumunda değilse, işi Queued yap
                if (printer == null || printer.Status != PrinterStatus.Printing)
                {
                    System.Diagnostics.Debug.WriteLine($"[MongoDB] ⚠ İş #{job.Id} Printing durumunda ama yazıcı aktif değil -> Queued yapılıyor");
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
                
                // Sadece gerçekten aktif bir yazıcıya atanmış ve zamanları olan işleri işle
                if (printer.JobEndTime.HasValue && printer.JobStartTime.HasValue)
                {
                    var elapsed = DateTime.Now - printer.JobStartTime.Value;
                    var total = printer.JobEndTime.Value - printer.JobStartTime.Value;
                    
                    if (total.TotalSeconds > 0)
                    {
                        var progress = Math.Min(100, (elapsed.TotalSeconds / total.TotalSeconds) * 100);
                        job.Progress = progress;
                        _printerService.UpdateJobProgress(job.PrinterId, progress);
                        
                        // MongoDB'de ilerlemeyi güncelle (her %10 değişimde veya tamamlandığında)
                        // Performans için sık güncelleme yapmıyoruz
                        if (_mongoDbService != null && ((int)progress % 10 == 0 || progress >= 100))
                        {
                            try
                            {
                                var filter = Builders<PrintJob>.Filter.Eq(j => j.Id, job.Id);
                                var update = Builders<PrintJob>.Update.Set(j => j.Progress, progress);
                                var result = _jobsCollection.UpdateOne(filter, update);
                                if (result.ModifiedCount > 0)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[MongoDB] İş #{job.Id} ilerlemesi güncellendi: %{progress:F1}");
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[MongoDB] İş ilerlemesi güncellenirken hata: {ex.Message}");
                            }
                        }

                        if (progress >= 100)
                        {
                            CompleteJob(job);
                        }
                    }
                }
            }
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
            
            var job = new PrintJob
            {
                Id = _nextJobId++,
                OrderId = order.Id,
                OrderItemId = item.Id,
                PrinterId = suitablePrinter.Id,
                ModelFileName = item.ModelFileName,
                Status = JobStatus.Printing,
                StartedAt = DateTime.Now,
                Material = requiredFilamentType, // Model için gerekli filament tipini kullan
                EstimatedTime = item.EstimatedTime,
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
            
            System.Diagnostics.Debug.WriteLine($"[MongoDB] İş kaydedildi: {job.ModelFileName} (ID: {job.Id}, Filament: {requiredFilamentType})");
            
            // Model bilgisini al ve filament tüketimini hesapla
            var modelInfo = _orderService.GetModelInfo(item.ModelFileName);
            double filamentUsage = modelInfo?.FilamentUsage ?? 3; // Varsayılan %3
            
            _printerService.AssignJobToPrinter(suitablePrinter.Id, item.ModelFileName, item.EstimatedTime);
            
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
                    job.StartedAt = DateTime.Now;
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
                                .Set(j => j.Material, job.Material);
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
                            
                            _printerService.AssignJobToPrinter(suitablePrinter.Id, item.ModelFileName, item.EstimatedTime);
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
}


