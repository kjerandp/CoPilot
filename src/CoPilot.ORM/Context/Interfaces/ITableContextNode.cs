using System.Collections.Generic;
using CoPilot.ORM.Mapping;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Context.Interfaces
{
    public interface ITableContextNode
    {
        ITableContextNode Origin { get; }
        Dictionary<string, TableContextNode> Nodes { get; }
        DbTable Table { get; }
        TableMapEntry MapEntry { get; }
        int Index { get; }
        int Level { get; }
        int Order { get; }
        string Path { get; }
        TableContext Context { get; }
    }
}