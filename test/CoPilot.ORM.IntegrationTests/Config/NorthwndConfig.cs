using CoPilot.ORM.Common;
using CoPilot.ORM.Config;
using CoPilot.ORM.Config.Naming;
using CoPilot.ORM.Database;
using CoPilot.ORM.IntegrationTests.Models.Northwind;

namespace CoPilot.ORM.IntegrationTests.Config
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

            var mapper = new DbMapper {DefaultAllowedOperations = OperationType.All};


            // Any properties that are not specifically mapped through 
            // configuration will be attempted to be auto-mapped. The naming
            // convention object allows you to control this behaviour when
            // the default all-caps, snake-cased and table-name-prefixed column
            // naming standard won't suit
            mapper.SetColumnNamingConvention(DbColumnNamingConvention.SameAsClassMemberNames);

            // Maps the Customer POCO to the Customer's table and then using the 
            // "AddKey"-method to specify that the property "ProductId" is representing 
            // the primary key (PK) and that it maps to the column name "ProductID".
            // The null-argument is passed to prevent CoPilot setting a default value 
            // as it assumes key columns are Identity-columns (auto-sequence)
            mapper.Map<Customer>("Customers").AddKey(r => r.CustomerId, "CustomerID", null).MaxSize(5);

            // Maps the Employee POCO to the Employees table and specifying 
            // that the property "Id" is the PK and that the corresponding 
            // column name is "EmployeeID". This is a short hand version that
            // can be used when the "AddKey"-method is not required for specifying
            // additional settings (in this case the PK is an Identity-column).
            mapper.Map<Employee>("Employees", r => r.Id, "EmployeeID");

            // Maps the Product POCO to the Products table with PK column name
            // ProductID
            mapper.Map<Product>("Products", r => r.ProductId, "ProductID");

            // Maps the Order POCO to the Orders table with PK column name
            // OrderID
            var orderMap = mapper.Map<Order>("Orders", r => r.OrderId, "OrderID");
            
            // Relating the order to the Employees table and specifying the 
            // navigation property to the Employee POCO as well as what the 
            // foreign key column is named. Note that we do not have a property
            // to hold the employee id in the Order POCO
            orderMap.HasOne(r => r.Employee, "EmployeeID");

            // Relating the order to the Customers table and also specifying
            // the foreign key column name as well as its data type, since it is
            // not an int as will be assumed by CoPilot when omitted
            orderMap.HasOne(r => r.Customer, "CustomerID");

            // Maps the OrderDetails POCO to the "Order Details"-table
            var detailsMap = mapper.Map<OrderDetails>("Order Details");

            // The order details table has a composite PK, so they need to be
            // both added using the "AddKey"-method. Again, the null-argument
            // passed is because these keys are not Identity-columns.
            detailsMap.AddKey(r => r.ProductId, "ProductID", null);
            detailsMap.AddKey(r => r.OrderId, "OrderID", null);

            // Relating the OrderDetails POCO to the Orders table. Here we do
            // have a property for the order id, and no navigation property that
            // points to an Order POCO. When present, relationships must be 
            // declared using the property mapped to the foreign key column. 
            // Navigation properties can be added (if present) by using the
            // "KeyForMember" and "InverseKeyMember" methods as seen here. 
            detailsMap.HasOne<Order>(r => r.OrderId, "OrderID").InverseKeyMember(r => r.OrderDetails);
            detailsMap.HasOne<Product>(r => r.ProductId, "ProductID").KeyForMember(r => r.Product);

            // Creates the IDb reference with the configurations applied
            return mapper.CreateDb(connectionString ?? DefaultConnectionString);
        }
    }
}
