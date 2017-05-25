using System.Collections.Generic;
using System.Linq;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Mapping.Mappers;
using CoPilot.ORM.Model;
using CoPilot.ORM.Tests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoPilot.ORM.Tests
{
    [TestClass]
    public class DbMapperTests
    {
        private DbModel _model;

        [TestInitialize]
        public void Init()
        {
            _model = TestModel.GetModel();
        }

        [TestMethod]
        public void CanMapValuesFromRecordToModel()
        {
            var city = new City()
            {
                Id = 2,
                Name = "City",
                CityCode = "1002"
            };

            var org = new Organization()
            {
                Id = 1,
                Name = "Test org",
                CountryCode = "NO",
                HostNames = new List<string> { "localhost", "city.domain.com" },
                City = city,
                CityId = 2,
                CityCode = "1002"
            };
            var orgRecord = new Dictionary<string, object>
            {
                {"GOT_ORGANIZATION_ID", org.Id},
                {"GOT_ORGANIZATION_NAME", org.Name},
                {"PUB_CITY_ID", org.CityId},
                {"GOT_ORGANIZATION_CITY_CODE", org.CityCode},
                {"GOT_ORGANIZATION_COUNTRY_CODE", org.CountryCode},
                {"GOT_ORGANIZATION_HOST_NAMES", string.Join(";", org.HostNames)},
                {"GOT_TYPE_ID" , "TypeB"}
            };

            var ctx = _model.CreateContext<Organization>();
            var mapper = ContextMapper.Create(ctx);
            var dataset = new DbRecordSet(orgRecord);
            var org2 = mapper.Invoke(dataset).Select(r => r.Instance).OfType<Organization>().Single();

            Assert.AreEqual(org.Id, org2.Id);
            Assert.AreEqual(org.Name, org2.Name);
            Assert.AreEqual(org.CityId, org2.CityId);
            Assert.AreEqual(org.CountryCode, org2.CountryCode);
            Assert.AreEqual(org.Id, org2.Id);
            Assert.AreEqual(org.HostNames.Count, org2.HostNames.Count);
            Assert.AreEqual(OrganizationType.TypeB, org2.OrganizationType);
        }

        [TestMethod]
        public void CanMapValuesFromRecordToModelWithDottedValues()
        {
            var city = new City()
            {
                Id = 1,
                Name = "City",
                CityCode = "1002"
            };

            var city2 = new City()
            {
                Id = 2,
                Name = "City2",
                CityCode = "1003"
            };

            var owner = new Organization()
            {
                Id = 1,
                Name = "Test org",
                CountryCode = "NO",
                HostNames = new List<string> { "localhost", "city.domain.com" },
                City = city,
                CityId = 1,
                CityCode = "1002",
                OrganizationType = OrganizationType.TypeC
            };
            var user = new Organization()
            {
                Id = 2,
                Name = "User org",
                CountryCode = "NO",
                HostNames = new List<string> { "city2.domain.com" },
                City = city2,
                CityId = 2,
                CityCode = "1003",
                OrganizationType = OrganizationType.TypeA
            };

            var resource = new Resource()
            {
                Id = 1,
                Name = "Sample resource",
                Owner = owner,
                UsedBy = user
            };
            var resRecord = new Dictionary<string, object>
            {
                {"TST_RESOURCE_ID", resource.Id},
                {"TST_RESOURCE_NAME", resource.Name},
                {"Owner.GOT_ORGANIZATION_ID", owner.Id},
                {"Owner.GOT_ORGANIZATION_NAME", owner.Name},
                {"Owner.PUB_CITY_ID", owner.CityId},
                {"Owner.GOT_ORGANIZATION_CITY_CODE", owner.CityCode},
                {"Owner.GOT_ORGANIZATION_COUNTRY_CODE", owner.CountryCode},
                {"Owner.GOT_ORGANIZATION_HOST_NAMES", string.Join(";", owner.HostNames)},
                {"Owner.GOT_TYPE_ID", "TypeC"},
                {"Owner.City.PUB_CITY_ID", city.Id},
                {"Owner.City.PUB_CITY_CODE", city.CityCode},
                {"Owner.City.PUB_CITY_NAME", city.Name},
                {"UsedBy.GOT_ORGANIZATION_ID", user.Id},
                {"UsedBy.GOT_ORGANIZATION_NAME", user.Name},
                {"UsedBy.PUB_CITY_ID", user.CityId},
                {"UsedBy.GOT_ORGANIZATION_CITY_CODE", user.CityCode},
                {"UsedBy.GOT_ORGANIZATION_COUNTRY_CODE", user.CountryCode},
                {"UsedBy.GOT_ORGANIZATION_HOST_NAMES", string.Join(";", user.HostNames)},
                {"UsedBy.GOT_TYPE_ID", "TypeA"},
                {"UsedBy.City.PUB_CITY_ID", city2.Id},
                {"UsedBy.City.PUB_CITY_CODE", city2.CityCode},
                {"UsedBy.City.PUB_CITY_NAME", city2.Name}
            };
            var ctx = _model.CreateContext<Resource>("Owner.City","UsedBy.City");
            var mapper = ContextMapper.Create(ctx);
            var dataset = new DbRecordSet(resRecord);
            var mappedResource = mapper.Invoke(dataset).Select(r => r.Instance).OfType<Resource>().Single();

            Assert.AreEqual(resource.Id, mappedResource.Id);
            Assert.AreEqual(resource.Name, mappedResource.Name);
            Assert.IsNotNull(mappedResource.Owner);
            Assert.AreEqual(resource.Owner.Id, mappedResource.Owner.Id);
            Assert.AreEqual(resource.Owner.Name, mappedResource.Owner.Name);
            Assert.AreEqual(resource.Owner.CityId, mappedResource.Owner.CityId);
            Assert.AreEqual(resource.Owner.CountryCode, mappedResource.Owner.CountryCode);
            Assert.AreEqual(resource.Owner.HostNames.Count, mappedResource.Owner.HostNames.Count);
            Assert.AreEqual(resource.Owner.OrganizationType, mappedResource.Owner.OrganizationType);
            Assert.IsNotNull(mappedResource.Owner.City);
            Assert.AreEqual(resource.Owner.City.Id, mappedResource.Owner.City.Id);
            Assert.AreEqual(resource.Owner.City.Name, mappedResource.Owner.City.Name);
            Assert.AreEqual(resource.Owner.City.CityCode, resource.Owner.City.CityCode);
            Assert.IsNotNull(mappedResource.UsedBy);
            Assert.AreEqual(resource.UsedBy.Id, mappedResource.UsedBy.Id);
            Assert.AreEqual(resource.UsedBy.Name, mappedResource.UsedBy.Name);
            Assert.AreEqual(resource.UsedBy.CityId, mappedResource.UsedBy.CityId);
            Assert.AreEqual(resource.UsedBy.CountryCode, mappedResource.UsedBy.CountryCode);
            Assert.AreEqual(resource.UsedBy.HostNames.Count, mappedResource.UsedBy.HostNames.Count);
            Assert.AreEqual(resource.UsedBy.OrganizationType, mappedResource.UsedBy.OrganizationType);
            Assert.IsNotNull(mappedResource.UsedBy.City);
            Assert.AreEqual(resource.UsedBy.City.Id, mappedResource.UsedBy.City.Id);
            Assert.AreEqual(resource.UsedBy.City.Name, mappedResource.UsedBy.City.Name);
            Assert.AreEqual(resource.UsedBy.City.CityCode, resource.UsedBy.City.CityCode);
        }


        [TestMethod]
        public void CanGetRelationshipsForEntityType()
        {
            var table = _model.GetTableMap<Organization>().Table;
            var fkRelationships = table.Relationships;
            Assert.AreEqual(2, fkRelationships.Length);
            Assert.AreEqual(1, fkRelationships.Count(r => r.IsLookupRelationship));
            var pkRelationships = table.InverseRelationships;
            Assert.AreEqual(2, pkRelationships.Length);

            table = _model.GetTableMap<Resource>().Table;
            fkRelationships = table.Relationships;
            Assert.AreEqual(2, fkRelationships.Length);
            pkRelationships = table.InverseRelationships;
            Assert.AreEqual(0, pkRelationships.Length);

            table = _model.GetTableMap<City>().Table;
            fkRelationships = table.Relationships;
            Assert.AreEqual(0, fkRelationships.Length);
            pkRelationships = table.InverseRelationships;
            Assert.AreEqual(1, pkRelationships.Length);

        }

        
    }
}
