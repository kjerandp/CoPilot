# CoPilot
CoPilot is an object relational mapper (ORM) with a specific purpose. If you are looking for a full featured enterprise level ORM then CoPilot is not what you are looking for. CoPilot is designed to assist you when  implementing the data access layer - simplifying the common tasks.

Think of it more in the sense of a micro ORM, only with a set of helpful  features added on top of it. 

**Key features:**  
* Map POCO models to tables including relationships (one-to-many and many-to-one relationships). 
* Author and execute SQL statements for CRUD operations. Related entities can be included for all operations, as long as they have a singular key defined. 
* Mapping of data from queries or stored procedures to dynamic objects or POCO classes
* Perform mulitple write operations as a unit-of-work (database transaction)
* Generate scripts to build database from model

### Important Note
This is still an early version and only works with MSSQL - use at your own risk!

## Usage
Most of the examples documented here can be found in the integration test project that is part of the source code of CoPilot.

### Basic usage
First part of these examples will work against the Northwnd database, which I have restored in my SqlExpress instance after downloading it from <https://northwinddatabase.codeplex.com/>

#### Connecting to the database
CoPilot works on an interface named `IDb`. So to start using CoPilot, we need to obtain a reference to an implementation of that interface. I have created a simple helper class here that uses a helper class in CoPilot called `DbMapper`. We will use this class later when we map our POCO models to database tables, but for now we will just have it create the simplest possible representation of the database, by handing it the connection string.

```
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
    }
```

#### Executing basic queries
For the first test, let's try to get some data. I have create a simple unit test in order to query for customers. Using the `Query` method, I can write regular sql and pass it an object with any arguments. In this case I have none, so I'm passing NULL.  


```
    [TestClass]
    public class NorthwndBasicQueryTests
    {
        private readonly IDb _db = NorthwndConfig.Create();

        [TestMethod]
        public void CanQueryAllCustomers()
        {
            var response = _db.Query("select * from customers", null);

            Assert.AreEqual(91, response.RecordSets[0].Records.Length);
        }
    }
```

The response will be an instance of the `DbResponse` class. This class holds basic information about the resultset.

```
    public struct DbResponse
    {
        internal DbResponse(DbRecordSet[] results, long elapsedMs)
        {
            RecordSets = results;
            ElapsedMs = elapsedMs;
        }

        public long ElapsedMs { get; }
        public DbRecordSet[] RecordSets { get; }
    }

    public struct DbRecordSet
    {
        (...)

        public string Name { get; set; }
        public string[] FieldNames { get; internal set; }
        public Type[] FieldTypes { get; internal set; }
        public object[][] Records { get; internal set; }

        (...)

    }
```
This is the internal representation CoPilot uses and we'll look at some better options for handling responses shortly. But first, you can execute multiple queries and name each resultset like this: * 

```
    _db.Query("select * from customers; select * from employees", null, "Customers","Employees");
```
This will add a `DbRecordSet` for each query and name them according to the names provided int the `params string[] names` parameter. In this case, "Customers" and "Employees". Each record set is then containing a list of field names (database column names), types and the records as a two dimensional array (indexed by row and then field).

This object is important if you want to create your own mapping delegate. If you want to see some more usage of this class, look into the Northwnd basic query tests.

#### Executing basic commands
We can do commands and scalars by calling the `Command` and `Scalar` methods respectively. Here are two basic examples:

```
        
// Update command
var rows = _db.Command("update employees set FirstName=@newName where EmployeeID=@id", new {id = 1, newName = "Kjerand"});

// Scalar
var regions = _db.Scalar<int>("select count(*) from region");
        
```

### Mapping
In order to map data, from the database to CLR objects, CoPilot is using a mapping delegate. There are three available mappers buildt into CoPilot:
* `DynamicMapper` - for mapping data to a dynamic object
* `BasicMapper` - for mapping data to POCO classes by doing a best-effort matching betweeb fieldnames and property names. Can be assisted by passing in a dictionary of column-to-property mappings. 
* `ContextMapper` - for mapping data to POCO classes that have been mapped with the `DbMapper`. Supports multiple resultsets with relational entities.  

#### Dynamic mapping
We have not created any POCO models yet, so in this example the query will be mapped to a dynamic object, using the `DynamicMapper`.

``` 
var response = _db.Query<dynamic>("select * from employees where EmployeeID=@id order by LastName", new { id = 5 }).Single();
```
There are a few new things to note here. First, we are mapping to a specific type, given by the generic argument. The second thing to note is the parameter binding in the query. You specify parameters in a query with the @[name] syntax and pass arguments for the named parameters by providing an anonymous object with a properties matching the names excluding the alpha-sign.

When mapping database fields to properties on the dynamic object the mapper will by default convert field names to camel case. This means that the column name `EmployeeID` will be mapped to a property named `EmployeeId`.  

We can prevent this by changing the bahaviour of the mapper:

``` 
var response = _db.Query<dynamic>(
    "select * from employees where EmployeeID=@id order byLastName", 
    new { id = 5 }, 
    DynamicMapper.Create(convertToCamelCase:false))
.Single();
```
We can also specify what letter case converter to use. If you want uppercase snake-case names on the properties you can do this:

```
var response = _db.Query<dynamic>(
    "select * from employees where EmployeeID=@id order byLastName", 
    new { id = 5 }, 
    DynamicMapper.Create(new SnakeOrKebabCaseConverter(r => r.ToUpper()))
.Single();
```
#### Basic mapping
Now let's go one step further and create a POCO class for an employee. I deliberately have not included a property for all columns as it is not required - it will map what it is able to match.

```
    public class Employee
    {
        public int EmployeeId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Title { get; set; }
        public string TitleOfCourtesy { get; set; }
        public DateTime BirthDate { get; set; }
        public DateTime HireDate { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
    }
```

Let's update our test to map to this new class instead of the dynamic.
``` 
var emp = _db.Query<Employee>("select * from employees where EmployeeID=@id order by LastName", new { id = 5 }).Single();
```

This time the basic mapper is used, as we are not mapping to a dynamic object. By default, it will try and map a column name directly to property names ignoring case. You can however provide a dictionary of column-to-property name mapping. Let's assume we renamed the `EmployeeId` property in our POCO to just `Id`. We could then provide a mapping dictionary to help the mapper:
``` 
var columnMapping = new Dictionary<string, string> { { "EmployeeID", "Id" } };

var emp = _db.Query<Employee>("select * from employees where EmployeeID=@id order by LastName", 
    new { id = 5 }, 
    BasicMapper.Create(typeof(Employee), columnMapping)
).Single();
```

The basic mapper can also be used to map a single column to a basic system type. Notice the type specified in the generic argument.

```
var productNames = _db.Query<string>("select ProductName from products order by 1", null).ToArray();
```

#### Contextual mapping
To get the most out of the features available in CoPilot, we need to provide it with some configurations that describes how your POCO classes relates to the database model. We create this configuration using the `DbMapper`.

Let's say we want all orders, with its order details, customer and employee information. First step would be to create a corresponding POCO class for these entites and then configure them using the `DbMapper`.

We can then retrieve all orders, with all related data mapped by specifying what to include in the arguments passed to the query method. Includes are specified as an array (or params) of strings and represents paths, based on your POCO classes property names, to which relations to include.
```
var orders = _db.Query<Order>(null, "OrderDetails.Product", "Employee", "Customer");
```
The first NULL-argument passed is to specify no filter. The other arguments are the paths that we want to include. The above instructs that we want to retrieve the orders with the matching order details along with its belonging product, and then also the order's related customer and employee records.

In order to bring in related entities, the POCO class being used for querying needs to have a property referencing the corresponding entity's POCO class and have its relationships explicitly declared in the config:

```
public static IDb CreateFromConfig(string connectionString = null)
{
    var mapper = new DbMapper();
    
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
    orderMap.HasOne(r => r.Customer, "CustomerID", DbDataType.String);

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
``` 

(To be continued)
