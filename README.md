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

#### Basic mapping
The `DbResponse` class is obviously not very convinient class to work with, so lets try to map the data to an object. We have not created any POCO models yet, so let's start by mapping a query to a dynamic object.

``` 
    [TestMethod]
    public void CanQuerySingleCustomerAndMapToDynamicObject()
    {
        var response = _db.Query<dynamic>("select * from employees where EmployeeID=@id order by LastName", new { id = 5 }).Single();
        Assert.AreEqual(5, response.EmployeeID);
    }
```
There are a few new things to note here. First, we are mapping to a specific type, given by the generic argument. The second thing to note is the parameter binding in the query. You specify parameters in a query with the @[name] syntax and pass arguments for the named parameters by providing an anonymous object with a properties matching the names excluding the alpha-sign.

By the way, the test above will fail. We are testing for a property called `EmployeeID`, which matches the column name in the Employee-table. However, the default mapper used will try to convert the column names by converting from snake case to camel case. Example, if the column was named `EMPLOYEE_ID`, then the property would be named `EmployeeId`. It will take the first letter in each part (seperated by underscore) and make it uppercase while the rest of the letters will be turned into lowercase. 

We can either change the way we access the property to use `Employeeid` or we can tell the mapper to not use its default behaviour like this:

``` 
    [TestMethod]
    public void CanQuerySingleCustomerAndMapToDynamicObject()
    {
        var response = _db.Query<dynamic>("select * from employees where EmployeeID=@id order by LastName", 
            new { id = 5 }, DynamicMapper.Create(convertToCamelCase:false)).Single();
        Assert.AreEqual(5, response.EmployeeID);
    }
```

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
    [TestMethod]
    public void CanQuerySingleCustomerAndMapToPocoObject()
    {
        var response = _db.Query<Employee>("select * from employees where EmployeeID=@id order by LastName", new { id = 5 }).Single();
        Assert.AreEqual(5, response.EmployeeId);
        Assert.AreEqual("Steven", response.FirstName);
    }
```

This time it is not the same mapper as in the previous test, as we are not mapping to a dynamic object. When we map to a class that is not configured using the `DbMapper` the default mapper being used is the `BasicMapper`. It will try and map a column name directly to property names ignoring case. You can however provide a dictionary of column-to-property name mapping. Let's rename the `EmployeeId` to just `Id` and try this with the following command:
``` 
    [TestMethod]
    public void CanQuerySingleCustomerAndMapToPocoObject()
    {
        var response = _db.Query<Employee>("select * from employees where EmployeeID=@id order by LastName", new { id = 5 }, 
            BasicMapper.Create(typeof(Employee), new Dictionary<string, string> { { "EmployeeID", "Id" } })).Single();
        Assert.AreEqual(5, response.Id);
        Assert.AreEqual("Steven", response.FirstName);
    }
```

Final example on basic mapping is to map a single column to a simple CLR type. Notice the type specified in the generic argument.

```
        [TestMethod]
        public void CanQuerySingleColumnAndMapToSimpleType()
        {
            var response = _db.Query<string>("select ProductName from products order by 1", null).ToArray();
            Assert.AreEqual(77, response.Length);
            Assert.AreEqual("Alice Mutton", response[0]);
        }
```

#### Executing basic commands
We can do commands and scalars by calling the `Command` and `Scalar` methods respectively. Here are two basic examples:

```
        [TestMethod]
        public void CanExecuteSimpleCommandAndScalar()
        {
            // Update command
            var rows = _db.Command("update employees set FirstName=@newName where EmployeeID=@id", new {id = 1, newName = "Kjerand"});
            Assert.AreEqual(1, rows);

            // Scalar
            var regions = _db.Scalar<int>("select count(*) from region");
            Assert.AreEqual(4, regions);
        }
```

## Mapping models and relationships
Low level usage of CoPilot is nice as a fallback method. What sets it apart from the the typical micro ORM though, is its abillity to write queries and other statements for you, based on the configuration you give it. Let's make a few more POCO classes and create a model configuration to see what we then can do.


(To be continued)
