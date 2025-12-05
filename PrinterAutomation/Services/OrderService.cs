using System;
using System.Collections.Generic;
using System.Linq;
using PrinterAutomation.Models;
using System.ComponentModel;
using System.IO;

namespace PrinterAutomation.Services
{
    public class OrderService
    {
        private BindingList<Order> _orders = new BindingList<Order>();
        private int _nextOrderId = 1;

        public BindingList<Order> GetAllOrders() => _orders;

        public Order? GetOrder(int id) => _orders.FirstOrDefault(o => o.Id == id);

        public List<Order> GetPendingOrders() => 
            _orders.Where(o => o.Status == OrderStatus.Pending).ToList();

        public Order CreateOrder(string orderNumber, string customerName, List<OrderItem> items)
        {
            var order = new Order
            {
                Id = _nextOrderId++,
                OrderNumber = orderNumber,
                CustomerName = customerName,
                Items = items,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending
            };

            _orders.Add(order);
            return order;
        }

        public void UpdateOrderStatus(int orderId, OrderStatus status)
        {
            var order = GetOrder(orderId);
            if (order != null)
            {
                order.Status = status;
            }
        }

        // Model klasöründeki STL dosyalarını al
        private List<string> GetAvailableModelFiles()
        {
            var modelFiles = new List<string>();
            
            // Model klasörünün farklı olası konumlarını dene
            var possiblePaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "model"),
                Path.Combine(Directory.GetCurrentDirectory(), "model"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "model"),
                Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "..", "..", "..", "..", "model"),
                "model" // Doğrudan model klasörü
            };

            string modelDirectory = null;
            foreach (var path in possiblePaths)
            {
                try
                {
                    var fullPath = Path.GetFullPath(path);
                    if (Directory.Exists(fullPath))
                    {
                        modelDirectory = fullPath;
                        break;
                    }
                }
                catch
                {
                    // Devam et
                }
            }

            if (modelDirectory != null && Directory.Exists(modelDirectory))
            {
                try
                {
                    // Tüm alt klasörlerdeki STL dosyalarını bul
                    var stlFiles = Directory.GetFiles(modelDirectory, "*.stl", SearchOption.AllDirectories);
                    foreach (var file in stlFiles)
                    {
                        try
                        {
                            // Göreli yol olarak ekle (model klasöründen itibaren)
                            var fullModelPath = Path.GetFullPath(file);
                            var fullModelDir = Path.GetFullPath(modelDirectory);
                            
                            if (fullModelPath.StartsWith(fullModelDir, StringComparison.OrdinalIgnoreCase))
                            {
                                var relativePath = fullModelPath.Substring(fullModelDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                                modelFiles.Add(relativePath.Replace('\\', '/'));
                            }
                        }
                        catch
                        {
                            // Dosya adını direkt kullan
                            modelFiles.Add(Path.GetFileName(file));
                        }
                    }
                }
                catch
                {
                    // Hata durumunda varsayılan modelleri kullan
                }
            }

            // Eğer hiç model bulunamazsa, varsayılan modelleri kullan
            if (modelFiles.Count == 0)
            {
                modelFiles.AddRange(new[] 
                { 
                    "octo/articulatedcuteoctopus.stl",
                    "shark/body.stl",
                    "shark/head_easy_press_in.stl",
                    "shark/head_hard_press_in.stl"
                });
            }

            return modelFiles;
        }

        // Model klasörlerini grupla ve her klasördeki dosyaları döndür
        private Dictionary<string, List<string>> GetModelFilesByFolder()
        {
            var modelFilesByFolder = new Dictionary<string, List<string>>();
            
            // Model klasörünün farklı olası konumlarını dene
            var possiblePaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "model"),
                Path.Combine(Directory.GetCurrentDirectory(), "model"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "model"),
                Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "..", "..", "..", "..", "model"),
                "model" // Doğrudan model klasörü
            };

            string modelDirectory = null;
            foreach (var path in possiblePaths)
            {
                try
                {
                    var fullPath = Path.GetFullPath(path);
                    if (Directory.Exists(fullPath))
                    {
                        modelDirectory = fullPath;
                        break;
                    }
                }
                catch
                {
                    // Devam et
                }
            }

            if (modelDirectory != null && Directory.Exists(modelDirectory))
            {
                try
                {
                    // Tüm alt klasörlerdeki STL dosyalarını bul
                    var stlFiles = Directory.GetFiles(modelDirectory, "*.stl", SearchOption.AllDirectories);
                    var fullModelDir = Path.GetFullPath(modelDirectory);
                    
                    foreach (var file in stlFiles)
                    {
                        try
                        {
                            var fullModelPath = Path.GetFullPath(file);
                            
                            if (fullModelPath.StartsWith(fullModelDir, StringComparison.OrdinalIgnoreCase))
                            {
                                var relativePath = fullModelPath.Substring(fullModelDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                                var normalizedPath = relativePath.Replace('\\', '/');
                                
                                // Klasör adını al (ilk / karakterinden önceki kısım)
                                var folderName = normalizedPath.Contains('/') 
                                    ? normalizedPath.Substring(0, normalizedPath.IndexOf('/'))
                                    : ""; // Root klasördeki dosyalar için
                                
                                if (string.IsNullOrEmpty(folderName))
                                {
                                    folderName = "root"; // Root klasördeki dosyalar için
                                }
                                
                                if (!modelFilesByFolder.ContainsKey(folderName))
                                {
                                    modelFilesByFolder[folderName] = new List<string>();
                                }
                                
                                modelFilesByFolder[folderName].Add(normalizedPath);
                            }
                        }
                        catch
                        {
                            // Dosya adını direkt kullan
                            var fileName = Path.GetFileName(file);
                            if (!modelFilesByFolder.ContainsKey("root"))
                            {
                                modelFilesByFolder["root"] = new List<string>();
                            }
                            modelFilesByFolder["root"].Add(fileName);
                        }
                    }
                }
                catch
                {
                    // Hata durumunda varsayılan modelleri kullan
                }
            }

            // Eğer hiç model bulunamazsa, varsayılan modelleri kullan
            if (modelFilesByFolder.Count == 0)
            {
                modelFilesByFolder["octo"] = new List<string> { "octo/articulatedcuteoctopus.stl" };
                modelFilesByFolder["shark"] = new List<string> 
                { 
                    "shark/body.stl",
                    "shark/head_easy_press_in.stl",
                    "shark/head_hard_press_in.stl"
                };
            }

            return modelFilesByFolder;
        }

        // E-ticaret sitesinden gelen sipariş simülasyonu
        public Order SimulateECommerceOrder()
        {
            var random = new Random();
            var orderNumber = $"ORD-{DateTime.Now:yyyyMMdd}-{random.Next(1000, 9999)}";
            var customerNames = new[] { "Ahmet Yılmaz", "Ayşe Demir", "Mehmet Kaya", "Zeynep Özkan", "Ali Çelik" };
            var materials = new[] { "PLA", "ABS", "PETG" };

            // Model klasörlerini grupla
            var modelFilesByFolder = GetModelFilesByFolder();
            
            if (modelFilesByFolder.Count == 0)
            {
                // Fallback: varsayılan modeller
                modelFilesByFolder["octo"] = new List<string> { "octo/articulatedcuteoctopus.stl" };
                modelFilesByFolder["shark"] = new List<string> 
                { 
                    "shark/body.stl",
                    "shark/head_easy_press_in.stl",
                    "shark/head_hard_press_in.stl"
                };
            }

            var items = new List<OrderItem>();
            int itemId = 1;

            // Sadece BİR model klasörü seç (rastgele)
            var folderKeys = modelFilesByFolder.Keys.ToList();
            if (folderKeys.Count > 0)
            {
                var selectedFolder = folderKeys[random.Next(folderKeys.Count)];
                var folderFiles = modelFilesByFolder[selectedFolder];
                
                // Seçilen klasördeki TÜM dosyaları siparişe ekle
                foreach (var modelFile in folderFiles)
                {
                    items.Add(new OrderItem
                    {
                        Id = itemId++,
                        ModelFileName = modelFile, // Tam yol ile kaydet
                        Quantity = 1, // Her dosya 1 adet
                        Material = materials[random.Next(materials.Length)],
                        EstimatedTime = random.Next(30, 180) // 30-180 dakika
                    });
                }
            }

            return CreateOrder(orderNumber, customerNames[random.Next(customerNames.Length)], items);
        }
    }
}


