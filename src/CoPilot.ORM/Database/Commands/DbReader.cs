using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using CoPilot.ORM.Context;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Database.Commands.SqlWriters.Interfaces;
using CoPilot.ORM.Filtering;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Mapping.Mappers;
using CoPilot.ORM.Mapping;
using CoPilot.ORM.Model;
using CoPilot.ORM.Context.Query;
using CoPilot.ORM.Database.Commands.Query.Interfaces;

namespace CoPilot.ORM.Database.Commands
{
    public class DbReader : IDisposable
    {
        private readonly SqlCommand _sqlCommand;
        private readonly SqlConnection _sqlConnection;
        private readonly DbModel _model;
        private readonly ISelectStatementWriter _writer;
        private readonly IQueryBuilder _builder;
        private readonly IQueryStrategySelector _strategySelector;
        public DbReader(IDb db) : this(db.Connection, db.Model) { }

        internal DbReader(SqlCommand command, DbModel model) : this(model)
        {
            _model = model;
            _sqlCommand = command;
            
            _sqlConnection = null;

        }

        internal DbReader(SqlConnection connection, DbModel model) : this(model)
        {
            _model = model;
            _sqlConnection = connection;
            _sqlCommand = new SqlCommand()
            {
                Connection = _sqlConnection,
                CommandTimeout = 0
            };
            _sqlConnection.Open();

            
        }

        private DbReader(DbModel model)
        {
            _writer = model.ResourceLocator.Get<ISelectStatementWriter>();
            _builder = model.ResourceLocator.Get<IQueryBuilder>();
            _strategySelector = model.ResourceLocator.Get<IQueryStrategySelector>();
        }

        public object FindByKey(ITableContextNode node, object key)
        {
            var strategy = _strategySelector.Get(node.Context);
            var filter = FilterGraph.CreateByPrimaryKeyFilter(node, key);
            var item = strategy.Execute(node, filter, this).SingleOrDefault();
            return item;
        }

        public object FindByKey(Type type, object key, params string[] include)
        {
            var ctx = _model.CreateContext(type, include);
            return FindByKey(ctx, key);
        }

        public T FindByKey<T>(object key, params string[] include) where T : class
        {
            return FindByKey(typeof(T), key, include) as T;
        }

        public DbResponse Query(string commandText, object args, params string[] names)
        {
            var request = DbRequest.CreateRequest(_model, commandText, args);
            var response = CommandExecutor.ExecuteQuery(_sqlCommand, request, names);
            
            return response;
        }

        public DbResponse Query(DbRequest request, params string[] names)
        {
            return CommandExecutor.ExecuteQuery(_sqlCommand, request, names);
        }

        public IEnumerable<T> Query<T>(string commandText, object args, params string[] names)
        {
            return Query<T>(commandText, args, null, names);
        }

        public IEnumerable<T> Query<T>(string commandText, object args, ObjectMapper mapper = null, params string[] names)
        {
            var response = Query(commandText, args, names);

            if (mapper == null && _model.IsMapped(typeof(T)))
            {
                var ctx = _model.CreateContext(typeof(T), response.GetPaths());
                return ContextMapper.MapAndMerge(ctx, response.RecordSets).OfType<T>();
            }

            return response.Map<T>(mapper);
        }

        public IEnumerable<T> Query<T>(Expression<Func<T, bool>> filter = null, params string[] include) where T : class
        {
            return Query(null, null, filter, include);
        }

        public IEnumerable<T> Query<T>(Predicates predicates, Expression<Func<T, bool>> filter = null, params string[] include) where T : class
        {
            return Query(null, predicates, filter, include);
        }

        public IEnumerable<T> Query<T>(OrderByClause<T> orderBy, Expression<Func<T, bool>> filter = null, params string[] include) where T : class
        {
            return Query(orderBy, null, filter, include);
        }

        public IEnumerable<T> Query<T>(OrderByClause<T> orderBy, Predicates predicates, Expression<Func<T, bool>> filter = null, params string[] include) where T : class
        {
            var ctx = CreateContext(orderBy, predicates, filter, include);
            var rootFilter = ctx.GetFilter();
            _sqlCommand.CommandType = CommandType.Text;

            return _strategySelector.Get(ctx).Execute(ctx, rootFilter, this).OfType<T>();
        }

        public IEnumerable<dynamic> Query<TEntity>(Expression<Func<TEntity, object>> selector, Expression<Func<TEntity, bool>> filter = null) where TEntity : class
        {
            return Query(selector, null, null, filter);
        }

        public IEnumerable<dynamic> Query<TEntity>(Expression<Func<TEntity, object>> selector, OrderByClause<TEntity> orderByClause, Expression<Func<TEntity, bool>> filter = null) where TEntity : class
        {
            return Query(selector, orderByClause, null, filter);
        }

        public IEnumerable<dynamic> Query<TEntity>(Expression<Func<TEntity, object>> selector, Predicates predicates, Expression<Func<TEntity, bool>> filter = null) where TEntity : class
        {
            return Query(selector, null, predicates, filter);
        }

        public IEnumerable<dynamic> Query<TEntity>(Expression<Func<TEntity, object>> selector, OrderByClause<TEntity> orderByClause, Predicates predicates,
            Expression<Func<TEntity, bool>> filter = null) where TEntity : class
        {
            return Query<TEntity, object>(selector, orderByClause, predicates, filter);
        }

        public IEnumerable<TDto> Query<TEntity, TDto>(Expression<Func<TEntity, object>> selector, OrderByClause<TEntity> orderByClause, Predicates predicates, Expression<Func<TEntity, bool>> filter = null) where TEntity : class
        {
            var ctx = CreateContext(selector, orderByClause, predicates, filter);
            var rootFilter = ctx.GetFilter();
            var stm = GetStatement(ctx, rootFilter);
            var queryResult = CommandExecutor.ExecuteQuery(_sqlCommand, stm, ctx.Path);
            _sqlCommand.CommandType = CommandType.Text;
            return queryResult.Map<TDto>();
        }

        public IEnumerable<TDto> Query<TEntity, TDto>(Expression<Func<TEntity, object>> selector, OrderByClause<TEntity> orderByClause, Expression<Func<TEntity, bool>> filter = null) where TEntity : class
        {
            return Query<TEntity, TDto>(selector, orderByClause, null, filter);
        }

        public IEnumerable<TDto> Query<TEntity, TDto>(Expression<Func<TEntity, object>> selector, Predicates predicates, Expression<Func<TEntity, bool>> filter = null) where TEntity : class
        {
            return Query<TEntity, TDto>(selector, null, predicates, filter);
        }

        public IEnumerable<TDto> Query<TEntity, TDto>(Expression<Func<TEntity, object>> selector, Expression<Func<TEntity, bool>> filter = null) where TEntity : class
        {
            return Query<TEntity, TDto>(selector, null, null, filter);
        }

        public T Single<T>(Expression<Func<T, bool>> filter, params string[] include) where T : class
        {
            return Query(filter, include).SingleOrDefault();
        }

        public TDto Single<TEntity, TDto>(Expression<Func<TEntity, object>> selector, Expression<Func<TEntity, bool>> filter) where TEntity : class
        {
            return Query<TEntity, TDto>(selector, filter).SingleOrDefault();
        }

        private SqlStatement GetStatement(ITableContextNode node, FilterGraph filter)
        {
            var q = node.Context.GetQueryContext(node, filter);
            return q.GetStatement(_builder, _writer);
        }

        private TableContext<T> CreateContext<T>(Expression<Func<T, bool>> filter = null, params string[] include) where T : class
        {
            var ctx = _model.CreateContext<T>(include);

            if (filter != null)
            {
                var expression = ExpressionHelper.DecodeExpression(filter);
                ctx.ApplyFilter(expression);
            }

            return ctx;
        }

        private TableContext<T> CreateContext<T>(OrderByClause<T> orderBy, Predicates predicates, Expression<Func<T, bool>> filter = null, params string[] include) where T : class
        {
            var ctx = CreateContext(filter, include);
            ApplyToContext(ctx, orderBy, predicates);

            return ctx;
        }

        private TableContext<T> CreateContext<T>(Expression<Func<T, object>> selector, OrderByClause<T> orderBy,Predicates predicates, Expression<Func<T, bool>> filter = null) where T : class
        {
            var ctx = CreateContext(filter);

            ctx.ApplySelector(selector);
            ApplyToContext(ctx, orderBy, predicates);

            return ctx;
        }

        private static void ApplyToContext<T>(TableContext ctx, OrderByClause<T> orderBy, Predicates predicates) where T : class
        {
            if (orderBy != null)
            {
                ctx.ApplyOrdering(orderBy.Get());
            }
            if (predicates != null)
            {
                ctx.SetQueryPredicates(predicates);
            }
        }

        public void Dispose()
        {
            if (_sqlConnection != null)
            {
                _sqlConnection.Close();
                _sqlConnection.Dispose();
            }
            _sqlCommand.Dispose();
        }

        
    }
}