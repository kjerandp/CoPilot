using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using CoPilot.ORM.Common;
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
            //var databaseSetup = new MySqlBandSampleSetup(model);
            var databaseSetup = new SqlServerBandSampleSetup(model);
            //databaseSetup.DropCreateDatabase();
            _db = databaseSetup.GetDb();
            
        }

        [TestMethod]
        public void CanCreateQueriesWithNewQuerySyntax()
        {
            var bands = _db.From<Band>()
                .Where(r => r.Id > 0 && !r.Name.Contains("Band")) 
                .Include("BandMembers")
                .OrderBy(r => r.Name)
                .ThenBy(r => r.Formed, Ordering.Descending)
                .ThenBy(r => r.Id)
                .Skip(1)
                .Take(20)
                .Distinct()
                .ToArray();

            Assert.IsTrue(bands.Any(r => r.BandMembers != null && r.BandMembers.Any()));
            /*
            var oldWay = _db.Query(
                    OrderByClause<Band>.OrderByAscending(r => r.Name)
                        .ThenByDecending(r => r.Formed)
                        .ThenByAscending(r => r.Id), 
                    new Predicates {Take = 20, Skip = 1, Distinct = true}, 
                    r => r.Id <= 40, 
                    "BandMembers");
            */

        }
        [TestMethod]
        public void CanCreateQueriesWithMultipleLevelsInclude()
        {
            var band = _db.From<Band>()
                .Where(r => r.Id == 1)
                .Include("Based.Country")
                .OrderBy(r => r.Name, Ordering.Descending)
                .ThenBy(r => r.Id)
                .Single();

            Assert.AreEqual(1, band.Id);
            Assert.IsFalse(string.IsNullOrEmpty(band.Name));
            Assert.IsFalse(string.IsNullOrEmpty(band.Based.Country.Name));
        }

        [TestMethod]
        public void CanCreateQueriesWithNewQuerySyntaxWithAnonymousType()
        {
            var band = _db.From<Band>()
                .Where(r => r.Id == 1)
                .Select(r => new { BandId = r.Id, BandName = r.Name, Nationality = r.Based.Country.Name })
                .OrderBy(r => r.BandName, Ordering.Descending)
                .ThenBy(r => r.BandId)
                .Single();

            Assert.AreEqual(1, band.BandId);
            Assert.IsFalse(string.IsNullOrEmpty(band.BandName));
            Assert.IsFalse(string.IsNullOrEmpty(band.Nationality));
        }

        [TestMethod]
        public void CanCreateQueriesWithNewQuerySyntaxSimpleAndDynamicType()
        {
            var bandNames = _db.From<Band>()
                .Where(r => r.Id <= 40)
                .Select(r => r.Name)
                .OrderBy(r => r)
                .AsEnumerable()
            ;
            Assert.IsTrue(bandNames.Any());

            var dynamicBands = _db.From<Band>()
                .Where(r => r.Id <= 40)
                .Select<dynamic>(r => new { BandId = r.Id, BandName = r.Name })
                .OrderBy(r => "BandName", Ordering.Descending)
                .ThenBy(r => "BandId")
                .ToArray();

            Assert.IsNotNull(dynamicBands.All(r => r is ExpandoObject));
        }

        [TestMethod]
        public void CanQueryForBands()
        {
            var bands = _db.From<Band>().Select("BandMembers.Person.City", "Based").ToArray();

            Assert.IsTrue(bands.Any());
            Assert.IsTrue(bands.Any(r => r.BandMembers.Any(m => m.Person?.City != null)));
            Assert.IsTrue(bands.Any(r => r.Based != null));
        }

        [TestMethod]
        public void CanQueryForBandMembers()
        {
            //var allBandMembers = _db.All<BandMember>("Person.City", "Band.Based").ToList();
            var allBandMembers = _db.From<BandMember>().Select("Person.City", "Band.Based").ToArray();
            //var someBandMembers = _db.Query<BandMember>(r => r.Band.Name.StartsWith("B"), "Person.City", "Band.Based").ToList();
            var someBandMembers = _db.From<BandMember>().Where(r => r.Band.Name.StartsWith("B")).Select("Person.City", "Band.Based").ToArray();
            Assert.IsTrue(allBandMembers.Length > someBandMembers.Length);
            Assert.IsTrue(allBandMembers.Any(r => r.Person?.City != null));
            Assert.IsTrue(someBandMembers.Any(r => r.Band?.Based != null));

        }

        [TestMethod]
        public void CanQueryForRecordings()
        {
            //var recordings = _db.All<Recording>(new Predicates { Take = 2000 }, "Genre", "Band", "AlbumTracks").ToList();
            var recordings = _db.From<Recording>().Select("Genre", "Band", "AlbumTracks").Take(2000).ToArray();
            Assert.IsTrue(recordings.Any());
            Assert.IsTrue(recordings.Any(r => r.Genre != null));
            Assert.IsTrue(recordings.Any(r => r.Band != null));
            Assert.IsTrue(recordings.Any(r => r.AlbumTracks != null && r.AlbumTracks.Any()));
        }

        [TestMethod]
        public void CanExecuteAndMapStoredProcedure()
        {
            var recordings = _db.Query<Recording>("Get_Recordings_CTE", new { recorded = new DateTime(2017, 5, 1) },"Base", "Base.AlbumTracks");

            Assert.IsTrue(recordings.Any());

        }

        [TestMethod]
        public void CanQueryForAlbums()
        {
            var albums = _db.From<Album>().Include("Tracks.Recording").ToArray();
            Assert.IsTrue(albums.Any());
            Assert.IsTrue(albums.Any(r => r.Tracks.Any(t => t.Recording != null)));
            
        }

        [TestMethod]
        public void CanQueryForAllRecordingsFromASpecificAlbumUsingSelectorSyntax()
        {
            var recordings = _db.From<AlbumTrack>().Where(r => r.Album.Id == 1).Select(r => r.Recording).AsEnumerable();

            Assert.IsTrue(recordings.Any());

            //Results in the following query:

            /* 
            SELECT
		        T2.RECORDING_ID as [Id]
		        ,T2.RECORDING_SONG_TITLE as [SongTitle]
		        ,T2.RECORDING_DURATION as [Duration]
		        ,T2.RECORDING_RECORDED as [Recorded]
	        FROM
		        ALBUM_TRACK T1
		        INNER JOIN RECORDING T2 ON T2.RECORDING_ID=T1.ALBUM_TRACK_RECORDING_ID
	        WHERE
		        T1.ALBUM_TRACK_ALBUM_ID = @param1     
             */

 
        }

        [TestMethod]
        public void CanQueryForAllRecordingTitlesFromASpecificAlbumUsingSelectorSyntax()
        {
            var recordings = _db.From<AlbumTrack>().Where(r => r.Album.Id == 1).Select(r => r.Recording.SongTitle).AsEnumerable();

            Assert.IsTrue(recordings.Any());


            //Results in the following query:

            /*
            SELECT
		        T2.RECORDING_SONG_TITLE as [SongTitle]
	        FROM
		        ALBUM_TRACK T1
		        INNER JOIN RECORDING T2 ON T2.RECORDING_ID=T1.ALBUM_TRACK_RECORDING_ID
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
            _db.DbProvider.Logger.SuppressLogging = true;
            const int insertCount = 1000;
            
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
            _db.DbProvider.Logger.SuppressLogging = false;
            Console.WriteLine(sw.ElapsedMilliseconds);
        }

        [TestMethod]
        public void CanDoLazyBulkInserts()
        {
            _db.DbProvider.Logger.SuppressLogging = true;

            const int insertCount = 1000;
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
            _db.DbProvider.Logger.SuppressLogging = false;
            Console.WriteLine(sw.ElapsedMilliseconds);
        }

        [TestMethod]
        public void CanValidateModel()
        {
            _db.ValidateModel();
        }
    }
}
