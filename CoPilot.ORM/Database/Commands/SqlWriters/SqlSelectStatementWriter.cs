using CoPilot.ORM.Database.Commands.SqlWriters.Interfaces;

namespace CoPilot.ORM.Database.Commands.SqlWriters
{
    public class SqlSelectStatementWriter : ISelectStatementWriter
    {
        public SqlStatement GetStatement(QuerySegments segments)
        {
            var statement = new SqlStatement
            {
                Parameters = segments.Parameters,
                Args = segments.Arguments
            };

            var sql = "SELECT"
                      + segments.Print(QuerySegment.PreSelect, " ")
                      + segments.Print(QuerySegment.Select, "\n\t,", required: true)
                      + segments.Print(QuerySegment.PostSelect, " ", "\n")
                      + "\nFROM"
                      + segments.Print(QuerySegment.PreBaseTable, "\n\t,")
                      + segments.Print(QuerySegment.BaseTable, "\n\t,", required: true)
                      + segments.Print(QuerySegment.PostBaseTable, "\n\t,")
                      + segments.Print(QuerySegment.PreJoins)
                      + segments.Print(QuerySegment.Joins)
                      + segments.Print(QuerySegment.PostJoins);
                
            if (segments.Exist(QuerySegment.Filter))
            {
                sql += "\nWHERE" + segments.Print(QuerySegment.PreFilter)
                    + segments.Print(QuerySegment.Filter) 
                    + segments.Print(QuerySegment.PostFilter);
            }
            if (segments.Exist(QuerySegment.Ordering))
            {
                sql += "\nORDER BY" + segments.Print(QuerySegment.PreOrdering)
                    + segments.Print(QuerySegment.Ordering, "\n\t,")
                    + segments.Print(QuerySegment.PostOrdering, "\n", "\n");
            }

            statement.Script.Add(sql);

            return statement;
        }
    }

    
}
