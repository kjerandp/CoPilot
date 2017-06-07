using System;
using System.Linq;
using CoPilot.ORM.Config;
using CoPilot.ORM.Config.Naming;
using CoPilot.ORM.Database;
using CoPilot.ORM.IntegrationTests.MySql.WorldModels;
using CoPilot.ORM.Providers.MySql;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoPilot.ORM.IntegrationTests.MySql
{
    [TestClass]
    public class WorldTests
    {
        private readonly IDb _db = WorldConfig.Create();

        [TestMethod]
        public void CanConnectAndExecuteSimpleQuery()
        {
            var response = _db.Query("select * from country", null);
            Assert.AreEqual(1,response.RecordSets.Length);
            Assert.IsTrue(response.RecordSets[0].Records.Any());
            response = _db.Query("select * from country;select * from city where population >= @population", new {population=1000000});
            Assert.AreEqual(2, response.RecordSets.Length);
            Assert.IsTrue(response.RecordSets[0].Records.Any());
            Assert.IsTrue(response.RecordSets[1].Records.Any());
        }

        [TestMethod]
        public void CanConnectAndExecuteSimpleContextQueries()
        {
            var response = _db.Query<Country>(r => r.Continent == "Europe");
            Assert.IsTrue(response.Any());
        }

        [TestMethod]
        public void CanConnectAndExecuteSimpleContextQueriesWithJoins()
        {
            var response = _db.Query<Country>(r => r.Continent == "Europe", "Cities");
            Assert.IsTrue(response.Any(r => r.Cities.Any()));

            response = _db.Query<Country>(r => r.Continent == "Europe", "Cities", "Languages");
            Assert.IsTrue(response.Any(r => r.Languages.Any()));
        }

        [TestMethod]
        public void CanConnectAndExecuteSelectorTypeQueries()
        {
            var response = _db.Query<CountryLanguage, Country>(r => r.Country, r => r.Language == "French" && r.IsOfficial);
            Assert.IsTrue(response.Any());

            
        }

    }

    public static class WorldConfig
    {
        private const string DefaultConnectionString = @"
                Server=localhost;
                Database=world;
                Uid=ApplicationUser;
                Pwd=fire4test;";

        public static IDb Create(string connectionString = null)
        {
            var mapper = new DbMapper();

            mapper.SetColumnNamingConvention(DbColumnNamingConvention.SameAsClassMemberNames);

            var cnt = mapper.Map<Country>("Country");
            cnt.AddKey(r => r.Code).MaxSize(3).DefaultValue(null);

            var cit = mapper.Map<City>("City", r => r.Id);
            cit.Column(r => r.CountryCode).MaxSize(3);

            var lan = mapper.Map<CountryLanguage>("CountryLanguage");
            lan.AddKey(r => r.CountryCode).MaxSize(3);
            lan.AddKey(r => r.Language).MaxSize(30);

            cit.HasOne<Country>(r => r.CountryCode).InverseKeyMember(r => r.Cities);
            lan.HasOne<Country>(r => r.CountryCode).KeyForMember(r => r.Country).InverseKeyMember(r => r.Languages);

            return mapper.CreateDb(connectionString ?? DefaultConnectionString, new MySqlServerProvider());
        }
    }
}
