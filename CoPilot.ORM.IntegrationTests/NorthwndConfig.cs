using CoPilot.ORM.Common;
using CoPilot.ORM.Config;
using CoPilot.ORM.Config.Naming;
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
            //CoPilotGlobalResources.LoggingLevel = LoggingLevel.Verbose;

            var mapper = new DbMapper();

            mapper.SetColumnNamingConvention(DbColumnNamingConvention.SameAsClassMemberNames);

            mapper.Map<Order>("Orders", r => r.OrderId, "OrderID");
            mapper.Map<Product>("Products", r => r.ProductId, "ProductID");

            var detailsMap = mapper.Map<OrderDetails>("Order Details");
            detailsMap.AddKey(r => r.ProductId, "ProductID").DefaultValue(null);
            detailsMap.AddKey(r => r.OrderId, "OrderID").DefaultValue(null);
            detailsMap.HasOne<Order>(r => r.OrderId, "OrderID").InverseKeyMember(r => r.OrderDetails);
            detailsMap.HasOne<Product>(r => r.ProductId, "ProductID").KeyForMember(r => r.Product);

            return mapper.CreateDb(connectionString ?? DefaultConnectionString);
        }
    }
}
