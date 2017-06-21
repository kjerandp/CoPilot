# CoPilot
CoPilot is an object relational mapper (ORM). In its core it works like a micro ORM, but it has some neat and powerful features added on top of it. 

Please note that this version will soon be depricated and replaced by a new version (see [master branch](https://github.com/kjerandp/CoPilot/tree/master)) and this version will contain some breaking changes.

**Key features:**  
* Map POCO models to tables including relationships (one-to-many and many-to-one relationships). 
* Author and execute SQL statements for CRUD operations. Related entities can be included for all operations, as long as they have a singular key defined. 
* Mapping of data from queries or stored procedures to dynamic objects or POCO classes
* Solves the "1+N"-problem by fetching child records in a single query and then merging the data with the parent records when mapped
* Support for limiting queries to only include specified columns when all you need is a subset of data from one or more tables. This will allow the database server to tweak its execution plans to reduce unnecessary table access. This is particularly useful if you only need data from columns that are indexed. 
* Perform mulitple write operations, including bulk commands, as a unit-of-work (database transaction)
* Transform values from and to the database by associating a `ValueAdapter` to relevant POCO properties. Examples of use cases is to serialize/deserialize to and from json, joining/splitting collections of primitive values, converting from/to enums etc.
* Use lookup tables - meaning that the value of a property can be used to lookup a key value in another table, and then pass that value to the mapped table and then do the same in reverse. Can be handy if you for instance want to use enums in your POCOs, but you want to enforce a foreign key constraint to another table for the mapped column.
* Generate scripts to build database from model configurations
* Validate configuration against database schema

## How to use
CoPilot aims to be as simple and intuitive as possible to use and most features are available from a single interface called `IDb`. Here's an illustration of a typical CoPilot statement:

![Anatomy of a CoPilot statement](https://raw.githubusercontent.com/kjerandp/files/master/images/anatomy_copilot_statement.png)

The `IDb` interface has the following key operations, with various overloads available:
* Query - for selection and returning multiple records
* Single - for selecting a single record
* Save - for inserting or updating a single record or a collection of records
* Delete - for deleting a single record or a collection of records
* Patch - to partially update a single record
* Command - for issueing a non-query SQL statement
* Scalar - for executing a SQL statement that returns a single value

Querying can be done by either writing the SQL statement (or name a stored procedure) and using anonymous objects for parameter binding, or by using a mapped POCO class to have CoPilot author the SQL for you, as in the example illustrated above. Using the available overloads for the query methods will allow you to do more, like specify an ordering clause, predicates and selecting into dto models or simple CLR types etc. 

Most operations supports working with a _context_, which in essence make up a base POCO class that is mapped to a specific table and the  related entities that is specified in the include-argument.  

Some basic examples can be found in the following sections, but many more examples can (and will in time) be found in the integration tests project. Also, a Wiki is planned for proper documentation.  

### Basic usage
These examples will work against the Northwind database, which I have restored in my SqlExpress instance after downloading it from <https://northwinddatabase.codeplex.com/>. It is recommended to check out the [Band Sample Tests](https://github.com/kjerandp/CoPilot/blob/master/test/CoPilot.ORM.IntegrationTests/BandSampleTests.cs) for some more and better (greenfield) examples.

#### Connecting to the database
I have created a simple helper class here that uses the `DbMapper` class to obtain an instance of the `IDb` interface that CoPilot works with. We will use this class later when we map our POCO models to database tables, but for now we will just have it create the simplest possible representation of the database, by handing it the connection string.

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
Let's try to get some data. I have created a simple unit test to query for customers. Using the `Query` method, I can write regular sql and pass it an object with any arguments. In this case I have none, so I'm passing NULL.  


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
        (...)

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
This is the internal representation CoPilot uses and we'll look at some better options for handling responses shortly. But first, you can execute multiple queries and name each resultset like this:  

```
    _db.Query("select * from customers; select * from employees", null, "Customers","Employees");
```
This will add a `DbRecordSet` for each query and name them according to the names provided int the `params string[] names` parameter. In this case, "Customers" and "Employees". Each record set is then containing a list of field names (database column names), types and the records as a two dimensional array (indexed by row and then field).

This object is important if you want to create your own mapping delegate. If you want to see some more usage of this class, look into the Northwind basic query tests.

#### Executing basic commands
We can do commands and scalars by calling the `Command` and `Scalar` methods respectively. Here are two basic examples:

```
        
// Update command
var rows = _db.Command("update employees set FirstName=@newName where EmployeeID=@id", new {id = 1, newName = "Kjerand"});

// Scalar
var regions = _db.Scalar<int>("select count(*) from region");
        
```
Parameter binding is acheived by specifying parameters in the sql statement with the @[name] syntax and pass arguments for the named parameters by providing an anonymous object with a properties matching the names excluding the alpha-sign.

### Mapping
In order to map data from the database to CLR objects, CoPilot is using a mapping delegate. There are three available mappers buildt into CoPilot:
* `DynamicMapper` - for mapping data to a dynamic object
* `BasicMapper` - for mapping data to POCO classes by doing a best-effort matching between fieldnames and property names. Can be assisted by passing in a dictionary of column-to-property mappings. 
* `ContextMapper` - for mapping data to POCO classes that have been mapped with the `DbMapper`. Supports multiple resultsets with relational entities.  

#### Dynamic mapping
We have not created any POCO models yet, so in this example the query will be mapped to a dynamic object, using the `DynamicMapper`.

``` 
var response = _db.Query<dynamic>("select * from employees where EmployeeID=@id order by LastName", new { id = 5 }).Single();
```
Note that we are mapping to a specific type, given by the generic argument. 

When mapping database fields to properties on the dynamic object the mapper will by default convert field names to camel case. This means that the column name `EmployeeID` will be mapped to a property named `EmployeeId`.  

We can prevent this by changing the bahaviour of the mapper:

``` 
var response = _db.Query<dynamic>(
    "select * from employees where EmployeeID=@id order by LastName", 
    new { id = 5 }, 
    DynamicMapper.Create(convertToCamelCase:false))
.Single();
```
We can also specify what letter case converter to use. If you want uppercase snake-case names on the properties you can do this:

```
var response = _db.Query<dynamic>(
    "select * from employees where EmployeeID=@id order by LastName", 
    new { id = 5 }, 
    DynamicMapper.Create(new SnakeOrKebabCaseConverter(r => r.ToUpper()))
.Single();
```
#### Basic mapping
Now let's go one step further and create a POCO class for an employee. I deliberately have not included a property for all columns and it is not required - it will map what it is able to match.

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
var emp = _db.Query<Employee>("select * from employees where EmployeeID=@id order by LastName", new { id = 5 }).SingleOrDefault();
```

This time the basic mapper is used, as we are not mapping to a dynamic object. By default, it will try and map a column name directly to property names ignoring case. You can however provide a dictionary of column-to-property name mapping. Let's assume we renamed the `EmployeeId` property in our POCO to just `Id`. We could then provide a mapping dictionary to help the mapper:
``` 
var columnMapping = new Dictionary<string, string> { { "EmployeeID", "Id" } };

var emp = _db.Query<Employee>("select * from employees where EmployeeID=@id order by LastName", 
    new { id = 5 }, 
    BasicMapper.Create(typeof(Employee), columnMapping)
).SingleOrDefault();
```

The basic mapper can also be used to map a single column to a basic system type. Notice the type specified in the generic argument.

```
var productNames = _db.Query<string>("select ProductName from products order by 1", null);
```

#### Contextual mapping
To get the most out of the features available in CoPilot, we need to provide it with some configurations that describes how your POCO classes relates to the database model. We create this configuration using the `DbMapper`.

Let's say we want all orders, with its order details, customer and employee information. First step would be to create a corresponding POCO class for these entites and then configure them using the `DbMapper`.
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
    // We are also setting a max size according to the column constraint in the db.
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
We can then retrieve all orders, with all related data mapped by specifying what to include in the arguments passed to the query method. Includes are specified as an array (or params) of strings and represents paths, based on your POCO classes property names, to which relations to include.
```
var orders = _db.Query<Order>(null, "OrderDetails.Product", "Employee", "Customer");
```
The first NULL-argument passed is to specify no filter. The other arguments are the _paths_ of what related entities we want to include. Use the dot notation when you want to include multiple levels of related entities, like the `Product` reference of the `OrderDetails`-relation in the above example.

In the following example I've applied ordering, predicates and filters. Predicates in CoPilot is a way to specify functions like `DISTINCT`, `TOP`, `SKIP` and `TAKE`. 
```
var orders = _db.Query<Order>(
    OrderByClause<Order>.OrderByAscending(r => r.OrderDate)
        .ThenByAscending(r => r.ShippedDate),
    new Predicates { Skip = 10, Take = 20 }, 
    r => r.Employee.Id == 4 && r.ShippedDate.HasValue,
    "OrderDetails.Product", "Employee", "Customer"
);
```
Here's another example based from the `Product` mapping.
```
var products = _db.Query<Product>(r => r.UnitPrice > 10f && r.ProductName != "Test product");
``` 
### Write operations
The `IDb` interface have methods for saving, patching and deleting entitities. Use these if you just want to execute a single command. Otherwise it is recommended that you use the `DbWriter` class, as it will allow you to perform multiple commands on one or more entities as a unit-of-work (database transaction). It supports all the write operations from the `IDb` interface.
```
// New up an instance of the DbWriter with a using statement. It takes the
// IDb instance as an argument.
using(var writer = new DbWriter(_db)){
    try {
        // do write operations and then commit
        (...)
        writer.Commit();
    } catch (Exception ex){
        // rollback the transaction 
        writer.Rollback();
        // handle exception
    }
}
```
The `include` parameter can also be used when saving and deleting entities, but only if the mapped tables involved has a singular primary key and an Identity column (auto sequence) defined. This can be useful if you want to insert/update/delete an entity along with its related entities. 

```
// insert a new order record and a new employee record
var newOrder = new Order {
    (...)
    Employee = new Employee {
        (...)
    }
};
_db.Save<Order>(newOrder, "Employee");
``` 
This would not work with the `Customer` relationship, as the `Customers` table does not use an `Identity` column. It will not work with the `OrderDetails` relation either, as that table has a composite primary key. You could on the other hand save the order details along with its products:
```
using (var writer = new DbWriter(_db))
{
    try
    {
        var customer = new Customer
        {
            CustomerId = "ACMEC",
            CompanyName = "Acme"
            (...)
        };
        writer.Insert(customer);

        var newOrder = new Order()
        {
            (...)
            Customer = customer,
            Employee = new Employee
            {
                (...)
            }
        };
        
        // This will first insert the employee record and then then the
        // new order. The `Id` property of `Employee` and the  `OrderId` 
        // property of `newOrder` will be populated with the primary key 
        // value generated by the database. 
        writer.Save(newOrder, "Employee");

        var newOrderDetails = new List<OrderDetails>
        {
            new OrderDetails
            {
                Product = new Product
                {
                    (...)
                },
                (...)
                OrderId = newOrder.OrderId
            }
        };
        
        // Then, the new product will be inserted and finally the two new 
        // order details records with the proper foreign key relationships 
        // (to the order, the new product and the existing product).
        writer.Insert<OrderDetails>(newOrderDetails, "Product");
        
        // Setting the order details reference on the order POCO
        newOrder.OrderDetails = newOrderDetails;
        
        // explicit update example (as save is only supported with single PK)
        var orderDetail = newOrderDetails[0];
        orderDetail.Discount = 0.3f;
        writer.Update(orderDetail);

        // example for deleting all new records created
        writer.Delete<OrderDetails>(newOrderDetails, "Product");
        writer.Delete(newOrder, "Employee", "Customer");
        
        writer.Commit();
    }
    catch (Exception ex)
    {
        writer.Rollback();
        // Error handling
    }
} 
```
