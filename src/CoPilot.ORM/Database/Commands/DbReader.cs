using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using CoPilot.ORM.Context;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Context.Query;
using CoPilot.ORM.Filtering;
using CoPilot.ORM.Mapping.Mappers;
using CoPilot.ORM.Mapping;
using CoPilot.ORM.Model;
using CoPilot.ORM.Database.Commands.Query;
using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Database.Providers;
using CoPilot.ORM.Helpers;

namespace CoPilot.ORM.Database.Commands
{
    public class DbReader : IDisposable, IQueryBuilder
    {
        private readonly IDbCommand _command;
        private readonly IDbConnection _connection;
        private readonly IDbProvider _provider;

        public QueryStrategySelector QueryStrategySelector { get; }

        private readonly DbModel _model;

        public DbReader(IDb db) : this(db.DbProvider, db.CreateConnection(), db.Model){}

        public DbReader(IDbProvider provider, IDbConnection connection, DbModel model) : this(provider, connection.CreateCommand(), model)
        {
            _connection = connection;
        }

        internal DbReader(IDbProvider provider, IDbCommand command, DbModel model)
        {
            _model = model;
            _command = command;
            _command.CommandTimeout = 0;
            _provider = provider;
            
            QueryStrategySelector = new DefaultQueryExecutionStrategy(_provider).Get();
        }

        public object FindByKey(ITableContextNode node, object key)
        {
            var strategy = QueryStrategySelector(node.Context);
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

        public IQuery<T> From<T>() where T : class
        {
            return new QueryBuilder<T>(_provider, _model, this);
        }

        public DbResponse Query(string commandText, object args, params string[] names)
        {
            var request = DbRequest.CreateRequest(_model, commandText, args);
            request.Command = _command;
            var response = _provider.ExecuteQuery(request, names);
            
            return response;
        }

        public DbResponse Query(DbRequest request, params string[] names)
        {
            request.Command = _command;
            return _provider.ExecuteQuery(request, names);
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
                return ContextMapper.MapAndMerge<T>(SelectTemplate.BuildFrom(ctx), response.RecordSets);
            }

            return response.Map<T>(mapper);
        }

        public IEnumerable<T> Query<T>(Expression<Func<T, bool>> filter = null, params string[] include) where T : class
        {
            var ctx = CreateContext(filter, include);

            var rootFilter = ctx.GetFilter();
            _command.CommandType = CommandType.Text;

            return QueryStrategySelector(ctx).Execute(ctx, rootFilter, this).OfType<T>();
        }

        public IEnumerable<TDto> Query<TEntity, TDto>(Expression<Func<TEntity, object>> selector, Expression<Func<TEntity, bool>> filter = null) where TEntity : class
        {
            var ctx = CreateContext(filter);

            ctx.ApplySelector(selector);

            var queryResult = Query(ctx);
            
            return queryResult.Map<TDto>();
        }

        public DbResponse Query(TableContext ctx)
        {
            var filter = ctx.GetFilter();
            var stm = GetStatement(ctx, filter);
            _command.CommandType = CommandType.Text;
            stm.Command = _command;

            return _provider.ExecuteQuery(stm, ctx.Path);
        }

        public T Single<T>(Expression<Func<T, bool>> filter, params string[] include) where T : class
        {
            return Query(filter, include).SingleOrDefault();
        }

        public TDto Single<TEntity, TDto>(Expression<Func<TEntity, object>> selector, Expression<Func<TEntity, bool>> filter) where TEntity : class
        {
            return Query<TEntity, TDto>(selector, filter).SingleOrDefault();
        }

        /// <summary>
        /// Same as Scalar-method in the IDb interface <seealso cref="IDb.Scalar"/>
        /// </summary>
        /// <param name="commandText">Parameterized sql statement</param>
        /// <param name="args">Anonymous object for passing arguments to named parameters</param>
        /// <returns>Scalar value</returns>
        public object Scalar(string commandText, object args = null)
        {
            var request = DbRequest.CreateRequest(_model, commandText, args);
            request.Command = _command;
            return _provider.ExecuteScalar(request);
        }

        /// <summary>
        /// Same as Scalar-method in the IDb interface <seealso cref="IDb.Scalar"/>
        /// </summary>
        /// <param name="commandText">Parameterized sql statement</param>
        /// <param name="args">Anonymous object for passing arguments to named parameters</param>
        /// <returns>Scalar value converted to type of T</returns>
        public T Scalar<T>(string commandText, object args = null)
        {
            object convertedValue;
            ReflectionHelper.ConvertValueToType(typeof(T), Scalar(commandText, args), out convertedValue);

            return (T)convertedValue;
        }


        private SqlStatement GetStatement(ITableContextNode node, FilterGraph filter)
        {
            var q = QueryContext.Create(node, filter);
            return q.GetStatement(_provider.SelectStatementBuilder, _provider.SelectStatementWriter);
        }

        private TableContext<T> CreateContext<T>(Expression<Func<T, bool>> filter = null, params string[] include) where T : class
        {
            var ctx = _model.CreateContext<T>(include);

            if (filter != null)
            {
                var decoder = new ExpressionDecoder(_provider);
                var expression = decoder.Decode(filter.Body);
                ctx.ApplyFilter(expression);
            }

            return ctx;
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.Close();
                _connection.Dispose();
            }
            _command.Dispose();
        }

        
    }
}