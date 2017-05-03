using CoPilot.ORM.Config;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Context
{
    public class ContextColumn
    {
        public ContextColumn(ITableContextNode node, DbColumn column, ValueAdapter adapter, string alias = null)
        {
            Node = node;
            Column = column;
            ColumnAlias = alias;
            Adapter = adapter;
        }
        public ITableContextNode Node { get; set; }
        public DbColumn Column { get; set; }

        public ValueAdapter Adapter { get; internal set; }

        public string ColumnAlias { get; set; }

        public override string ToString()
        {
            var colName = Column.ColumnName;
            if (Column.ColumnName.Contains(" ")) colName = "[" + colName + "]";
            var str = $"T{Node.Index}.{colName}";
            if (!string.IsNullOrEmpty(ColumnAlias))
            {
                str += $" as [{ColumnAlias}]";
            }
            return str;
        }
    }
}