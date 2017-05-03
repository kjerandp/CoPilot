using CoPilot.ORM.Config;
using CoPilot.ORM.Database;
using CoPilot.ORM.IntegrationTests.Models;

namespace CoPilot.ORM.IntegrationTests
{
    public class NorthwndConfig
    {
        public static IDb Create(string connectionString = null)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = @"
                data source=localhost; 
                initial catalog=NORTHWND; 
                Integrated Security=true;
                MultipleActiveResultSets=True; 
                App=CoPilotIntegrationTest;";
            }
            return DbMapper.Create(connectionString);
        }

        public static IDb CreateFromConfig(string connectionString = null)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = @"
                data source=localhost; 
                initial catalog=NORTHWND; 
                Integrated Security=true;
                MultipleActiveResultSets=True; 
                App=CoPilotIntegrationTest;";
            }

            var mapper = new DbMapper();

            var orderMap = mapper.Map<Order>("Orders", r => r.OrderId, "OrderID");
            orderMap.Column(r => r.OrderDate, "OrderDate");
            orderMap.Column(r => r.RequiredDate, "RequiredDate");
            orderMap.Column(r => r.ShippedDate, "ShippedDate");
            orderMap.Column(r => r.ShipName, "ShipName");
            var detailsMap = mapper.Map<OrderDetails>("Order Details", r => r.ProductId);
            detailsMap.WithKey(r => r.OrderId);
            orderMap.HasMany(r => r.OrderDetails, "OrderID");

            

            return mapper.CreateDb(connectionString);
        }
    }
}
