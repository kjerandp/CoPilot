using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using CoPilot.ORM.Common;
using CoPilot.ORM.Context;
using CoPilot.ORM.Context.Query;
using CoPilot.ORM.Database.Providers;
using CoPilot.ORM.Filtering;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Mapping.Mappers;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Database.Commands.Query
{
    public class QueryBuilder<T> : IQuery<T>, IOrderedQuery<T> where T : class
    {
        internal TableContext<T> Ctx;

        private readonly IDbProvider _provider;
        private readonly DbModel _model;
        private readonly DbReader _dbReader;
        private Dictionary<string, Ordering> _orderByPaths;
        private SelectModifiers _predicates;
        private Expression<Func<T, bool>> _filterPredicate;
        

        internal QueryBuilder(IDbProvider provider, DbModel model, DbReader dbReader)
        {
            _provider = provider;
            _model = model;
            _dbReader = dbReader;
        }

        public IFilteredQuery<T> Where(Expression<Func<T, bool>> predicate)
        {
            _filterPredicate = predicate;
            return this;
        }

        public IIncludableQuery<T> Select()
        {
            return this;
        }

        public IOrderableQuery<T> Include(params string[] paths)
        {
            Ctx = _model.CreateContext<T>(paths);
            return this;
        }

        public IOrderableQuery<T> Select(params string[] include)
        {
            Ctx = _model.CreateContext<T>(include);
            return this;
        }

        public IOrderableQuery<T,TTarget> Select<TTarget>(Expression<Func<T, TTarget>> selector)
        {
            Ctx = _model.CreateContext<T>();
            Ctx.ApplySelector(selector);
            return new QueryBuilder<T,TTarget>(this);
        }

        internal void AddToOrderBy(string path, Ordering ordering)
        {
            if(_orderByPaths == null)
                _orderByPaths = new Dictionary<string, Ordering>();

            _orderByPaths.Add(path, ordering);
        }

        public IOrderedQuery<T> OrderBy(string path, Ordering ordering = Ordering.Ascending)
        {
            AddToOrderBy(path, ordering);
            return this;
        }

        public IOrderedQuery<T> OrderBy(Expression<Func<T, object>> member, Ordering ordering = Ordering.Ascending)
        {
            var path = ExpressionHelper.GetPathFromExpression(member);
            AddToOrderBy(path, ordering);
            return this;
        }

        public IOrderedQuery<T> ThenBy(string path, Ordering ordering = Ordering.Ascending)
        {
            AddToOrderBy(path, ordering);
            return this;
        }

        public IOrderedQuery<T> ThenBy(Expression<Func<T, object>> member, Ordering ordering = Ordering.Ascending)
        {
            var path = ExpressionHelper.GetPathFromExpression(member);
            AddToOrderBy(path, ordering);
            return this;
        }

        public IPreparedQuery<T> Take(int take)
        {
            if (_predicates == null)
            {
                _predicates = new SelectModifiers {Take = take};
            }
            else
            {
                _predicates.Take = take;
            }
            return this;
        }

        public IPreparedQuery<T> Skip(int skip)
        {
            if (_predicates == null)
            {
                _predicates = new SelectModifiers { Skip = skip };
            }
            else
            {
                _predicates.Skip = skip;
            }
            return this;
        }

        public IPreparedQuery<T> Distinct()
        {
            if (_predicates == null)
            {
                _predicates = new SelectModifiers { Distinct = true};
            }
            else
            {
                _predicates.Distinct = true;
            }
            return this;
        }

        public T Single()
        {
            return AsEnumerable().SingleOrDefault();
        }

        public T[] ToArray()
        {
            return AsEnumerable().ToArray();
        }
        

        public IEnumerable<T> AsEnumerable()
        {
            return Execute().OfType<T>();
        }

        internal void UpdateContext()
        {
            if (_filterPredicate != null)
            {
                var decoder = new ExpressionDecoder(_provider);
                var filter = decoder.Decode(_filterPredicate.Body);
                Ctx.ApplyFilter(filter);
            }

            if (_orderByPaths != null)
            {
                Ctx.ApplyOrdering(_orderByPaths);
            }
            if (_predicates != null)
            {
                Ctx.SetQueryPredicates(_predicates);
            }
        }

        internal IEnumerable<object> Execute()
        {
            if (Ctx == null)
                Ctx = _model.CreateContext<T>();

            UpdateContext();
            var rootFilter = Ctx.GetFilter();
            return _provider.QueryStrategySelector(Ctx).Execute(Ctx, rootFilter, _dbReader);
        }

        internal IEnumerable<TTarget> Execute<TTarget>()
        {
            UpdateContext();
            var mapper = (typeof(TTarget) == typeof(object) || typeof(TTarget) == typeof(IDictionary<string, object>)) ?
                    DynamicMapper.Create() :
                    SelectTemplateMapper.Create(Ctx, typeof(TTarget));
            return _dbReader.Query(Ctx).Map<TTarget>(mapper);
        }
    }

    public class QueryBuilder<T, TTarget> : IOrderableQuery<T, TTarget>, IOrderedQuery<T, TTarget> where T : class
    {
        private readonly QueryBuilder<T> _baseBuilder;

        internal QueryBuilder(QueryBuilder<T> baseBuilder)
        {
            _baseBuilder = baseBuilder;
        }

        public IPreparedQuery<T, TTarget> Take(int take)
        {
            _baseBuilder.Take(take);
            return this;
        }

        public IPreparedQuery<T, TTarget> Skip(int skip)
        {
            _baseBuilder.Skip(skip);
            return this;
        }

        public IPreparedQuery<T, TTarget> Distinct()
        {
            _baseBuilder.Distinct();
            return this;
        }

        public IOrderedQuery<T, TTarget> OrderBy(string path, Ordering ordering = Ordering.Ascending)
        {
            _baseBuilder.AddToOrderBy(path, ordering);
            return this;
        }

        public IOrderedQuery<T, TTarget> OrderBy(Expression<Func<TTarget, object>> member, Ordering ordering = Ordering.Ascending)
        {
            var path = GetPathFromExpression(member);
            
            _baseBuilder.AddToOrderBy(path, ordering);
            return this;
        }

        private string GetPathFromExpression(Expression<Func<TTarget, object>> member)
        {
            if (member.NodeType == ExpressionType.Lambda)
            {
                var primExpr = member.Body as ParameterExpression;
                if (primExpr != null)
                {
                    return PathHelper.RemoveFirstElementFromPathString(primExpr.Name);
                }
                var memExpr = member.Body as MemberExpression;
                if (memExpr != null)
                {
                    return memExpr.Member.Name;
                }

                var constExpr = member.Body as ConstantExpression;
                return constExpr != null ? constExpr.Value.ToString() : "1";
                
            }
            return ExpressionHelper.GetPathFromExpression(member);
        }

        public IOrderedQuery<T, TTarget> ThenBy(string path, Ordering ordering = Ordering.Ascending)
        {
            _baseBuilder.AddToOrderBy(path, ordering);
            return this;
        }

        public IOrderedQuery<T, TTarget> ThenBy(Expression<Func<TTarget, object>> member, Ordering ordering = Ordering.Ascending)
        {
            var path = GetPathFromExpression(member);
            _baseBuilder.AddToOrderBy(path, ordering);
            return this;
        }

        public TTarget Single()
        {
            return AsEnumerable().SingleOrDefault();
        }

        public TTarget[] ToArray()
        {
            return AsEnumerable().ToArray();
        }

        public IEnumerable<TTarget> AsEnumerable()
        {
            var result = _baseBuilder.Execute<TTarget>();
            return result;
        } 
    }
    
}
