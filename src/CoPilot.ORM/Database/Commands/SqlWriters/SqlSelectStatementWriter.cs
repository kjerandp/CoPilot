﻿using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Database.Commands.SqlWriters.Interfaces;
using CoPilot.ORM.Scripting;

namespace CoPilot.ORM.Database.Commands.SqlWriters
{
    public class SqlSelectStatementWriter : ISelectStatementWriter
    {
        public ScriptBlock GetStatement(QuerySegments segments)
        {
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
            
            return new ScriptBlock(sql);
        }
    }

    
}