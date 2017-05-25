using System.Collections.Generic;
using CoPilot.ORM.Context;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Filtering.Interfaces;
using CoPilot.ORM.Filtering.Operands;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Filtering
{
    public class FilterGraph
    {
        private Dictionary<string, object> _args;
        public BinaryOperand Root { get; set; }
        
        public ContextMemberOperand[] GetMemberExpressions()
        {
            var list = new List<ContextMemberOperand>();
            GetAllMemberExpressions(Root, list);
            return list.ToArray();

        }

        public Dictionary<string, object> GetParameters()
        {
            if (_args == null)
            {
                _args = new Dictionary<string, object>();
                CollectParameters(Root, _args);
            }
            return _args;
        }

        private static void CollectParameters(IExpressionOperand op, Dictionary<string, object> dict)
        {
            var bop = op as BinaryOperand;
            if (bop != null)
            {
                CollectParameters(bop.Left, dict);
                CollectParameters(bop.Right, dict);
            }

            var vop = op as ValueOperand;
            if (vop != null)
            {
                dict.Add(vop.ParamName, vop.Value);
            }

        }

        private static void GetAllMemberExpressions(IExpressionOperand op, List<ContextMemberOperand> items)
        {
            var bop = op as BinaryOperand;
            if (bop != null)
            {
                GetAllMemberExpressions(bop.Left, items);
                GetAllMemberExpressions(bop.Right, items);
            }

            var mop = op as ContextMemberOperand;
            if (mop != null)
            {
                items.Add(mop);
            }
        }


        public override string ToString()
        {
            return Root.ToString();
        }

        internal static FilterGraph CreateChildFilter(TableContextNode node, object[] keys)
        {
            var filter = new FilterGraph();
            var left = new ContextMemberOperand(null) { ContextColumn = new ContextColumn(node, node.GetTargetKey, null) };
            var right = new ValueListOperand("@id", keys);
            filter.Root = new BinaryOperand(left, right, "IN");

            return filter;
        }


        public static FilterGraph CreateByPrimaryKeyFilter(ITableContextNode node, object key)
        {
            var filter = new FilterGraph();
            var left = new ContextMemberOperand(null) { ContextColumn = new ContextColumn(node, node.Table.GetSingularKey(), null) };
            var right = new ValueOperand("@key", key);
            filter.Root = new BinaryOperand(left, right, "=");

            return filter;
        }

        public static FilterGraph CreateChildFilterUsingTempTable(TableContextNode node, string tempTableName)
        {
            var filter = new FilterGraph();
            var left = new ContextMemberOperand(null) { ContextColumn = new ContextColumn(node, node.GetTargetKey, null) };
            var right = new CustomOperand($"(Select [{node.GetSourceKey.ColumnName}] from {tempTableName})");
            filter.Root = new BinaryOperand(left, right, "IN");

            return filter;

        }
    }

    public class CustomOperand : IExpressionOperand
    {
        private readonly string _stm;
        public CustomOperand(string s)
        {
            _stm = s;
        }

        public override string ToString()
        {
            return _stm;
        }
    }
}
