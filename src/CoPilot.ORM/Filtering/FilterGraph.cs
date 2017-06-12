using System.Collections.Generic;
using CoPilot.ORM.Common;
using CoPilot.ORM.Context;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Filtering.Interfaces;
using CoPilot.ORM.Filtering.Operands;

namespace CoPilot.ORM.Filtering
{
    public class FilterGraph
    {
        private Dictionary<string, object> _args;
        private List<DbParameter> _parameters;
        private List<MemberExpressionOperand> _contextMembers;
        public BinaryOperand Root { get; set; }


        public MemberExpressionOperand[] MemberExpressions
        {
            get
            {
                if (_contextMembers == null) Collect();
                return _contextMembers?.ToArray();

            }
        }
        public DbParameter[] Parameters
        {
            get
            {
                if (_parameters == null) Collect();
                return _parameters?.ToArray();

            }
        }

        public Dictionary<string, object> Arguments
        {
            get
            {
                if (_args == null) Collect();
                return _args;

            }
        }

        private void Collect()
        {
            _contextMembers = new List<MemberExpressionOperand>();
            _parameters = new List<DbParameter>();
            _args = new Dictionary<string, object>();

            Collect(Root);
        }

        private void Collect(IExpressionOperand op)
        {
            var bop = op as BinaryOperand;
            if (bop != null)
            {
                Collect(bop.Left);
                Collect(bop.Right);
            }

            var mop = op as MemberExpressionOperand;
            if (mop != null)
            {
                _contextMembers.Add(mop);
            }

            var vop = op as ValueOperand;
            if (vop != null)
            {
                _parameters.Add(vop.GetParameter());
                _args.Add(vop.ParamName, vop.Value);
            }
        }

        
        public override string ToString()
        {
            return Root.ToString();
        }

        
        public static FilterGraph CreateByPrimaryKeyFilter(ITableContextNode node, object key)
        {
            var filter = new FilterGraph();
            var left = new MemberExpressionOperand(new ContextColumn(node, node.Table.GetSingularKey(), null));
            var right = new ValueOperand("@key", key);
            filter.Root = new BinaryOperand(left, right, SqlOperator.Equal);

            return filter;
        }

        
    }
}
