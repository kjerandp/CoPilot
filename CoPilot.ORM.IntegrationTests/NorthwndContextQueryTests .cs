
using System;
using System.Collections.Generic;
using System.Linq;
using CoPilot.ORM.Context.Query;
using CoPilot.ORM.Database;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.IntegrationTests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoPilot.ORM.IntegrationTests
{
    [TestClass]
    public class NorthwndContextQueryTests
    {
        private readonly IDb _db = NorthwndConfig.CreateFromConfig();

        [TestMethod]
        public void CanQueryAllOrdersWithRelatedEntities()
        {
            var orders = _db.Query<Order>(null, "OrderDetails.Product", "Employee", "Customer").ToArray();
            Assert.IsNotNull(orders);
            Assert.IsTrue(orders.All(r => r.OrderDetails.Any()));
            Assert.IsNotNull(orders.First()?.OrderDetails.First()?.Product);
        }

        [TestMethod]
        public void CanGetSingleOrderById()
        {
            var order = _db.Single<Order>(r => r.OrderId == 10254, "OrderDetails.Product", "Employee", "Customer");
            Assert.IsNotNull(order);
            Assert.AreEqual(10254, order.OrderId);
            Assert.AreEqual("Chop-suey Chinese", order.Customer.CompanyName);
            Assert.AreEqual(5, order.Employee.Id);
            Assert.AreEqual(3, order.OrderDetails.Count);
            Assert.AreEqual(625.2f, order.OrderDetails.Sum(r => r.Quantity * r.UnitPrice));
        }

        [TestMethod]
        public void CanQueryOrdersWithOrderByClauseAndPredicates()
        {
            var order = _db.Query(
                OrderByClause<Order>.OrderByAscending(r => r.OrderDate)
                    .ThenByAscending(r => r.ShippedDate),
                new Predicates { Skip = 10, Take = 20 }, 
                r => r.Employee.Id == 4 && r.ShippedDate.HasValue
            );
            Assert.IsNotNull(order);
            Assert.AreEqual(20, order.Count());
        
        }

        [TestMethod]
        public void CanQueryCustomerWithFilterExpressions()
        {
            var customers = _db.Query<Customer>(r => r.CompanyName.StartsWith("A"));
            Assert.IsNotNull(customers);
            Assert.AreEqual(4, customers.Count());
        }

        [TestMethod]
        public void CanQueryProductWithFilterExpressions()
        {
            var products = _db.Query<Product>(r => r.UnitPrice > 10f && r.ProductName != "Test product");
            Assert.IsNotNull(products);
            Assert.AreEqual(63, products.Count());

            var sortedProducts = _db.Query(OrderByClause<Product>.OrderByAscending(r => r.ProductName), r => r.ProductName != "Test product");
            Assert.AreEqual("Alice Mutton", sortedProducts.First().ProductName);

            var sortedProductsDesc = _db.Query(OrderByClause<Product>.OrderByDecending(r => r.ProductName), r => r.ProductName != "Test product");
            Assert.AreEqual("Alice Mutton", sortedProductsDesc.Last().ProductName);
        }

        [TestMethod]
        public void CanCreateAndSaveAnOrder()
        {
            using (var writer = new DbWriter(_db))
            {
                try
                {
                    var newOrder = new Order()
                    {
                        ShipName = "Test",
                        OrderDate = DateTime.Now
                    };

                    writer.Save(newOrder);

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
                    writer.Commit();
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
