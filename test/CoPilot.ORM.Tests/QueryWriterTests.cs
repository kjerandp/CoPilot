using System;
using CoPilot.ORM.Database.Commands.Options;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Model;
using CoPilot.ORM.Providers.SqlServer;
using CoPilot.ORM.Scripting;
using CoPilot.ORM.Tests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoPilot.ORM.Tests
{
    [TestClass]
    public class QueryWriterTests
    {
        private DbModel _model;
        private SqlServerProvider _provider;

        [TestInitialize]
        public void Init()
        {
            _model = TestModel.GetModel();
            _provider = new SqlServerProvider();
        }

        [TestMethod]
        public void CanCreateCreateStatements()
        {
            var scriptBuilder = new ScriptBuilder(_provider, _model);
            Console.WriteLine(scriptBuilder.CreateTable(_model.GetTable("TST_MEDIA"),CreateOptions.Default()));
        }

        [TestMethod]
        public void CanWriteProperSqlForSelectSingle()
        {
            var writer = _provider.SelectStatementWriter;
            var builder = _provider.SelectStatementBuilder;
            var ctx = _model.CreateContext<Resource>("Owner.City", "UsedBy.City");
            var filterGraph = ExpressionHelper.DecodeExpression<Resource>(r => r.Id == 1, _provider);
            ctx.ApplyFilter(filterGraph);
            var sql = writer.GetStatement(builder.Build(ctx.GetQueryContext()));

            Console.WriteLine(sql);
        }
    }
}
