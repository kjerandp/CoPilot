using CoPilot.ORM.Common;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Context
{
    public struct TableJoinDescription
    {
        internal TableJoinDescription(FromListItem item)
        {
            var join = item.Node as TableContextNode;
            if(join == null) throw new CoPilotUnsupportedException("Root node cannot be part of the joined tables list!");
            TargetTableIndex = join.Index;
            SourceTableIndex = join.Origin.Index;
            if (join.IsInverted)
            {
                TargetKey = join.Relationship.ForeignKeyColumn;
                SourceKey = join.Relationship.PrimaryKeyColumn;
                JoinType = item.ForceInnerJoin ? TableJoinType.InnerJoin : TableJoinType.LeftJoin;
            }
            else
            {
                TargetKey = join.Relationship.PrimaryKeyColumn;
                SourceKey = join.Relationship.ForeignKeyColumn;
                JoinType = item.ForceInnerJoin ? TableJoinType.InnerJoin : join.JoinType;
            }
            
        }
        
        public TableJoinType JoinType { get; }
        public int TargetTableIndex { get; }
        public int SourceTableIndex { get; }
        public DbColumn TargetKey { get; set; }
        public DbColumn SourceKey { get; set; }


    }
}