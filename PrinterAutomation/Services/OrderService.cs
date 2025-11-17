using System;
using System.Collections.Generic;
using System.Linq;
using PrinterAutomation.Models;
using System.ComponentModel;

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

        // E-ticaret sitesinden gelen sipariş simülasyonu
        public Order SimulateECommerceOrder()
        {
            var random = new Random();
            var orderNumber = $"ORD-{DateTime.Now:yyyyMMdd}-{random.Next(1000, 9999)}";
            var customerNames = new[] { "Ahmet Yılmaz", "Ayşe Demir", "Mehmet Kaya", "Zeynep Özkan", "Ali Çelik" };
            var modelFiles = new[] { "model1.stl", "model2.stl", "model3.stl", "model4.stl", "model5.stl" };
            var materials = new[] { "PLA", "ABS", "PETG" };

            var items = new List<OrderItem>();
            int itemCount = random.Next(1, 4); // 1-3 arası item

            for (int i = 0; i < itemCount; i++)
            {
                items.Add(new OrderItem
                {
                    Id = i + 1,
                    ModelFileName = modelFiles[random.Next(modelFiles.Length)],
                    Quantity = random.Next(1, 5),
                    Material = materials[random.Next(materials.Length)],
                    EstimatedTime = random.Next(30, 180) // 30-180 dakika
                });
            }

            return CreateOrder(orderNumber, customerNames[random.Next(customerNames.Length)], items);
        }
    }
}


