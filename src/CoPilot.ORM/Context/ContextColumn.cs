using CoPilot.ORM.Config;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Context
{
    public class ContextColumn
    {
        private ContextColumn(ITableContextNode node, DbColumn column, ValueAdapter adapter, string alias = null)
        {
            Node = node;
            Column = column;
            ColumnAlias = alias;
            Adapter = adapter;
        }

        public static ContextColumn Create(ITableContextNode node, DbColumn column, string joinAlias = null,
            string alias = null)
        {
            ValueAdapter adapter = null;
            if (node.MapEntry != null)
            {
                adapter = node.MapEntry.GetAdapter(column);
            }
            return Create(node, column, adapter, joinAlias, alias);
        }

        public static ContextColumn Create(ITableContextNode node, DbColumn column, ValueAdapter adapter, string joinAlias = null, string alias = null)
        {
            
            var givenName = column.ColumnName;

            var selCol = new ContextColumn(node, column, adapter);

            if (column.ForeignkeyRelationship != null && column.ForeignkeyRelationship.IsLookupRelationship)
            {
                selCol.Node = node.Nodes["LOOKUP~" + column.ColumnName];
                selCol.Column = column.ForeignkeyRelationship.LookupColumn;
            }

            var aliasPart = "";

            if (!string.IsNullOrEmpty(alias))
            {
                aliasPart = $"{alias}";
            }
            else if (!string.IsNullOrEmpty(joinAlias))
            {
                aliasPart = $"{joinAlias}.{givenName}";
            }
            else if (givenName != selCol.Column.ColumnName)
            {
                aliasPart = $"{givenName}";
            }
            selCol.ColumnAlias = aliasPart;

            return selCol;
        }

        public ITableContextNode Node { get; set; }

        public DbColumn Column { get; set; }

        public ClassMemberInfo MappedMember => Node.MapEntry?.GetMappedMember(Column);
        
        public ValueAdapter Adapter { get; internal set; }

        public string ColumnAlias { get; set; }
        public string Name => string.IsNullOrEmpty(ColumnAlias) ? Column.ColumnName : ColumnAlias;

        public override string ToString()
        {
            var colName = Column.ColumnName;
            
            var str = $"T{Node.Table.TableName}.{colName}";
            if (!string.IsNullOrEmpty(ColumnAlias))
            {
                str += $" ({ColumnAlias})";
            }
            return str;
        }
    }
}