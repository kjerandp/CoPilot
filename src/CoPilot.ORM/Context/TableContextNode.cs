using System.Collections.Generic;
using System.Linq;
using CoPilot.ORM.Common;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Context.Query;
using CoPilot.ORM.Filtering;
using CoPilot.ORM.Mapping;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Context
{
    public class TableContextNode : ITableContextNode
    {
        public TableContextNode(ITableContextNode origin, DbRelationship relationship, bool isInverted, int index, TableMapEntry mapEntry)
        {
            Index = index;
            Relationship = relationship;
            IsInverted = isInverted;
            Nodes = new Dictionary<string, TableContextNode>();
            Origin = origin;
            MapEntry = mapEntry;
        }

        public DbRelationship Relationship { get; }
        public bool IsInverted { get; }

        public TableJoinType JoinType
        {
            get
            {
                if(Relationship.ForeignKeyColumn.IsNullable) return TableJoinType.LeftJoin;
                var origin = Origin as TableContextNode;
                if(origin != null && origin.JoinType == TableJoinType.LeftJoin)
                    return TableJoinType.LeftJoin;
                return TableJoinType.InnerJoin;
            }
        }

        public int Index { get; }
        public int Level => Origin.Level + 1;
        public int Order => JoinType == TableJoinType.InnerJoin ? 1 : 2;
        public string Path => Origin.Path + "." + Origin.Nodes.Single(r => r.Value.Equals(this)).Key;
        public ITableContextNode Origin { get; set; }
        public TableContext Context => Origin.Context;
        
        public TableMapEntry MapEntry { get; }
        public DbTable Table => IsInverted ? Relationship.ForeignKeyColumn.Table : Relationship.PrimaryKeyColumn.Table;
        public DbColumn GetTargetKey => IsInverted ? Relationship.ForeignKeyColumn : Relationship.PrimaryKeyColumn;
        public DbColumn GetSourceKey => IsInverted ? Relationship.PrimaryKeyColumn : Relationship.ForeignKeyColumn;
        public Dictionary<string, TableContextNode> Nodes { get; }

        public override int GetHashCode()
        {
            return Index.GetHashCode();  
        }

        public override string ToString()
        {
            return $"T{Index} ({Table})";
        }
    }
}
