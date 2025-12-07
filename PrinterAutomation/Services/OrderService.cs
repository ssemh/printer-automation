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
        private readonly BindingList<Order> _orders = new BindingList<Order>();
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
                _ => new List<string>()
            };
        }

        public Order SimulateECommerceOrder()
        {
            var random = new Random();
            var customerNames = new[] { "Ahmet Yılmaz", "Ayşe Demir", "Mehmet Kaya", "Zeynep Özkan", "Ali Çelik" };
            var materials = new[] { "PLA", "ABS", "PETG" };

            var subfolders = GetModelSubfolders();
            if (subfolders.Count == 0)
                return CreateOrder($"ORD-{DateTime.Now:yyyyMMdd}-{random.Next(1000, 9999)}", 
                    customerNames[random.Next(customerNames.Length)], new List<OrderItem>());

            var selectedSubfolder = subfolders[random.Next(subfolders.Count)];
            var modelsInSubfolder = GetModelsInSubfolder(selectedSubfolder);

            var items = modelsInSubfolder.Select((model, index) => new OrderItem
            {
                Id = index + 1,
                ModelFileName = model,
                Quantity = 1,
                Material = materials[random.Next(materials.Length)],
                EstimatedTime = random.Next(30, 180)
            }).ToList();

            return CreateOrder($"ORD-{DateTime.Now:yyyyMMdd}-{random.Next(1000, 9999)}", 
                customerNames[random.Next(customerNames.Length)], items);
        }
    }
}



