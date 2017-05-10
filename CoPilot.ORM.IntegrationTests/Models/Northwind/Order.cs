using System;
using System.Collections.Generic;

namespace CoPilot.ORM.IntegrationTests.Models.Northwind
{
    public class Order
    {
        public int OrderId { get; set; }
        public DateTime? OrderDate { get; set; }
        public DateTime? RequiredDate { get; set; }
        public DateTime? ShippedDate { get; set; }
        public string ShipName { get; set; }
        public Customer Customer { get; set; }
        public Employee Employee { get; set; }
        public List<OrderDetails> OrderDetails { get; set; }

    }

    public class OrderDetails
    {
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }
        public float UnitPrice { get; set; }
        public int Quantity { get; set; }
        public float Discount { get; set; }
    }

    public class Product
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string QuantityPerUnit { get; set; }
        public float UnitPrice { get; set; }
    }
}
