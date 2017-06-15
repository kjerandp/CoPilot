using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Context.Operations;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Extensions;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Context
{
    public class TableContext<T> : TableContext where T : class
    {
        public TableContext(DbModel model, params string[] include) : base(model, typeof(T), include) { }

        #region selector processing
        public void ApplySelector<TTarget>(Expression<Func<T, TTarget>> selector)
        {
            ApplySelector(selector.Body);
        }

        public void ApplySelector(Expression<Func<T, object>> selector)
        {
            ApplySelector(selector.Body);
        }

        private void ApplySelector(Expression selectorBody)
        {
            var memberExpression = selectorBody as MemberExpression;
            if (memberExpression == null)
            {
                var unaryExpression = selectorBody as UnaryExpression;
                memberExpression = unaryExpression?.Operand as MemberExpression;
            }
            if (memberExpression != null)
            {
                var path = PathHelper.RemoveFirstElementFromPathString(memberExpression.ToString());

                if (!path.Contains("."))
                {
                    var classMemberInfo = ClassMemberInfo.Create(ExpressionHelper.GetPropertyFromMemberExpression<T>(memberExpression));
                    if (classMemberInfo.MemberType.IsCollection())
                    {
                        throw new CoPilotUnsupportedException("Selector cannot return a collection type!");
                    }
                    if (classMemberInfo.MemberType.IsReference())
                    {
                        var dtoMembers = classMemberInfo.MemberType.GetClassMembers();
                        foreach (var memberInfo in dtoMembers)
                        {
                            if (memberInfo.MemberType.IsSimpleValueType())
                            {
                                BuildFromPath(memberInfo.Name, path + "." + memberInfo.Name);
                            }

                        }
                        return;
                    }

                }

                BuildFromPath(memberExpression.Member.Name, path);
                return;
            }

            var members = new Dictionary<string, MemberExpression>();

            var memberInitExpression = selectorBody as MemberInitExpression;
            if (memberInitExpression != null)
            {
                foreach (var binding in memberInitExpression.Bindings.OfType<MemberAssignment>())
                {
                    var member = binding.Expression as MemberExpression;
                    if (member != null)
                    {
                        members.Add(binding.Member.Name, member);
                    }
                    else
                    {
                        throw new CoPilotUnsupportedException("Selector object not supported!");
                    }
                }

            }
            else
            {
                var templateExpression = selectorBody as NewExpression;

                if (templateExpression == null) throw new CoPilotUnsupportedException("Only a new anonymous object with named member references are supported!");

                for (var i = 0; i < templateExpression.Members.Count; i++)
                {
                    memberExpression = templateExpression.Arguments[i] as MemberExpression;
                    if (memberExpression == null)
                    {
                        throw new CoPilotUnsupportedException("Selector object can only contain direct member access!");
                    }
                    members.Add(templateExpression.Members[i].Name, memberExpression);
                }
            }


            BuildFromMemberExpressions(members);
        }
        #endregion


        public OperationContext Insert(T entity)
        {
            return Insert(this, entity);
        }

        public OperationContext Delete(T entity)
        {
            return Delete(this, entity);
        }

        public OperationContext Update(T entity)
        {
            return Update(this, entity);
        }


    }
}
