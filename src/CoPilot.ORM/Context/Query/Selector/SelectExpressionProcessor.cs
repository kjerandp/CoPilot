using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Extensions;
using CoPilot.ORM.Helpers;

namespace CoPilot.ORM.Context.Query.Selector
{
    public class SelectExpressionProcessor
    {
        private readonly TableContext _ctx;
        private readonly SelectTemplate _template;
        private readonly HashSet<string> _memberPaths;
        public SelectExpressionProcessor(TableContext ctx)
        {
            _ctx = ctx;
            _template = new SelectTemplate();
            _memberPaths = new HashSet<string>();
        }
         

        public SelectTemplate Decode(Expression expression)
        {
            var lambda = expression as LambdaExpression;
            if (lambda != null)
            {
                _template.ShapeFunc = lambda.Compile();
                ProcessLambdaExpression(lambda, null, "");
            }
            _template.Complete();
            return _template;
        }

        private void ProcessExpression(Expression expr, ITableContextNode sourceNode, string joinAlias, string alias)
        {
            switch (expr.NodeType)
            {
                case ExpressionType.Lambda:
                    ProcessLambdaExpression((LambdaExpression) expr, sourceNode, joinAlias);
                    break;
                case ExpressionType.New:
                    ProcessNewExpression((NewExpression) expr, sourceNode, joinAlias);
                    break;
                case ExpressionType.MemberInit:
                    ProcessMemberInitExpression((MemberInitExpression)expr, sourceNode, joinAlias);
                    break;
                case ExpressionType.MemberAccess:
                    ProcessMemberExpression((MemberExpression) expr, sourceNode, joinAlias, alias);
                    break;
                case ExpressionType.Call:
                    ProcessMethodCallExpression((MethodCallExpression) expr, sourceNode, joinAlias, alias);
                    break;
                case ExpressionType.Conditional:
                    ProcessConditionalExpression((ConditionalExpression) expr, sourceNode, joinAlias, alias);
                    break;
            }
            var binExpr = expr as BinaryExpression;
            if (binExpr != null)
            {
                ProcessBinaryExpression(binExpr, sourceNode, joinAlias, alias);

            } else {
                var unaryExpr = expr as UnaryExpression;
                if (unaryExpr != null)
                {
                    ProcessUnaryExpression(unaryExpr, sourceNode, joinAlias, alias);
                }
            }
        }

        private void ProcessMemberInitExpression(MemberInitExpression memberInitExpr, ITableContextNode sourceNode, string joinAlias)
        {
            foreach (var memberBinding in memberInitExpr.Bindings)
            {
                var binding = memberBinding as MemberAssignment;
                if (binding != null)
                {
                    ProcessExpression(binding.Expression, sourceNode, joinAlias, null);
                }
            }
            ProcessNewExpression(memberInitExpr.NewExpression, sourceNode, joinAlias);
        }

        private void ProcessBinaryExpression(BinaryExpression binExpr, ITableContextNode sourceNode, string joinAlias, string alias)
        {
            ProcessExpression(binExpr.Left, sourceNode, joinAlias, alias);
            ProcessExpression(binExpr.Right, sourceNode, joinAlias, alias);
        }

        private void ProcessLambdaExpression(LambdaExpression lambda, ITableContextNode sourceNode, string joinAlias)
        {
            if (sourceNode == null)
            {
                sourceNode = _ctx;
            }

            ProcessExpression(lambda.Body, sourceNode, joinAlias, null);
  
        }

        private void ProcessUnaryExpression(UnaryExpression unaryExpression, ITableContextNode sourceNode, string joinAlias, string alias)
        {
            ProcessExpression(unaryExpression.Operand, sourceNode, joinAlias, alias);
        }

        private void ProcessMemberExpression(MemberExpression memberExpression, ITableContextNode sourceNode, string joinAlias, string alias)
        {
            if (!_ctx.Model.IsMapped(memberExpression.Member.DeclaringType))
            {
                if (memberExpression.Expression != null)
                {
                    ProcessExpression(memberExpression.Expression, sourceNode, joinAlias, alias);
                    return;
                }
                throw new CoPilotUnsupportedException("Member expression was not understood!");
            }

            var splitPath = PathHelper.SplitFirstAndLastInPathString(memberExpression.ToString());
            
            var nodePath = PathHelper.Combine(sourceNode.Path, splitPath.Item2);
            var node = sourceNode;
            var path = PathHelper.RemoveFirstElementFromPathString(nodePath);
            
            if (!nodePath.Equals(sourceNode.Path))
            {
                node = _ctx.FindByPath(path) ?? _ctx.AddPath(path);
            }

            var member = node.MapEntry.GetMemberByName(splitPath.Item3);
            var memberPath = PathHelper.Combine(path, member.Name);
            if (_memberPaths.Contains(memberPath)) return;

            if (member.MemberType.IsReference())
            {
                if (node.Nodes.ContainsKey(member.Name))
                {
                    node = node.Nodes[member.Name];
                }
                else
                {
                    node = _ctx.AddPath(memberPath);
                }
                
                _template.AddNode(node);
            }
            else
            {
                var column = node.MapEntry.GetColumnByMember(member);
                _template.AddEntry(node, column, member, joinAlias, alias);
            }
            _memberPaths.Add(memberPath);
            Console.WriteLine("Added "+memberPath);
        }

        public void ProcessNewExpression(NewExpression expr, ITableContextNode sourceNode, string joinAlias)
        {
            if (expr.Members != null)
            {
                for (var i = 0; i < expr.Members.Count; i++)
                {
                    ProcessExpression(expr.Arguments[i], sourceNode, joinAlias, expr.Members[i].Name);
                }
            }
            else
            {
                foreach (var arg in expr.Arguments)
                {
                    ProcessExpression(arg, sourceNode, joinAlias, null);
                }
            }
            
        }

        
        private void ProcessConditionalExpression(ConditionalExpression conditionalExpression, ITableContextNode sourceNode, string joinAlias, string alias)
        {
            ProcessExpression(conditionalExpression.IfFalse, sourceNode, joinAlias, alias);
            ProcessExpression(conditionalExpression.IfTrue, sourceNode, joinAlias, alias);
        }

        private void ProcessMethodCallExpression(MethodCallExpression methodCallExpression, ITableContextNode sourceNode, string joinAlias, string alias)
        {
            if (methodCallExpression.Method.Name.Equals("Select", StringComparison.Ordinal))
            {
                //if (methodCallExpression.Arguments.Count >= 2)
                //{
                var sourceMemberExpression = methodCallExpression.Arguments[0] as MemberExpression;
                var lambdaExpression = methodCallExpression.Arguments[1] as LambdaExpression;
                if (sourceMemberExpression != null && lambdaExpression != null)
                {
                    var memberPath = PathHelper.RemoveFirstElementFromPathString(sourceMemberExpression.ToString());
                    ITableContextNode newNode;
                    if (sourceNode.Nodes.ContainsKey(memberPath))
                    {
                        newNode = sourceNode.Nodes[memberPath];
                    }
                    else
                    {
                        newNode =
                            _ctx.AddPath(PathHelper.Combine(
                                PathHelper.RemoveFirstElementFromPathString(sourceNode.Path), memberPath));
                    }

                    ProcessLambdaExpression(lambdaExpression, newNode, PathHelper.Combine(joinAlias, alias));
                }
                //}
            }
            else
            {
                if (methodCallExpression.Object != null)
                {
                    ProcessExpression(methodCallExpression.Object, sourceNode, joinAlias, alias);
                }
                foreach (var expression in methodCallExpression.Arguments)
                {
                    ProcessExpression(expression, sourceNode, joinAlias, alias);
                }
            }
        }
    }
}
