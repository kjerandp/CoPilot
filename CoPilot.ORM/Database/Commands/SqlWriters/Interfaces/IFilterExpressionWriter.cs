using System.Collections.Generic;
using CoPilot.ORM.Filtering;

namespace CoPilot.ORM.Database.Commands.SqlWriters.Interfaces
{
    public interface IFilterExpressionWriter
    {
        string GetExpression(FilterGraph filter, List<DbParameter> parameters, Dictionary<string, object> args);
    }
}