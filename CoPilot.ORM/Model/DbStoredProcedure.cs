using System.Collections.Generic;
using CoPilot.ORM.Database.Commands;

namespace CoPilot.ORM.Model
{
    public class DbStoredProcedure
    {
        
        public DbStoredProcedure(string procName)
        {
            ProcedureName = procName;
            Parameters = new List<DbParameter>();
        }

        public string ProcedureName { get; }
        public List<DbParameter> Parameters { get; }

    }
}