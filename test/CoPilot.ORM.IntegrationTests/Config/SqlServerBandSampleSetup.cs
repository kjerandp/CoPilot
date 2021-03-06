﻿using System;
using CoPilot.ORM.Common;
using CoPilot.ORM.Database;
using CoPilot.ORM.Database.Commands.Options;
using CoPilot.ORM.IntegrationTests.Models.BandSample;
using CoPilot.ORM.Model;
using CoPilot.ORM.SqlServer;
using CoPilot.ORM.Scripting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoPilot.ORM.IntegrationTests.Config
{
    public class SqlServerBandSampleSetup
    {
        private readonly SqlServerProvider _provider;
        private readonly DbModel _model;
        private const string ConnectionString = @"
                    data source=localhost; 
                    initial catalog=<DATABASE>; 
                    Integrated Security=true;
                    MultipleActiveResultSets=True; 
                    App=CoPilotIntegrationTest;";

        public SqlServerBandSampleSetup(DbModel model, LoggingLevel logginLevel = LoggingLevel.None)
        {
            _model = model;
            _provider = new SqlServerProvider(true, logginLevel);
        }

        public void DropCreateDatabase()
        {
            var db = _model.CreateDb(GetConnectionString(true), _provider);
            var scriptBuilder = new ScriptBuilder(db.DbProvider, db.Model);

            db.Command(CreateDatabaseScript(scriptBuilder));

            Console.WriteLine(BandSampleConfig.DbName + " database created...");

            //supress any logging and seed data
            _provider.Logger.SuppressLogging = true;

            Seed(db, scriptBuilder);

            _provider.Logger.SuppressLogging = false;

        }
        private static string CreateDatabaseScript(ScriptBuilder builder)
        {
            const string databaseName = BandSampleConfig.DbName;
            var block = new ScriptBlock();
            var createOptions = CreateOptions.Default();

            var go = new ScriptBlock("GO");

            //start
            block.Append(builder.MultiLineComment("Autogenerated script for CoPilot Bands sample database"));

            //initialize database
            block.Append(
                builder.DropCreateDatabase(databaseName)
            );
            block.Append(go);
            block.Append(
                builder.UseDatabase(databaseName)
            );
            block.Append(go);

            //create all tables
            block.Append(
                builder.CreateTablesIfNotExists(createOptions)
            );

            //create stored procedure
            block.Append(
                builder.CreateStoredProcedureFromQuery<Recording>("Get_Recordings_CTE", r => r.Recorded > DateTime.MinValue, null, "Genre", "Band", "AlbumTracks")
            );

            return block.ToString();
        }

        private static void Seed(IDb db, ScriptBuilder builder)
        {
            //var options = new ScriptOptions { EnableIdentityInsert = false, SelectScopeIdentity = true, UseNvar = true, Parameterize = true };
            using (var writer = new DbWriter(db) { Operations = OperationType.All })
            {
                try
                {
                    var script = builder.UseDatabase(BandSampleConfig.DbName);
                    writer.Command(script.ToString());

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
            return ConnectionString.Replace("<DATABASE>", admin ? "master": BandSampleConfig.DbName);
        }

        public IDb GetDb()
        {
            return _model.CreateDb(GetConnectionString(), _provider);
        }
    }
}
