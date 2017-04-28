using System.Collections.Generic;
using System.Data;
using CoPilot.ORM.Config;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Database.Commands
{
    public class SqlStoredProcedure : DbRequest
    {
        public SqlStoredProcedure(string procName)
        {
            Parameters = new List<DbParameter>();
            Args = new Dictionary<string, object>();
            ProcName = procName;
        }
        public string ProcName { get; }
        public override CommandType CommandType => CommandType.StoredProcedure;

        public static DbRequest CreateRequest(DbStoredProcedure proc, object args)
        {
            var p = new SqlStoredProcedure(proc.ProcedureName);
            
            if (args != null)
            {
                p.SetParameters(proc.Parameters, args);
            }
            return p;
        }

        public override string ToString()
        {
            return ProcName;
        }

        
    }
}