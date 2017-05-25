using System;
using CoPilot.ORM.Database.Commands.Options;
using CoPilot.ORM.Database.Commands.Query;
using CoPilot.ORM.Database.Commands.SqlWriters;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Model;
using CoPilot.ORM.Scripting;
using CoPilot.ORM.Tests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoPilot.ORM.Tests
{
    [TestClass]
    public class QueryWriterTests
    {
        private DbModel _model;

        [TestInitialize]
        public void Init()
        {
            _model = TestModel.GetModel();
        }

        [TestMethod]
        public void CanCreateCreateStatements()
        {
            var scriptBuilder = new ScriptBuilder(_model);
            Console.WriteLine(scriptBuilder.CreateTable(_model.GetTable("TST_MEDIA"),CreateOptions.Default()));
        }

        [TestMethod]
        public void CanWriteProperSqlForSelectSingle()
        {
            var writer = new SqlSelectStatementWriter();
            var builder = new SqlQueryBuilder();
            var ctx = _model.CreateContext<Resource>("Owner.City", "UsedBy.City");
            var filterGraph = ExpressionHelper.DecodeExpression<Resource>(r => r.Id == 1);
            ctx.ApplyFilter(filterGraph);
            var sql = writer.GetStatement(builder.Build(ctx.GetQueryContext()));

            Console.WriteLine(sql);
        }
    }
}
