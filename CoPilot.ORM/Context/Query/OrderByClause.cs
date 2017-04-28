using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using CoPilot.ORM.Common;
using CoPilot.ORM.Helpers;

namespace CoPilot.ORM.Context.Query
{
    public class OrderByClause<T> where T : class
    {
        private readonly Dictionary<string, Ordering> _paths;

        internal Dictionary<string, Ordering> Get()
        {
            return _paths;
        }

        private OrderByClause()
        {
            _paths = new Dictionary<string, Ordering> ();

        }

        public static OrderByClause<T> OrderByAscending(Expression<Func<T, object>> member) //TODO add override with column as string
        {
            var clause = new OrderByClause<T>();
            clause.Add(member, Ordering.Ascending);
            return clause;
        }
        public static OrderByClause<T> OrderByDecending(Expression<Func<T, object>> member)
        {
            var clause = new OrderByClause<T>();
            clause.Add(member, Ordering.Descending);
            return clause;
        }

        public OrderByClause<T> ThenByAscending(Expression<Func<T, object>>  member)
        {
            Add(member, Ordering.Ascending);
            return this;
        }

        public OrderByClause<T> ThenByDecending(Expression<Func<T, object>> member)
        {
            Add(member, Ordering.Descending);
            return this;
        }

        private void Add(Expression<Func<T, object>> member, Ordering ordering)
        {
            var path = ExpressionHelper.GetPathFromExpression(member);
            _paths.Add(path, ordering);
        }
        
    }
}