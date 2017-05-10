
using System.Collections.Generic;
using System.Linq;
using CoPilot.ORM.Config.Naming;
using CoPilot.ORM.Database;
using CoPilot.ORM.IntegrationTests.Config;
using CoPilot.ORM.IntegrationTests.Models.Northwind;
using CoPilot.ORM.Mapping.Mappers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoPilot.ORM.IntegrationTests
{
    [TestClass]
    public class NorthwndTests
    {
        private readonly IDb _db = NorthwndConfig.Create();

        [TestMethod]
        public void CanQueryAllCustomers()
        {
            var response = _db.Query("select * from customers", null);
            Assert.AreEqual(91, response.RecordSets[0].Records.Length);
        }

        [TestMethod]
        public void CanQueryMultipleSets()
        {
            var response = _db.Query("select * from customers; select * from employees order by FirstName", null, "Customers","Employees");
            Assert.AreEqual("Customers", response.RecordSets[0].Name);
            Assert.AreEqual("Employees", response.RecordSets[1].Name);
            Assert.AreEqual(91, response.RecordSets[0].Records.Length);
            Assert.AreEqual(9, response.RecordSets[1].Records.Length);

            var firstNameIndex = response.RecordSets[1].GetIndex("FirstName"); //Get index of field name
            var names = response.RecordSets[1].Vector(firstNameIndex); //Get all values in set for this field
            Assert.IsTrue(names.Contains("Andrew"));
            Assert.IsTrue(names.Contains("Janet"));
            Assert.IsTrue(names.Contains("Margaret"));
            Assert.IsTrue(names.Contains("Steven"));

            var employeeDictionary = response.RecordSets[1].ToDictionary(); //Dictionary with field names as keys and value containing the field's vector
            Assert.IsTrue(employeeDictionary.ContainsKey("FirstName"));
            Assert.IsTrue(names.All(r => employeeDictionary["FirstName"].Contains(r)));

            var singleEmployeeDictionary = response.RecordSets[1].ToDictionary(1); //Dictionary with field names as keys and corresponding value for a single record
            Assert.AreEqual("Dodsworth", singleEmployeeDictionary["LastName"]);

        }

        [TestMethod]
        public void CanQueryAllCustomersAndMapToDynamicObject()
        {
            var response = _db.Query<dynamic>("select * from employees order by LastName", null);
            Assert.AreEqual(9, response.Count());        
        }

        [TestMethod]
        public void CanQuerySingleCustomerAndMapToDynamicObject()
        {
            var response = _db.Query<dynamic>("select * from employees where EmployeeID=@id order by LastName", new { id = 5 }, DynamicMapper.Create(convertToCamelCase:false)).Single();
            Assert.AreEqual(5, response.EmployeeID);
            Assert.AreEqual("Steven", response.FirstName);
        }

        [TestMethod]
        public void CanExecuteStoredProcedureAndMapToDynamicObject()
        {
            var response = _db.Query<dynamic>("[Ten Most Expensive Products]", null, DynamicMapper.Create(new SnakeOrKebabCaseConverter()));

        }

        [TestMethod]
        public void CanQuerySingleCustomerAndMapToPocoObject()
        {
            var response = _db.Query<Employee>("select * from employees where EmployeeID=@id order by LastName", new { id = 5 }, 
                BasicMapper.Create(typeof(Employee), new Dictionary<string, string> { { "EmployeeID", "Id" } })).Single();
            Assert.AreEqual(5, response.Id);
            Assert.AreEqual("Steven", response.FirstName);
        }

        [TestMethod]
        public void CanQuerySingleColumnAndMapToSimpleType()
        {
            var response = _db.Query<string>("select ProductName from products where ProductName <> @testName order by 1", new {testName= "Test product" }).ToArray();
            Assert.AreEqual(77, response.Length);
            Assert.AreEqual("Alice Mutton", response[0]);
        }

        [TestMethod]
        public void CanExecuteSimpleCommandAndScalar()
        {
            var employeeName = _db.Query<string>("select FirstName from employees where  EmployeeID=@id", new {id = 1}).Single();
            // Update command
            var rows = _db.Command("update employees set FirstName=@newName where EmployeeID=@id", new {id = 1, newName = "Kjerand"});
            Assert.AreEqual(1, rows);

            var updatedEmployeeName = _db.Query<string>("select FirstName from employees where  EmployeeID=@id", new { id = 1 }).Single();
            Assert.AreEqual("Kjerand", updatedEmployeeName);

            _db.Command("update employees set FirstName=@newName where EmployeeID=@id", new { id = 1, newName = employeeName });

            // Scalar
            var regions = _db.Scalar<int>("select count(*) from region");
            Assert.AreEqual(4, regions);
        }
    }
}
