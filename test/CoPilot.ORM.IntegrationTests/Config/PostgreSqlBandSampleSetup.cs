using System;
using CoPilot.ORM.Common;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Database.Commands.Options;
using CoPilot.ORM.IntegrationTests.Models.BandSample;
using CoPilot.ORM.Model;
using CoPilot.ORM.PostgreSql;
using CoPilot.ORM.Scripting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoPilot.ORM.IntegrationTests.Config
{
    public class PostgreSqlBandSampleSetup
    {
        private readonly PostgreSqlProvider _provider;
        private readonly DbModel _model;
        private const string ConnectionString = @"
                    Server=localhost; 
                    Database=<DATABASE>; 
                    User Id=testuser;
                    Password=password;";

        public PostgreSqlBandSampleSetup(DbModel model, LoggingLevel loggingLevel = LoggingLevel.None)
        {
            _model = model;
            _provider = new PostgreSqlProvider(loggingLevel: loggingLevel);
        }

        public void DropCreateDatabase()
        {
            var db = _model.CreateDb(GetConnectionString(true), _provider);
            var scriptBuilder = new ScriptBuilder(db.DbProvider, db.Model);

            var dbExist = db.Scalar($"SELECT 1 FROM pg_database WHERE datname = '{BandSampleConfig.DbName.ToLower()}'") != null;
            if (dbExist)
            {
                db.Command(scriptBuilder.DropDatabase(BandSampleConfig.DbName).ToString());
            }
            db.Command(scriptBuilder.CreateDatabase(BandSampleConfig.DbName).ToString());

            db = _model.CreateDb(GetConnectionString(), _provider);

            db.Command(CreateDatabaseObjectsScript(scriptBuilder));

            Console.WriteLine(BandSampleConfig.DbName + " database created...");

            //supress any logging and seed data
            
            _provider.Logger.SuppressLogging = true;
            
            Seed(db);

            _provider.Logger.SuppressLogging = false;


        }
        

        private static string CreateDatabaseObjectsScript(ScriptBuilder builder)
        {
            var block = new ScriptBlock();
            var createOptions = CreateOptions.Default();

            //create all tables
            block.Append(
                builder.CreateTablesIfNotExists(createOptions)
            );

            //create stored procedure
            block.Append(
                CreateTestFunction()
            );

            return block.ToString();
        }

        private static ScriptBlock CreateTestFunction()
        {
            var script = new ScriptBlock(@"
            CREATE OR REPLACE FUNCTION Get_Recordings_CTE (recorded Timestamp) RETURNS SETOF refcursor AS $$
            DECLARE
	            ref1 refcursor;
                ref2 refcursor;
            BEGIN
		    /*
			    Record sets should be named as follows when executed from CoPilot:
			        - Base
			        - Base.AlbumTracks
		    */
		    CREATE TEMPORARY TABLE IF NOT EXISTS tmp_Base AS (
		    SELECT
			    T1.RECORDING_ID
			    ,T1.RECORDING_SONG_TITLE
			    ,T1.RECORDING_DURATION
			    ,T1.RECORDING_RECORDED
			    ,T2.MUSIC_GENRE_ID as ""Genre.MUSIC_GENRE_ID""
                ,T2.MUSIC_GENRE_NAME as ""Genre.MUSIC_GENRE_NAME""
                , T3.BAND_ID as ""Band.BAND_ID""
                , T3.BAND_NAME as ""Band.BAND_NAME""
                , T3.BAND_FORMED as ""Band.BAND_FORMED""
            FROM
                RECORDING T1
                INNER JOIN MUSIC_GENRE T2 ON T2.MUSIC_GENRE_ID = T1.RECORDING_GENRE_ID
                INNER JOIN BAND T3 ON T3.BAND_ID = T1.RECORDING_BAND_ID
            WHERE
                T1.RECORDING_RECORDED > recorded
            );

            OPEN ref1 FOR SELECT *FROM tmp_Base;
            RETURN NEXT ref1;

            OPEN ref2 FOR SELECT
                T4.ALBUM_TRACK_ID
			    ,T4.ALBUM_TRACK_NUMBER
			    ,T4.ALBUM_TRACK_RECORDING_ID
            FROM
                ALBUM_TRACK T4
                INNER JOIN tmp_Base T1 ON T4.ALBUM_TRACK_RECORDING_ID = T1.RECORDING_ID;
                RETURN NEXT ref2;
            END;
            $$ LANGUAGE plpgsql;");
            return script;
        }

        private static void Seed(IDb db)
        {
            //var options = new ScriptOptions { EnableIdentityInsert = false, SelectScopeIdentity = true, UseNvar = true, Parameterize = true };
            using (var writer = new DbWriter(db) { Operations = OperationType.All })
            {
                try
                {
                    var fakeData = new FakeData();

                    fakeData.Seed(writer);

                    writer.Commit();
                    Console.WriteLine(BandSampleConfig.DbName + " database seeded...");
                }
                catch (Exception ex)
                {
                    writer.Rollback();
                    Assert.Fail(ex.Message);
                }

            }
        }

        private static string GetConnectionString(bool admin = false)
        {
            return ConnectionString.Replace("<DATABASE>", admin ? "postgres" : BandSampleConfig.DbName.ToLower());
        }

        public IDb GetDb()
        {
            return _model.CreateDb(GetConnectionString(), _provider);
        }
    }
}
