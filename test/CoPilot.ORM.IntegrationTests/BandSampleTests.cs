using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CoPilot.ORM.Context.Query;
using CoPilot.ORM.Database;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Database.Commands.Options;
using CoPilot.ORM.IntegrationTests.Config;
using CoPilot.ORM.IntegrationTests.Models.BandSample;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace CoPilot.ORM.IntegrationTests
{
    [TestClass]
    public class BandSampleTests
    {
        private static IDb _db;
        
        [ClassInitialize]
        public static void BandSampleTestsInitialize(TestContext testContext)
        {
            var model = BandSampleConfig.CreateModel();
            var databaseSetup = new MySqlBandSampleSetup(model);
            //var databaseSetup = new SqlServerBandSampleSetup(model);
            databaseSetup.DropCreateDatabase();
            _db = databaseSetup.GetDb();
            
        }

        [TestMethod]
        public void CanQueryForBands()
        {
            var bands = _db.All<Band>("BandMembers.Person.City", "Based").ToList();

            Assert.IsTrue(bands.Any());
            Assert.IsTrue(bands.Any(r => r.BandMembers.Any(m => m.Person?.City != null)));
            Assert.IsTrue(bands.Any(r => r.Based != null));
        }

        [TestMethod]
        public void CanQueryForBandMembers()
        {
            var allBandMembers = _db.All<BandMember>("Person.City", "Band.Based").ToList();

            var someBandMembers = _db.Query<BandMember>(r => r.Band.Name.StartsWith("B"), "Person.City", "Band.Based").ToList();

            Assert.IsTrue(allBandMembers.Count() > someBandMembers.Count);
            Assert.IsTrue(allBandMembers.Any(r => r.Person?.City != null));
            Assert.IsTrue(someBandMembers.Any(r => r.Band?.Based != null));

        }

        [TestMethod]
        public void CanQueryForRecordings()
        {
            var recordings = _db.All<Recording>(new Predicates { Top = 2000 }, "Genre", "Band", "AlbumTracks").ToList();

            Assert.IsTrue(recordings.Any());
            Assert.IsTrue(recordings.Any(r => r.Genre != null));
            Assert.IsTrue(recordings.Any(r => r.Band != null));
            Assert.IsTrue(recordings.Any(r => r.AlbumTracks != null && r.AlbumTracks.Any()));
        }

        [TestMethod]
        public void CanExecuteAndMapStoredProcedure()
        {
            var recordings = _db.Query<Recording>(
                "Get_Recordings_CTE",
                new { recorded = new DateTime(2017, 5, 1) },
                "Base", "Base.AlbumTracks"
            );

            Assert.IsTrue(recordings.Any());

        }

        [TestMethod]
        public void CanQueryForAlbums()
        {
            var albums = _db.Query<Album>(null, "Tracks.Recording").ToList();
            Assert.IsTrue(albums.Any());
            Assert.IsTrue(albums.Any(r => r.Tracks.Any(t => t.Recording != null)));
            
        }

        [TestMethod]
        public void CanQueryForAllRecordingsFromASpecificAlbumUsingSelectorSyntax()
        {
            var recordings = _db.Query<AlbumTrack, Recording>(r => r.Recording, r => r.Album.Id == 1);

            Assert.IsTrue(recordings.Any());

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

            Assert.IsTrue(recordings.Any());


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
        public void CanExecuteStatememtAndPassObjectWithMatchingParameterNamesAsArgs()
        {
            var band = new Band {Id = 1, Name = "Not the actual name"};
            var dbBand = _db.Query<Band>("SELECT * FROM BAND WHERE BAND_ID=@Id", band).Single();

            Assert.AreEqual(band.Id, dbBand.Id);
            Assert.AreNotEqual(band.Name, dbBand.Name);

            var rowsUpdated = _db.Command("UPDATE BAND SET BAND_NAME=@Name WHERE BAND_ID=@Id", band);

            Assert.AreEqual(1, rowsUpdated);
            
            var updatedBand = _db.Query<Band>("SELECT * FROM BAND WHERE BAND_ID=@Id", new {band.Id}).Single();
            Assert.AreEqual(band.Id, updatedBand.Id);
            Assert.AreEqual(band.Name, updatedBand.Name);

            rowsUpdated = _db.Command("UPDATE BAND SET BAND_NAME=@Name WHERE BAND_ID=@Id", dbBand);

            Assert.AreEqual(1, rowsUpdated);

            updatedBand = _db.Query<Band>("SELECT * FROM BAND WHERE BAND_ID=@Id", new { updatedBand.Id }).Single();
            Assert.AreEqual(dbBand.Id, updatedBand.Id);
            Assert.AreEqual(dbBand.Name, updatedBand.Name);
        }

        [TestMethod]
        public void CanInsertWithIdentityInsertEnabled()
        {
            var options = new ScriptOptions
            {
                EnableIdentityInsert = true
            };

            using (var writer = new DbWriter(_db) {Options = options})
            {
                var maxId = writer.Scalar<int>("select max(band_id) from band");
                var testBand = new Band
                {
                    Id = maxId + 1,
                    Formed = DateTime.Today,
                    Name = "Test Band",
                    Based = writer.GetReader().FindByKey<City>(1)
                };

                writer.Save(testBand);
                writer.Rollback();
            }
        }

        [TestMethod]
        public void CanDoBulkInserts()
        {
            const int insertCount = 10000;
            
            var bands = new List<object>();
            var dt = new DateTime(1980, 1, 1);

            for (var i = 0; i < insertCount; i++)
            {
                bands.Add(new { cityId = 1, bandName = "Bulk Band " + i, formed = dt.AddDays(i) });
            }

            var sw = Stopwatch.StartNew();
            using (var writer = new DbWriter(_db))
            {
                writer.BulkCommand(
                    "insert into BAND (city_id,band_name,band_formed) values (@cityId, @bandName, @formed)", bands);

                writer.Commit();
            }
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);
        }

        [TestMethod]
        public void CanDoLazyBulkInserts()
        {
            const int insertCount = 10000;
            var sw = Stopwatch.StartNew();
            using (var writer = new DbWriter(_db))
            {
                var dt = new DateTime(1980,1,1);
                writer.PrepareCommand("insert into BAND (city_id,band_name,band_formed) values (@cityId, @bandName, @formed)", new {cityId=0, bandName=string.Empty, formed=dt});

                for (var i = 0; i < insertCount; i++)
                {
                    writer.Command(new {cityId = 1, bandName = "Lazy Band " + i, formed = dt.AddDays(i)});
                }

                writer.Commit();
            }
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);
        }

        [TestMethod]
        public void CanValidateModel()
        {
            _db.ValidateModel();
        }
    }
}
