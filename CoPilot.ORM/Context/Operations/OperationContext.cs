using System.Collections.Generic;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Context.Operations
{
    public class OperationContext
    {
        public OperationContext()
        {
            Columns = new Dictionary<DbColumn, DbParameter>();
            Args = new Dictionary<string, object>();
        }

        public ITableContextNode Node { get; set; }
        public Dictionary<DbColumn, DbParameter> Columns { get; set; }
        public Dictionary<string, object> Args { get; set; }
    }
}