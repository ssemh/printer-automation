using System;
using System.Collections.Generic;
using System.Linq;
using PrinterAutomation.Models;
using System.ComponentModel;
using System.IO;
using MongoDB.Driver;

namespace PrinterAutomation.Services
{
    public class OrderService
    {
        private readonly BindingList<Order> _orders = new BindingList<Order>();
        private readonly MongoDbService? _mongoDbService;
        private readonly IMongoCollection<Order>? _ordersCollection;
        private readonly IMongoCollection<ModelInfo>? _modelsCollection;
        private int _nextOrderId = 1;

        public OrderService(MongoDbService mongoDbService = null)
        {
            _mongoDbService = mongoDbService;
            System.Diagnostics.Debug.WriteLine($"[OrderService] Constructor çağrıldı. MongoDB servisi: {(_mongoDbService != null ? "MEVCUT" : "NULL")}");
            
            if (_mongoDbService != null)
            {
                try
                {
                    _ordersCollection = _mongoDbService.GetCollection<Order>("orders");
                    _modelsCollection = _mongoDbService.GetCollection<ModelInfo>("modelInfos");
                    System.Diagnostics.Debug.WriteLine($"[OrderService] Collection oluşturuldu: orders, modelInfos");
                    System.Diagnostics.Debug.WriteLine($"[OrderService] Collection null mu? {(_ordersCollection == null ? "EVET" : "HAYIR")}");
                    LoadOrdersFromDatabase();
                    
                    // Model bilgilerini başlat (hata olsa bile devam et)
                    try
                    {
                        InitializeModelInfos();
                    }
                    catch (Exception modelEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[OrderService] Model bilgileri başlatılırken hata: {modelEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"[OrderService] Model bilgileri StackTrace: {modelEx.StackTrace}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[OrderService] Collection oluşturulurken hata: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[OrderService] StackTrace: {ex.StackTrace}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[OrderService] ⚠ MongoDB servisi NULL - veriler sadece bellekte tutulacak");
            }
        }

        private void LoadOrdersFromDatabase()
        {
            if (_ordersCollection == null) return;
            
            try
            {
                var orders = _ordersCollection.Find(_ => true).ToList();
                foreach (var order in orders)
                {
                    _orders.Add(order);
                    if (order.Id >= _nextOrderId)
                    {
                        _nextOrderId = order.Id + 1;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MongoDB] Siparişler yüklenirken hata: {ex.Message}");
                System.Console.WriteLine($"[MongoDB] Siparişler yüklenirken hata: {ex.Message}");
            }
        }

        public BindingList<Order> GetAllOrders() => _orders;

        public int DeleteCompletedOrders()
        {
            int deletedCount = 0;
            
            try
            {
                var completedOrders = _orders.Where(o => o.Status == OrderStatus.Completed).ToList();
                deletedCount = completedOrders.Count;
                
                foreach (var order in completedOrders)
                {
                    _orders.Remove(order);
                    
                    // MongoDB'den sil
                    if (_mongoDbService != null && _ordersCollection != null)
                    {
                        try
                        {
                            var filter = Builders<Order>.Filter.Eq(o => o.Id, order.Id);
                            _ordersCollection.DeleteOne(filter);
                            System.Diagnostics.Debug.WriteLine($"[MongoDB] Tamamlanan sipariş silindi: Order #{order.Id} ({order.OrderNumber})");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MongoDB] Sipariş silinirken hata: {ex.Message}");
                        }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[OrderService] {deletedCount} tamamlanan sipariş silindi");
                System.Console.WriteLine($"[OrderService] {deletedCount} tamamlanan sipariş silindi");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OrderService] Tamamlanan siparişler silinirken hata: {ex.Message}");
                System.Console.WriteLine($"[OrderService] Tamamlanan siparişler silinirken hata: {ex.Message}");
            }
            
            return deletedCount;
        }

        public Order? GetOrder(int id) => _orders.FirstOrDefault(o => o.Id == id);

        public bool DeleteOrder(int orderId)
        {
            try
            {
                var order = _orders.FirstOrDefault(o => o.Id == orderId);
                if (order == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[OrderService] Sipariş bulunamadı: {orderId}");
                    return false;
                }

                _orders.Remove(order);

                // MongoDB'den sil
                if (_mongoDbService != null && _ordersCollection != null)
                {
                    try
                    {
                        var filter = Builders<Order>.Filter.Eq(o => o.Id, orderId);
                        _ordersCollection.DeleteOne(filter);
                        System.Diagnostics.Debug.WriteLine($"[MongoDB] Sipariş silindi: Order #{orderId} ({order.OrderNumber})");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MongoDB] Sipariş silinirken hata: {ex.Message}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[OrderService] Sipariş silindi: Order #{orderId}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OrderService] Sipariş silinirken hata: {ex.Message}");
                return false;
            }
        }

        public List<Order> GetPendingOrders() => 
            _orders.Where(o => o.Status == OrderStatus.Pending).ToList();

        public Order CreateOrder(string orderNumber, string customerName, List<OrderItem> items)
        {
            System.Diagnostics.Debug.WriteLine($"[OrderService] CreateOrder çağrıldı: {orderNumber}");
            
            // Fiyatlandırma: Model setine göre fiyat belirle
            decimal totalPrice = 0;
            if (items.Count > 0)
            {
                // İlk item'ın klasör adından model setini belirle
                var firstItem = items[0].ModelFileName;
                string modelSet = firstItem.Contains("/") ? firstItem.Split('/')[0].ToLower() : "";
                
                // Octo için 100 TL, diğerleri için 150 TL
                totalPrice = modelSet == "octo" ? 100 : 150;
            }

            var order = new Order
            {
                Id = _nextOrderId++,
                OrderNumber = orderNumber,
                CustomerName = customerName,
                Items = items,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending,
                TotalPrice = totalPrice
            };

            _orders.Add(order);
            System.Diagnostics.Debug.WriteLine($"[OrderService] Sipariş belleğe eklendi: {order.OrderNumber} (ID: {order.Id})");
            
            // MongoDB'ye kaydet
            bool savedToMongoDb = false;
            string mongoError = null;
            
            if (_mongoDbService == null)
            {
                mongoError = "MongoDB servisi NULL";
                System.Diagnostics.Debug.WriteLine($"[MongoDB] ⚠ {mongoError}");
            }
            else if (_ordersCollection == null)
            {
                mongoError = "Orders collection NULL";
                System.Diagnostics.Debug.WriteLine($"[MongoDB] ⚠ {mongoError}");
            }
            else
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[MongoDB] Sipariş kaydediliyor: {order.OrderNumber} (ID: {order.Id})");
                    _ordersCollection.InsertOne(order);
                    savedToMongoDb = true;
                    System.Diagnostics.Debug.WriteLine($"[MongoDB] ✓ Sipariş başarıyla kaydedildi: {order.OrderNumber} (ID: {order.Id})");
                }
                catch (Exception ex)
                {
                    savedToMongoDb = false;
                    mongoError = ex.Message;
                    System.Diagnostics.Debug.WriteLine($"[MongoDB] ✗ Sipariş kaydedilirken hata: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[MongoDB] StackTrace: {ex.StackTrace}");
                    
                    // Hata mesajını kullanıcıya göster
                    System.Windows.Forms.MessageBox.Show(
                        $"MongoDB'ye sipariş kaydedilemedi!\n\nHata: {ex.Message}\n\nSipariş bellekte tutuluyor.",
                        "MongoDB Kayıt Hatası",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Warning);
                }
            }
            
            if (!savedToMongoDb && mongoError != null)
            {
                System.Diagnostics.Debug.WriteLine($"[MongoDB] ⚠ Sipariş MongoDB'ye kaydedilemedi: {mongoError}");
            }
            
            // MongoDB kayıt durumunu order'a ekle (opsiyonel - reflection ile)
            // Şimdilik sadece log olarak bırakıyoruz
            
            return order;
        }

        public void UpdateOrderStatus(int orderId, OrderStatus status)
        {
            var order = GetOrder(orderId);
            if (order != null)
            {
                order.Status = status;
                
                // MongoDB'de güncelle
                if (_mongoDbService != null && _ordersCollection != null)
                {
                    try
                    {
                        var filter = Builders<Order>.Filter.Eq(o => o.Id, orderId);
                        var update = Builders<Order>.Update.Set(o => o.Status, status);
                        _ordersCollection.UpdateOne(filter, update);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MongoDB] Sipariş güncellenirken hata: {ex.Message}");
                        System.Console.WriteLine($"[MongoDB] Sipariş güncellenirken hata: {ex.Message}");
                    }
                }
            }
        }

        private string GetModelFolderPath()
        {
            try
            {
                var paths = new[]
                {
                    Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "model")),
                    Path.Combine(Directory.GetCurrentDirectory(), "model"),
                    Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())?.FullName ?? "", "model")
                };

                return paths.FirstOrDefault(Directory.Exists);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Model klasörü bulunurken hata: {ex.Message}");
                return null;
            }
        }

        private List<string> GetModelSubfolders()
        {
            var modelPath = GetModelFolderPath();
            if (string.IsNullOrEmpty(modelPath)) 
                return new List<string> { "octo", "shark" };

            try
            {
                var subfolders = Directory.GetDirectories(modelPath)
                    .Where(dir => Directory.GetFiles(dir, "*.stl").Length > 0)
                    .Select(Path.GetFileName)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Cast<string>()
                    .ToList();

                return subfolders.Count > 0 ? subfolders : new List<string> { "octo", "shark" };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Alt klasörler bulunurken hata: {ex.Message}");
                return new List<string> { "octo", "shark" };
            }
        }

        private List<string> GetModelsInSubfolder(string subfolderName)
        {
            var modelPath = GetModelFolderPath();
            if (string.IsNullOrEmpty(modelPath))
                return GetDefaultModels(subfolderName);

            try
            {
                var subfolderPath = Path.Combine(modelPath, subfolderName);
                if (!Directory.Exists(subfolderPath))
                    return GetDefaultModels(subfolderName);

                var modelFiles = Directory.GetFiles(subfolderPath, "*.stl")
                    .Select(file =>
                    {
                        var relativePath = file.Substring(modelPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        return relativePath.Replace('\\', '/');
                    })
                    .ToList();

                return modelFiles.Count > 0 ? modelFiles : GetDefaultModels(subfolderName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Alt klasördeki dosyalar bulunurken hata: {ex.Message}");
                return GetDefaultModels(subfolderName);
            }
        }

        private static List<string> GetDefaultModels(string subfolderName)
        {
            return subfolderName switch
            {
                "octo" => new List<string> { "octo/articulatedcuteoctopus.stl" },
                "shark" => new List<string> { 
                    "shark/body.stl",
                    "shark/head_easy_press_in.stl",
                    "shark/head_hard_press_in.stl"
                },
                "whist" => new List<string> { "whist/v29d_engraved.stl" },
                _ => new List<string>()
            };
        }

        private void InitializeModelInfos()
        {
            if (_mongoDbService == null || _modelsCollection == null)
            {
                System.Diagnostics.Debug.WriteLine($"[ModelInfo] MongoDB servisi veya collection null - model bilgileri başlatılamıyor");
                return;
            }

            try
            {
                // Mevcut model bilgilerini kontrol et
                var existingModels = _modelsCollection.Find(_ => true).ToList();
                if (existingModels.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[ModelInfo] {existingModels.Count} model bilgisi zaten mevcut");
                    return;
                }

                // Tüm modelleri bul ve bilgilerini oluştur
                var subfolders = GetModelSubfolders();
                if (subfolders == null || subfolders.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[ModelInfo] Model klasörü bulunamadı veya alt klasör yok");
                    return;
                }

                var allModels = new List<ModelInfo>();

                foreach (var subfolder in subfolders)
                {
                    try
                    {
                        var models = GetModelsInSubfolder(subfolder);
                        if (models == null || models.Count == 0) 
                        {
                            System.Diagnostics.Debug.WriteLine($"[ModelInfo] {subfolder} klasöründe model bulunamadı");
                            continue;
                        }
                        
                        foreach (var model in models)
                        {
                            // Varsayılan değerler (model dosyasına göre)
                            double estimatedTime = 60; // Dakika
                            double filamentUsage = 5; // %

                            // Model tipine göre varsayılan değerler
                            if (model.Contains("octo"))
                            {
                                estimatedTime = 90;
                                filamentUsage = 8;
                            }
                            else if (model.Contains("shark"))
                            {
                                if (model.Contains("body"))
                                {
                                    estimatedTime = 120;
                                    filamentUsage = 12;
                                }
                                else if (model.Contains("head"))
                                {
                                    estimatedTime = 60;
                                    filamentUsage = 6;
                                }
                            }
                            else if (model.Contains("whist"))
                            {
                                estimatedTime = 75;
                                filamentUsage = 7;
                            }

                            allModels.Add(new ModelInfo
                            {
                                ModelFileName = model,
                                EstimatedTime = estimatedTime,
                                FilamentUsage = filamentUsage
                            });
                        }
                    }
                    catch (Exception subfolderEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ModelInfo] {subfolder} klasörü işlenirken hata: {subfolderEx.Message}");
                    }
                }

                if (allModels.Count > 0)
                {
                    _modelsCollection.InsertMany(allModels);
                    System.Diagnostics.Debug.WriteLine($"[ModelInfo] {allModels.Count} model bilgisi oluşturuldu");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ModelInfo] Model bilgileri oluşturulurken hata: {ex.Message}");
            }
        }

        public ModelInfo? GetModelInfo(string modelFileName)
        {
            if (_modelsCollection == null) 
                return null;

            try
            {
                return _modelsCollection.Find(m => m.ModelFileName == modelFileName).FirstOrDefault();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ModelInfo] Model bilgisi alınırken hata: {ex.Message}");
                return null;
            }
        }

        public Order SimulateECommerceOrder()
        {
            var random = new Random();
            var customerNames = new[] 
            { 
                "Ahmet Yılmaz", "Ayşe Demir", "Mehmet Kaya", "Zeynep Özkan", "Ali Çelik",
                 "Mustafa Arslan", "Elif Yıldız", "Hasan Korkmaz", "Selin Aydın",
                "Burak Özdemir", "Ceren Avcı", "Emre Doğan", "Gizem Çetin", "Kaan Yücel",
                "Leyla Aktaş", "Onur Şen", "Pınar Koç", "Serkan Güneş", "Tuğba Erdem",
                "Uğur Çakır", "Yasemin Karaca", "Barış Öztürk", "Derya Yılmaz", "Furkan Aydın",
                "Gamze Kaya", "Hakan Demir", "İrem Şahin", "Kemal Arslan", "Melis Yıldız",
                "Nazlı Korkmaz", "Okan Aydın", "Pelin Çelik", "Rıza Özkan", "Seda Demir",
                "Tolga Yılmaz", "Ülkü Kaya", "Volkan Şahin", "Zeynep Arslan", "Arda Yıldız",
                "Beste Korkmaz", "Can Aydın", "Deniz Çelik", "Ece Özkan", "Fırat Demir",
                "Gökçe Yılmaz", "Hüseyin Kaya", "İpek Şahin", "Jale Arslan", "Koray Yıldız"
            };
            var materials = new[] { "PLA", "ABS", "PETG" };

            var subfolders = GetModelSubfolders();
            if (subfolders.Count == 0)
                return CreateOrder($"ORD-{DateTime.Now:yyyyMMdd}-{random.Next(1000, 9999)}", 
                    customerNames[random.Next(customerNames.Length)], new List<OrderItem>());

            var selectedSubfolder = subfolders[random.Next(subfolders.Count)];
            var modelsInSubfolder = GetModelsInSubfolder(selectedSubfolder);

            var items = modelsInSubfolder.Select((model, index) =>
            {
                // Model bilgisini veritabanından al
                var modelInfo = GetModelInfo(model);
                double estimatedTime = modelInfo?.EstimatedTime ?? 60; // Varsayılan 60 dakika

                return new OrderItem
                {
                    Id = index + 1,
                    ModelFileName = model,
                    Quantity = 1,
                    Material = materials[random.Next(materials.Length)],
                    EstimatedTime = estimatedTime
                };
            }).ToList();

            return CreateOrder($"ORD-{DateTime.Now:yyyyMMdd}-{random.Next(1000, 9999)}", 
                customerNames[random.Next(customerNames.Length)], items);
        }
    }
}
