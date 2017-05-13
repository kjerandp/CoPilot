using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using CoPilot.ORM.Common;
using CoPilot.ORM.Helpers;

namespace CoPilot.ORM.Context.Query
{
    /// <summary>
    /// Helper class to specify ordering in queries
    /// </summary>
    /// <typeparam name="T">POCO type used as base for the query</typeparam>
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

        /// <summary>
        /// Create order clause for ascending ordering
        /// </summary>
        /// <param name="member">Property to sort by</param>
        /// <returns>Order clause</returns>
        public static OrderByClause<T> OrderByAscending(Expression<Func<T, object>> member) 
        {
            var clause = new OrderByClause<T>();
            clause.Add(member, Ordering.Ascending);
            return clause;
        }
        /// <summary>
        /// Create order clause for decending ordering
        /// </summary>
        /// <param name="member">Property to sort by</param>
        /// <returns>Order clause</returns>
        public static OrderByClause<T> OrderByDecending(Expression<Func<T, object>> member)
        {
            var clause = new OrderByClause<T>();
            clause.Add(member, Ordering.Descending);
            return clause;
        }

        /// <summary>
        /// Create order clause for ascending ordering
        /// </summary>
        /// <param name="path">Name/path of member to sort by</param>
        /// <returns>Order clause</returns>
        public static OrderByClause<T> OrderByAscending(string path) 
        {
            var clause = new OrderByClause<T>();
            clause.Add(path, Ordering.Ascending);
            return clause;
        }
        /// <summary>
        /// Create order clause for decending ordering
        /// </summary>
        /// <param name="path">Name/path of member to sort by</param>
        /// <returns>Order clause</returns>
        public static OrderByClause<T> OrderByDecending(string path)
        {
            var clause = new OrderByClause<T>();
            clause.Add(path, Ordering.Descending);
            return clause;
        }

        /// <summary>
        /// Add additional ascending ordering to clause
        /// </summary>
        /// <param name="member">Property to sort by</param>
        /// <returns>Order clause</returns>
        public OrderByClause<T> ThenByAscending(Expression<Func<T, object>> member)
        {
            Add(member, Ordering.Ascending);
            return this;
        }

        /// <summary>
        /// Add additional decending ordering to clause
        /// </summary>
        /// <param name="member">Property to sort by</param>
        /// <returns>Order clause</returns>
        public OrderByClause<T> ThenByDecending(Expression<Func<T, object>> member)
        {
            Add(member, Ordering.Descending);
            return this;
        }

        /// <summary>
        /// Add additional ascending ordering to clause
        /// </summary>
        /// <param name="path">Name/path of member to sort by</param>
        /// <returns>Order clause</returns>
        public OrderByClause<T> ThenByAscending(string path)
        {
            Add(path, Ordering.Ascending);
            return this;
        }

        /// <summary>
        /// Add additional decending ordering to clause
        /// </summary>
        /// <param name="path">Name/path of member to sort by</param>
        /// <returns>Order clause</returns>
        public OrderByClause<T> ThenByDecending(string path)
        {
            Add(path, Ordering.Descending);
            return this;
        }
        private void Add(Expression<Func<T, object>> member, Ordering ordering)
        {
            var path = ExpressionHelper.GetPathFromExpression(member);
            Add(path, ordering);
        }
        private void Add(string path, Ordering ordering)
        {
            _paths.Add(path, ordering);
        }

    }
}