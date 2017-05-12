using CoPilot.ORM.Common;
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


        [ClassInitialize]
        public static void BandSampleTestsInitialize(TestContext testContext)
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

        [TestMethod]
        public void CanQueryForAllRecordingsFromASpecificAlbumUsingSelectorSyntax()
        {
            var recordings = _db.Query<AlbumTrack, Recording>(r => r.Recording, r => r.Album.Id == 1);

            //Results in the following query:

            /* 
            SELECT
		        T3.RECORDING_ID as [Id]
		        ,T3.RECORDING_SONG_TITLE as [SongTitle]
		        ,T3.RECORDING_DURATION as [Duration]
		        ,T3.RECORDING_RECORDED as [Recorded]
	        FROM
		        ALBUM_TRACK T1
		        INNER JOIN RECORDING T3 ON T3.RECORDING_ID=T1.ALBUM_TRACK_RECORDING_ID
	        WHERE
		        T1.ALBUM_TRACK_ALBUM_ID = @param1     
             */

            // The reason there is no T2 in this case is that the filter is referencing the ALBUM's id column (PK)
            // but CoPilot optimized this to use the FK of ALBUM_TRACK instead.  
        }

        [TestMethod]
        public void CanQueryForAllRecordingTitlesFromASpecificAlbumUsingSelectorSyntax()
        {
            var recordings = _db.Query<AlbumTrack, string>(r => r.Recording.SongTitle, r => r.Album.Id == 1);
            
            //Results in the following query:

            /*
            SELECT
		        T3.RECORDING_SONG_TITLE as [SongTitle]
	        FROM
		        ALBUM_TRACK T1
		        INNER JOIN RECORDING T3 ON T3.RECORDING_ID=T1.ALBUM_TRACK_RECORDING_ID
	        WHERE
		        T1.ALBUM_TRACK_ALBUM_ID = @param1 
            */
        }

        [TestMethod]
        public void CanValidateModel()
        {
            _db.ValidateModel();
        }
    }
}
