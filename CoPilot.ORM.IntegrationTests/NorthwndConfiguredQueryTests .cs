
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CoPilot.ORM.Common;
using CoPilot.ORM.Database;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.IntegrationTests.Models;
using CoPilot.ORM.Mapping.Mappers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoPilot.ORM.IntegrationTests
{
    [TestClass]
    public class NorthwndConfiguredQueryTests
    {
        private readonly IDb _db = NorthwndConfig.CreateFromConfig();

        [TestMethod]
        public void CanQueryAllCustomers()
        {
            var orders = _db.Query<Order>(null, "OrderDetails.Product");
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
