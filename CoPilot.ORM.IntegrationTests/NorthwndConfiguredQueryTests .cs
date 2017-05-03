
using System.Collections.Generic;
using System.Linq;
using CoPilot.ORM.Common;
using CoPilot.ORM.Database;
using CoPilot.ORM.IntegrationTests.Models;
using CoPilot.ORM.Mapping.Mappers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoPilot.ORM.IntegrationTests
{
    [TestClass]
    public class NorthwndConfiguredQueryTests
    {
        private readonly IDb _db = NorthwndConfig.CreateFromConfig();
        
        //Composite primary key
        //Names with spaces
        //Column naming convention (tablename added by default)


        [TestMethod]
        public void CanQueryAllCustomers()
        {
            CoPilotGlobalResources.LoggingLevel = LoggingLevel.Verbose;
            var orders = _db.Query<Order>(null, "OrderDetails");
        }

        
    }
}
