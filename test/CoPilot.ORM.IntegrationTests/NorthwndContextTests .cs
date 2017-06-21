
using System;
using System.Collections.Generic;
using System.Linq;
using CoPilot.ORM.Common;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.IntegrationTests.Config;
using CoPilot.ORM.IntegrationTests.Models.Northwind;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoPilot.ORM.IntegrationTests
{
    [TestClass]
    public class NorthwndContextTests
    {
        private readonly IDb _db = NorthwndConfig.CreateFromConfig();

        [TestMethod]
        public void CanValidateModel()
        {
            _db.ValidateModel();
        }

        [TestMethod]
        public void CanQueryAllOrdersWithRelatedEntities()
        {
            //var orders = _db.Query<Order>(null, "OrderDetails.Product", "Employee", "Customer").ToArray();
            var orders = _db.From<Order>().Select("OrderDetails.Product", "Employee", "Customer").ToArray();
            Assert.IsNotNull(orders);
            Assert.IsTrue(orders.All(r => r.OrderDetails.Any()));
            Assert.IsNotNull(orders.First()?.OrderDetails.First()?.Product);
        }

        [TestMethod]
        public void CanGetSingleOrderById()
        {
            var order = _db.From<Order>().Where(r => r.OrderId == 10254).Select("OrderDetails.Product", "Employee", "Customer").Single();
            Assert.IsNotNull(order);
            Assert.AreEqual(10254, order.OrderId);
            Assert.AreEqual("Chop-suey Chinese", order.Customer.CompanyName);
            Assert.AreEqual(5, order.Employee.Id);
            Assert.AreEqual(3, order.OrderDetails.Count);
            Assert.AreEqual(625.2f, order.OrderDetails.Sum(r => r.Quantity * r.UnitPrice));
            var order2 = _db.FindByKey<Order>(10254, "OrderDetails.Product", "Employee", "Customer");
            Assert.AreEqual(order.OrderId, order2.OrderId);
            Assert.AreEqual(order.Employee.Id, order2.Employee.Id);
            Assert.AreEqual(order.OrderDetails.Count, order2.OrderDetails.Count);
            Assert.AreEqual(order.OrderDetails.Sum(r => r.Quantity * r.UnitPrice), order2.OrderDetails.Sum(r => r.Quantity * r.UnitPrice));

        }

        [TestMethod]
        public void CanQueryOrdersWithOrderByClauseAndPredicates()
        {
            //var orders = _db.Query(
            //    OrderByClause<Order>.OrderByAscending(r => r.OrderDate)
            //        .ThenByAscending(r => r.ShippedDate),
            //    new Predicates { Skip = 10, Take = 20 }, 
            //    r => r.Employee.Id == 4 && r.ShippedDate.HasValue,
            //     "OrderDetails.Product", "Employee", "Customer"
            //).ToArray();
            var orders = _db.From<Order>()
                .Where(r => r.Employee.Id == 4 && r.ShippedDate.HasValue)
                .Select("OrderDetails.Product", "Employee", "Customer")
                .OrderBy(r => r.OrderDate)
                .ThenBy(r => r.ShippedDate)
                .Skip(10)
                .Take(20)
                .ToArray();
            Assert.IsNotNull(orders);
            Assert.AreEqual(20, orders.Length);
            Assert.IsTrue(orders.Any(r => r.OrderDetails != null && r.OrderDetails.Any()));
        
        }

        [TestMethod]
        public void CanQueryCustomerWithFilterExpressions()
        {
            var customers = _db.From<Customer>().Where(r => r.CompanyName.StartsWith("A")).Select().AsEnumerable();
            Assert.IsNotNull(customers);
            Assert.AreEqual(4, customers.Count());
        }

        [TestMethod]
        public void CanQueryProductWithFilterExpressions()
        {
            var products = _db.From<Product>().Where(r => r.UnitPrice > 10f && r.ProductName != "Test product").Select().ToArray();
            Assert.IsNotNull(products);
            Assert.AreEqual(63, products.Length);

            //var sortedProducts = _db.Query(OrderByClause<Product>.OrderByAscending(r => r.ProductName), r => r.ProductName != "Test product");
            var sortedProducts = _db.From<Product>().Where(r => r.ProductName != "Test product").Select().OrderBy(r => r.ProductName).AsEnumerable();
            Assert.AreEqual("Alice Mutton", sortedProducts.First().ProductName);

            var sortedProductsDesc = _db.From<Product>().Where(r => r.ProductName != "Test product").Select().OrderBy(r => r.ProductName, Ordering.Descending).AsEnumerable();
            Assert.AreEqual("Alice Mutton", sortedProductsDesc.Last().ProductName);
        }

        [TestMethod]
        public void CanCreateAndSaveAnOrder()
        {
            using (var writer = new DbWriter(_db))
            {
                try
                {
                    var customer = new Customer
                    {
                        CompanyName = "Acme",
                        ContactName = "John Smith",
                        Country = "USA",
                        CustomerId = "XYXMZ"
                    };
                    writer.Insert(customer);

                    var newOrder = new Order()
                    {
                        ShipName = "Test",
                        OrderDate = DateTime.Now,
                        Customer = customer,
                        Employee = new Employee
                        {
                            Address = "Some address",
                            City = "Sandnes",
                            BirthDate = new DateTime(1977, 6, 30),
                            Title = "Mr",
                            FirstName = "Kjerand",
                            LastName = "Pedersen",
                            HireDate = new DateTime(2000, 1, 1)
                        }
                    };

                    writer.Save(newOrder, "Employee");

                    var newOrderDetails = new List<OrderDetails>
                    {
                        new OrderDetails
                        {
                            Product = new Product
                            {
                                ProductName = "Test product",
                                QuantityPerUnit = "4 pieces",
                                UnitPrice = 56
                            },
                            Discount = 0.5f,
                            UnitPrice = 28,
                            Quantity = 3,
                            OrderId = newOrder.OrderId
                        }
                    };

                    writer.Insert<OrderDetails>(newOrderDetails, "Product");
                    newOrder.OrderDetails = newOrderDetails;
                    var orderDetail = newOrderDetails.First();
                    orderDetail.Discount = 0.3f;
                    writer.Update(orderDetail);

                    //delete all new records created
                    writer.Delete<OrderDetails>(newOrderDetails, "Product");
                    writer.Delete(newOrder, "Employee", "Customer");

                    writer.Rollback(); //normally you would commit here, but we do a rollback since this is a test
                }
                catch (Exception ex)
                {
                    writer.Rollback();
                    Assert.Fail("Unable to save entity: " + ex.Message);
                }
            } 
        }
    }
}
