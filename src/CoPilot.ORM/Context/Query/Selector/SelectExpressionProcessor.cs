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
        private readonly Dictionary<string, ITableContextNode> _sourceNodes;
        public SelectExpressionProcessor(TableContext ctx)
        {
            _ctx = ctx;
            _template = new SelectTemplate();
            _memberPaths = new HashSet<string>();
            _sourceNodes = new Dictionary<string, ITableContextNode>();
        }
         

        public SelectTemplate Decode(Expression expression)
        {
            var lambda = expression as LambdaExpression;
            if (lambda != null)
            {
                _template.ShapeFunc = lambda.Compile();
                ProcessLambdaExpression(lambda, _ctx, "");
            }
            _template.Complete();
            return _template;
        }

        private void ProcessExpression(Expression expr, string joinAlias, string alias)
        {
            switch (expr.NodeType)
            {
                case ExpressionType.New:
                    ProcessNewExpression((NewExpression) expr, joinAlias);
                    break;
                case ExpressionType.MemberInit:
                    ProcessMemberInitExpression((MemberInitExpression)expr, joinAlias);
                    break;
                case ExpressionType.MemberAccess:
                    ProcessMemberExpression((MemberExpression) expr, joinAlias, alias);
                    break;
                case ExpressionType.Call:
                    ProcessMethodCallExpression((MethodCallExpression) expr, joinAlias, alias);
                    break;
                case ExpressionType.Conditional:
                    ProcessConditionalExpression((ConditionalExpression) expr, joinAlias, alias);
                    break;
            }
            var binExpr = expr as BinaryExpression;
            if (binExpr != null)
            {
                ProcessBinaryExpression(binExpr, joinAlias, alias);

            } else {
                var unaryExpr = expr as UnaryExpression;
                if (unaryExpr != null)
                {
                    ProcessUnaryExpression(unaryExpr, joinAlias, alias);
                }
            }
        }

        private void ProcessMemberInitExpression(MemberInitExpression memberInitExpr, string joinAlias)
        {
            foreach (var memberBinding in memberInitExpr.Bindings)
            {
                var binding = memberBinding as MemberAssignment;
                if (binding != null)
                {
                    ProcessExpression(binding.Expression, joinAlias, null);
                }
            }
            ProcessNewExpression(memberInitExpr.NewExpression, joinAlias);
        }

        private void ProcessBinaryExpression(BinaryExpression binExpr, string joinAlias, string alias)
        {
            ProcessExpression(binExpr.Left, joinAlias, alias);
            ProcessExpression(binExpr.Right, joinAlias, alias);
        }

        private void ProcessLambdaExpression(LambdaExpression lambda, ITableContextNode sourceNode, string joinAlias)
        {
            var source = lambda.Parameters[0].Name;
            if (!_sourceNodes.ContainsKey(source))
            {
                _sourceNodes.Add(source, sourceNode);
            }
            ProcessExpression(lambda.Body, joinAlias, null);
  
        }

        private void ProcessUnaryExpression(UnaryExpression unaryExpression, string joinAlias, string alias)
        {
            ProcessExpression(unaryExpression.Operand, joinAlias, alias);
        }

        private void ProcessMemberExpression(MemberExpression memberExpression, string joinAlias, string alias)
        {
            if (!_ctx.Model.IsMapped(memberExpression.Member.DeclaringType))
            {
                if (memberExpression.Expression != null)
                {
                    ProcessExpression(memberExpression.Expression, joinAlias, alias);
                    return;
                }
                throw new CoPilotUnsupportedException("Member expression was not understood!");
            }

            var splitPath = PathHelper.SplitFirstAndLastInPathString(memberExpression.ToString());

            var sourceNode = _sourceNodes[splitPath.Item1];

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

            if (!member.MemberType.IsSimpleValueType())
            {
                node = node.Nodes.ContainsKey(member.Name) ? node.Nodes[member.Name] : _ctx.AddPath(memberPath);

                _template.AddNode(node);
            }
            else
            {
                var column = node.MapEntry.GetColumnByMember(member);
                _template.AddEntry(node, column, member, joinAlias, alias);
            }
            _memberPaths.Add(memberPath);
            //Console.WriteLine("Added "+memberPath);
        }

        public void ProcessNewExpression(NewExpression expr, string joinAlias)
        {
            if (expr.Members != null)
            {
                for (var i = 0; i < expr.Members.Count; i++)
                {
                    ProcessExpression(expr.Arguments[i], joinAlias, expr.Members[i].Name);
                }
            }
            else
            {
                foreach (var arg in expr.Arguments)
                {
                    ProcessExpression(arg, joinAlias, null);
                }
            }
            
        }

        
        private void ProcessConditionalExpression(ConditionalExpression conditionalExpression, string joinAlias, string alias)
        {
            ProcessExpression(conditionalExpression.IfFalse, joinAlias, alias);
            ProcessExpression(conditionalExpression.IfTrue, joinAlias, alias);
        }

        private void ProcessMethodCallExpression(MethodCallExpression methodCallExpression, string joinAlias, string alias)
        {
            if (methodCallExpression.Method.Name.Equals("Select", StringComparison.Ordinal) || methodCallExpression.Method.Name.Equals("SelectMany", StringComparison.Ordinal))
            {
                //if (methodCallExpression.Arguments.Count >= 2)
                //{
                var sourceMemberExpression = methodCallExpression.Arguments[0] as MemberExpression;
                var lambdaExpression = methodCallExpression.Arguments[1] as LambdaExpression;
                if (sourceMemberExpression != null && lambdaExpression != null)
                {
                    var splitPath = PathHelper.SplitFirstInPathString(sourceMemberExpression.ToString());
                    var memberPath = splitPath.Item2;
                    var sourceNode = _sourceNodes[splitPath.Item1];

                    ITableContextNode newNode;
                    if (sourceNode.Nodes.ContainsKey(memberPath))
                    {
                        newNode = sourceNode.Nodes[memberPath];
                    }
                    else
                    {
                        newNode = _ctx.AddPath(
                            PathHelper.Combine(PathHelper.RemoveFirstElementFromPathString(sourceNode.Path), memberPath)
                        );
                    }

                    ProcessLambdaExpression(lambdaExpression, newNode, PathHelper.Combine(joinAlias, alias));
                }
                //}
            }
            else
            {
                if (methodCallExpression.Object != null)
                {
                    ProcessExpression(methodCallExpression.Object, joinAlias, alias);
                }
                foreach (var expression in methodCallExpression.Arguments)
                {
                    ProcessExpression(expression, joinAlias, alias);
                }
            }
        }
    }
}
