using CoPilot.ORM.Common;
using CoPilot.ORM.Config;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Database;
using CoPilot.ORM.IntegrationTests.Models;

namespace CoPilot.ORM.IntegrationTests
{
    public class NorthwndConfig
    {
        private const string DefaultConnectionString = @"
                data source=localhost; 
                initial catalog=NORTHWND; 
                Integrated Security=true;
                MultipleActiveResultSets=True; 
                App=CoPilotIntegrationTest;";
        public static IDb Create(string connectionString = null)
        {
            return DbMapper.Create(connectionString ?? DefaultConnectionString);
        }

        public static IDb CreateFromConfig(string connectionString = null)
        {
            CoPilotGlobalResources.LoggingLevel = LoggingLevel.Verbose;

            var mapper = new DbMapper();

            mapper.SetPrefixColumnNamesWithTableName(false);

            var orderMap = mapper.Map<Order>("Orders", r => r.OrderId, "OrderID");
            orderMap.Column(r => r.OrderDate, "OrderDate");
            orderMap.Column(r => r.RequiredDate, "RequiredDate").IsNullable();
            orderMap.Column(r => r.ShippedDate, "ShippedDate").IsNullable();
            orderMap.Column(r => r.ShipName, "ShipName");
            var productMap = mapper.Map<Product>("Products", r => r.ProductId, "ProductID");
            productMap.Column(r => r.ProductName, "ProductName");
            productMap.Column(r => r.QuantityPerUnit, "QuantityPerUnit");
            productMap.Column(r => r.UnitPrice, "UnitPrice");

            var detailsMap = mapper.Map<OrderDetails>("Order Details");
            detailsMap.WithKey(r => r.ProductId, "ProductID").DefaultValue(null);
            detailsMap.WithKey(r => r.OrderId, "OrderID").DefaultValue(null);
            detailsMap.HasOne<Order>(r => r.OrderId, "OrderID").InverseKeyMember(r => r.OrderDetails);
            detailsMap.HasOne<Product>(r => r.ProductId, "ProductID").KeyForMember(r => r.Product);
            detailsMap.Column(r => r.UnitPrice, "UnitPrice");

            return mapper.CreateDb(connectionString ?? DefaultConnectionString);
        }
    }
}
