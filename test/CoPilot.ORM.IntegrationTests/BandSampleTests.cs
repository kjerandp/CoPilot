using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using CoPilot.ORM.Common;
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

        /// <summary>
        /// Select the provider by comment/uncomment the proper databaseSetup variable.
        /// If you want to drop/create the sample database, uncomment databaseSetup.DropCreateDatabase();
        /// To view the resulting SQL statements executed, set logging level to VERBOSE
        /// </summary>
        /// <param name="testContext"></param>
        [ClassInitialize]
        public static void BandSampleTestsInitialize(TestContext testContext)
        {
            var logginLevel = LoggingLevel.None;
            var model = BandSampleConfig.CreateModel();
            //var databaseSetup = new MySqlBandSampleSetup(model, logginLevel);
            var databaseSetup = new SqlServerBandSampleSetup(model, logginLevel);
            //databaseSetup.DropCreateDatabase();
            _db = databaseSetup.GetDb();        
        }

        /// <summary>
        /// This shows most of the query builder functions available
        /// </summary>
        [TestMethod]
        public void CanCreateQueriesWithNewQuerySyntax()
        {
            var bands = _db.From<Band>()
                .Where(r => !r.Name.StartsWith("B") && !r.Name.StartsWith("L")) 
                .Include("BandMembers")
                .OrderBy(r => r.Name)
                .ThenBy(r => r.Formed, Ordering.Descending)
                .ThenBy(r => r.Id)
                .Skip(1)
                .Take(20)
                .Distinct()
                .AsEnumerable();

            Assert.IsTrue(bands.Any(r => r.BandMembers != null && r.BandMembers.Any()));
        }

        [TestMethod]
        public void CanCreateQueriesWithNewQuerySyntaxAndProjection()
        {
            var band = _db.From<Band>()
                .Where(r => r.Id == 1)
                .Select(r => new { BandId = r.Id, BandName = r.Name, Nationality = r.Based.Country.Name })
                .OrderBy(r => r.Nationality)
                .Single();

            Assert.AreEqual(1, band.BandId);
            Assert.IsFalse(string.IsNullOrEmpty(band.BandName));
            Assert.IsFalse(string.IsNullOrEmpty(band.Nationality));

            var bands = _db.From<Band>()
                .Select(r => new { BandId = r.Id, BandName = r.Name, Nationality = r.Based.Country.Name })
                .OrderBy(r => r.Nationality)
                .Take(20)
                .ToArray();

            Assert.AreEqual(20, bands.Length);
        }

        [TestMethod]
        public void CanBuildTemplateFromMultipleLevelsOfIncludesAndMapToModel()
        {
            var test = _db.From<Band>()
                .Where(r => r.Id <= 20)
                .Include("BandMembers.Person.City.Country", "Based.Country", "Recordings.Genre")
                .ToArray();

            Assert.IsTrue(test.All(r => r.Id <= 20));  
            Assert.IsTrue(test.Any(b => b.Based?.Country?.Name != null));
            Assert.IsTrue(test.Any(r => r.BandMembers != null && r.BandMembers.Any(b => b.Person?.City?.Country?.Name != null)));
            Assert.IsTrue(test.Any(r => r.Recordings != null && r.Recordings.Any(b => b.Genre?.Name != null)));
        }

        [TestMethod]
        public void CanCreateUseArithmeticsInWhereClause()
        {
            var test = _db.From<Album>()
                .Where(r => r.Id + 4 == 5)
                .ToArray();

            Console.WriteLine(test.Length);
        }

        [TestMethod]
        public void CanCreateQueriesAndMapWithComplexProjections()
        {
           
            var band = _db.From<Band>()
                .Where(r => r.Id == 1)
                .Select(r => new
                {
                    BandName = r.Name,
                    Songs = r.Recordings.Select(a => new
                    {
                        Title = a.SongTitle.ToUpper(),
                        a.Duration.TotalSeconds,
                        Genre = a.Genre.Name
                    }),
                    Artists = r.BandMembers.Select(n => new
                    {
                        Name = n.ArtistName ?? n.Person.Name,
                        Letter = n.Person.Name != null ? n.Person.Name[0]:'?',
                        HasArtistName = n.ArtistName != null,
                        n.Instrument,
                        ArtistInfo = new
                        {
                            IdAsString = n.Id.ToString(),
                            n.Person.Name,
                            Nationality = n.Person.City.Country
                        }
                    })
                })
                .OrderBy(r => r.BandName, Ordering.Descending)
                .Single();
        
            Assert.IsTrue(band.Songs.Any());
            Assert.IsTrue(band.Artists.Any());


        }

        [TestMethod]
        public void CanCreateQueriesWithIncludeAndOneToManyRelations()
        {
            var band = _db.From<Band>()
                .Where(r => r.Id == 1)
                .Select("BandMembers")
                .OrderBy(r => r.Name, Ordering.Descending)
                .Single();

            Assert.IsNotNull(band);
            Assert.AreEqual(1, band.Id);
            Assert.IsTrue(band.BandMembers != null && band.BandMembers.Any());

        }

        [TestMethod]
        public void CanProjectToDtoWithConstructor()
        {  
            var bands = _db.From<Band>().Where(r => r.Id <= 30).Select(r => new Foo(r.Name, r.Id)).ToArray();

            Assert.IsTrue(bands.All(r => !string.IsNullOrEmpty(r.Value)));         
        }

        [TestMethod]
        public void CanProjectToDtoWithClassInit()
        {
            var bands = _db.From<Band>().Where(r => r.Id <= 30).Select(r => new Foo(r.Name, r.Id) { Date = r.Formed }).ToArray();
            Assert.IsTrue(bands.All(r => !string.IsNullOrEmpty(r.Value)));
            Assert.IsTrue(bands.Any(r => r.Date.HasValue));
        }

        // class used in above tests
        private class Foo
        {
            public Foo(string name, int id)
            {
                Value = name + id;
            }

            public string Value { get; }
            public DateTime? Date { get; set; }
        }


        [TestMethod]
        public void CanSelectManyWithProjection()
        {

            var bands = _db.From<Band>().Where(r => r.Id <= 5).Select(r => 
                new {
                    BandName = r.Name,
                    Discography = r.Recordings.SelectMany(b => b.AlbumTracks.Select(t => 
                    new {
                        Album = t.Album.Title,
                        Song = b.SongTitle,
                        t.TrackNumber   
                    })).OrderBy(x => x.Album).ThenBy(x => x.TrackNumber)
                }
                ).ToArray();

            Assert.IsTrue(bands.Any(r => r.Discography != null && r.Discography.Any()));
        }
        
        

        [TestMethod]
        public void CanCreateQueriesWithMultipleLevelsInclude()
        {
            var band = _db.From<Band>()
                .Where(r => r.Id == 1)
                .Include("Based.Country")
                .OrderBy(r => r.Name, Ordering.Descending)
                .ThenBy(r => r.Based.Name)
                .ThenBy(r => r.Id)
                .Single();

            Assert.AreEqual(1, band.Id);
            Assert.IsFalse(string.IsNullOrEmpty(band.Name));
            Assert.IsFalse(string.IsNullOrEmpty(band.Based.Country.Name));
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
            var bands = _db.From<Band>().Where(r => !r.Name.Contains("Bulk Band")).Select("BandMembers.Person.City", "Based").ToArray();

            Assert.IsTrue(bands.Any());
            Assert.IsTrue(bands.Any(r => r.BandMembers != null && r.BandMembers.Any(m => m.Person?.City != null)));
            Assert.IsTrue(bands.Any(r => r.Based != null));
        }

        [TestMethod]
        public void CanQueryForBandMembers()
        {

            var allBandMembers = _db.From<BandMember>().Select("Person.City", "Band.Based").ToArray();
            var someBandMembers = _db.From<BandMember>().Where(r => r.Band.Name.StartsWith("B")).Select("Person.City", "Band.Based").ToArray();

            Assert.IsTrue(allBandMembers.Length > someBandMembers.Length);
            Assert.IsTrue(allBandMembers.Any(r => r.Person?.City != null));
            Assert.IsTrue(someBandMembers.Any(r => r.Band?.Based != null));

        }

        [TestMethod]
        public void CanQueryForRecordings()
        {

            var recordings = _db.From<Recording>().Select("Genre", "Band", "AlbumTracks").Take(2000).ToArray();
            Assert.IsTrue(recordings.Any());
            Assert.IsTrue(recordings.Any(r => r.Genre != null));
            Assert.IsTrue(recordings.Any(r => r.Band != null));
            Assert.IsTrue(recordings.Any(r => r.AlbumTracks != null && r.AlbumTracks.Any()));
        }

        [TestMethod]
        public void CanExecuteAndMapStoredProcedure()
        {
            var recordings = _db.Query<Recording>(
                "Get_Recordings_CTE",                           //stored proc name
                new { recorded = new DateTime(2017, 5, 1) },    //arguments
                "r", "r.AlbumTracks"                        //record set names (first part of path "r" is irrelevant and can be anything)
            );

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
                    writer.Command(new {cityId = 1, bandName = "Lazy Bulk Band " + i, formed = dt.AddDays(i)});
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
