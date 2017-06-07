using CoPilot.ORM.Context.Interfaces;

namespace CoPilot.ORM.Context
{
    internal struct FromListItem
    {
        internal FromListItem(ITableContextNode node, bool forceInnerJoin)
        {
            Node = node;
            ForceInnerJoin = forceInnerJoin;
        }
        internal ITableContextNode Node { get; }
        internal bool ForceInnerJoin { get; }

        public override int GetHashCode()
        {
            return Node.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                var other = (FromListItem)obj;
                return Node.Equals(other.Node);
            }
            return false;

        }

        public override string ToString()
        {

            var tblName = Node.Table.TableName;
            //if (tblName.Contains(" ")) tblName = "[" + tblName + "]";
            return $"{tblName} <T{Node.Index}>";
        }
    }
}
