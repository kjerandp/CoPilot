using System.Collections.Generic;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Filtering;
using CoPilot.ORM.Mapping.Mappers;
using System.Linq;

namespace CoPilot.ORM.Database.Commands.Query.Strategies
{
    public class SingleStatementStrategy : IQueryExecutionStrategy
    {
        private readonly ISingleStatementQueryWriter _scriptCreator;

        public SingleStatementStrategy(ISingleStatementQueryWriter scriptCreator)
        {
            _scriptCreator = scriptCreator;
        }
        public IEnumerable<object> Execute(ITableContextNode node, FilterGraph filter, DbReader reader)
        {
            return Execute<object>(node, filter, reader);
        }

        public IEnumerable<T> Execute<T>(ITableContextNode node, FilterGraph filter, DbReader reader)
        {
            string[] names;
            var stm = _scriptCreator.CreateStatement(node, filter, out names);

            var response = reader.Query(stm, names.ToArray());

            return ContextMapper.MapAndMerge<T>(node.Context.SelectTemplate, response.RecordSets);
        }
    }
}
