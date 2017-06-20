using System;
using System.Linq;
using System.Reflection;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Extensions;
using CoPilot.ORM.Filtering;
using CoPilot.ORM.Filtering.Interfaces;
using CoPilot.ORM.Filtering.Operands;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Mapping;

namespace CoPilot.ORM.Context.Query.Filter
{
    public class FilterExpressionProcessor
    {
        private readonly TableContext _ctx;

        public FilterExpressionProcessor(TableContext ctx)
        {
            _ctx = ctx;
        }

        public FilterGraph Decode(ExpressionGraph filter)
        {
            var filterGraph = new FilterGraph
            {
                Root = ProcessFilter(filter.Root) as BinaryOperand
            };

            return filterGraph;
        }
        private IExpressionOperand ProcessFilter(IExpressionOperand sourceOp)
        {
            if (sourceOp is UnsupportedOperand) throw new CoPilotUnsupportedException(sourceOp.ToString());

            var bop = sourceOp as BinaryOperand;
            if (bop != null)
            {
                var bin = new BinaryOperand(
                    ProcessFilter(bop.Left),
                    ProcessFilter(bop.Right),
                    bop.Operator
                );

                var memberOperand = bin.Left as MemberExpressionOperand;
                ValueOperand matchingVop;
                if (memberOperand == null)
                {
                    memberOperand = bin.Right as MemberExpressionOperand;
                    matchingVop = bin.Left as ValueOperand;
                }
                else
                {
                    matchingVop = bin.Right as ValueOperand;
                }

                if (matchingVop != null && memberOperand?.ColumnReference.Adapter != null)
                {
                    //special case for enums
                    var member = memberOperand.ColumnReference.Node?.MapEntry?.GetMappedMember(memberOperand.ColumnReference.Column);
                    if (member != null && member.MemberType.GetTypeInfo().IsEnum)
                    {
                        matchingVop.Value = Enum.ToObject(member.MemberType, matchingVop.Value);
                    }
                    matchingVop.Value = memberOperand.ColumnReference.Adapter.Invoke(MappingTarget.Database, matchingVop.Value);
                }
                return bin;
            }

            var mop = sourceOp as MemberExpressionOperand;
            if (mop != null)
            {
                ProcessMemberExpression(mop);
                return mop;
            }

            var vop = sourceOp as ValueOperand;
            if (vop != null)
            {
                return new ValueOperand(vop.ParamName, vop.Value);
            }

            return new NullOperand();
        }

        private void ProcessMemberExpression( MemberExpressionOperand memberExpression)
        {
            var path = memberExpression.Path;
            var splitPath = PathHelper.SplitLastInPathString(path);

            if (!string.IsNullOrEmpty(splitPath.Item1) && !_ctx.Exist(splitPath.Item1))
            {
                _ctx.AddPath(splitPath.Item1, false);
            }

            TableMapEntry mapEntry;
            ITableContextNode node = _ctx;
            if (string.IsNullOrEmpty(splitPath.Item1))
            {
                mapEntry = _ctx.MapEntry;
            }
            else
            {
                node = _ctx.FindByPath(splitPath.Item1);
                mapEntry = node.MapEntry;
            }
            var member = mapEntry.GetMemberByName(splitPath.Item2);

            var adapter = mapEntry.GetAdapter(member);
            var col = mapEntry.GetColumnByMember(member);
            if (col == null && !member.MemberType.IsSimpleValueType())
            {
                var rel = mapEntry.GetRelationshipByMember(member);
                if (rel != null)
                {
                    col = rel.ForeignKeyColumn; //member.MemberType.IsReference() ? rel.ForeignKeyColumn : rel.PrimaryKeyColumn;
                }
            }
            if (col == null) throw new CoPilotRuntimeException("Cannot map expression to a column!");
            if (col.ForeignkeyRelationship != null && col.ForeignkeyRelationship.IsLookupRelationship)
            {
                node = _ctx.GetOrCreateLookupNode(node, col);
                col = col.ForeignkeyRelationship.LookupColumn;

            }
            else if (col.IsPrimaryKey && node.Origin != null)
            {
                node = node.Origin;
                var newCol = node.Table.Columns.FirstOrDefault(r => r.IsForeignKey && r.ForeignkeyRelationship.PrimaryKeyColumn.Equals(col));
                col = newCol;
            }
            else if (col.Table != node.Table)
            {
                node = node.Nodes.Single(r => r.Value.Table == col.Table).Value;
            }

            if (col == null) throw new CoPilotRuntimeException("Column could not found!");

            memberExpression.ColumnReference = ContextColumn.Create(node, col, adapter);
        }
    }
}
