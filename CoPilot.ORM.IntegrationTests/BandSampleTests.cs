using CoPilot.ORM.Database;
using CoPilot.ORM.IntegrationTests.Config;
using CoPilot.ORM.IntegrationTests.Models.BandSample;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoPilot.ORM.IntegrationTests
{
    [TestClass]
    public class BandSampleTests
    {
        private static IDb _db;

        private const string ConnectionString = @"
                data source=localhost; 
                initial catalog="+BandSampleDatabase.DbName+@"; 
                Integrated Security=true;
                MultipleActiveResultSets=True; 
                App=CoPilotIntegrationTest;";


        [AssemblyInitialize()]
        public static void MyTestInitialize(TestContext testContext)
        {
            //CoPilotGlobalResources.LoggingLevel = LoggingLevel.Verbose;
            var model = BandSampleConfig.CreateModel();

            BandSampleDatabase.DropCreateDatabase(model);

            _db = model.CreateDb(ConnectionString);
            
        }

        [TestMethod]
        public void CanQueryForBands()
        {
            var bands = _db.Query<Band>(null, "BandMembers.Person.City", "Based");
        }

        [TestMethod]
        public void CanQueryForBandMembers()
        {
            var bandMembers = _db.Query<BandMember>(null, "Person.City", "Band.Based");
        }

        [TestMethod]
        public void CanQueryForRecordings()
        {
            var recordings = _db.Query<Recording>(null, "Genre", "Band");
        }

        [TestMethod]
        public void CanQueryForAlbums()
        {
            var albums = _db.Query<Album>(null, "Tracks.Recording");
        }



    }
}
