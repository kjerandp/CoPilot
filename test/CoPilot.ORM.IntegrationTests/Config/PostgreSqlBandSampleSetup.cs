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
            /*block.Append(
                builder.CreateStoredProcedureFromQuery<Recording>("Get_Recordings_CTE", r => r.Recorded > DateTime.MinValue, null, "Genre", "Band", "AlbumTracks")
            );*/

            return block.ToString();
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
