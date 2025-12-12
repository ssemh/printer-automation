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
                foreach (var job in jobs)
                {
                    _printJobs.Add(job);
                    if (job.Id >= _nextJobId)
                    {
                        _nextJobId = job.Id + 1;
                    }
                }
                System.Diagnostics.Debug.WriteLine($"[MongoDB] {jobs.Count} iş yüklendi");
                System.Console.WriteLine($"[MongoDB] {jobs.Count} iş veritabanından yüklendi");
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
            _progressTimer.Start();
        }

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            var activeJobs = _printJobs.Where(j => j.Status == JobStatus.Printing).ToList();
            
            foreach (var job in activeJobs)
            {
                var printer = _printerService.GetPrinter(job.PrinterId);
                if (printer != null && printer.JobEndTime.HasValue && printer.JobStartTime.HasValue)
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

            // İlk boşta olan yazıcıya ata
            var printer = availablePrinters.First();
            var job = new PrintJob
            {
                Id = _nextJobId++,
                OrderId = order.Id,
                OrderItemId = item.Id,
                PrinterId = printer.Id,
                ModelFileName = item.ModelFileName,
                Status = JobStatus.Printing,
                StartedAt = DateTime.Now,
                Material = item.Material,
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
            
            System.Diagnostics.Debug.WriteLine($"[MongoDB] İş kaydedildi: {job.ModelFileName} (ID: {job.Id})");
            
            _printerService.AssignJobToPrinter(printer.Id, item.ModelFileName, item.EstimatedTime);
            
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
                    var printer = availablePrinters.First();
                    job.PrinterId = printer.Id;
                    job.Status = JobStatus.Printing;
                    job.StartedAt = DateTime.Now;

                    // MongoDB'de güncelle
                    if (_mongoDbService != null)
                    {
                        try
                        {
                            var filter = Builders<PrintJob>.Filter.Eq(j => j.Id, job.Id);
                            var update = Builders<PrintJob>.Update
                                .Set(j => j.PrinterId, job.PrinterId)
                                .Set(j => j.Status, job.Status)
                                .Set(j => j.StartedAt, job.StartedAt);
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
                            _printerService.AssignJobToPrinter(printer.Id, item.ModelFileName, item.EstimatedTime);
                            availablePrinters.Remove(printer);
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


