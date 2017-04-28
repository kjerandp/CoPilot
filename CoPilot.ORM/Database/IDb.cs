using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq.Expressions;
using CoPilot.ORM.Common;
using CoPilot.ORM.Context.Query;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Mapping;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Database
{
    public interface IDb
    {
        DbModel Model { get; }      
        SqlConnection Connection { get; }

        DbResponse Query(string commandText, object args, params string[] names);

        IEnumerable<T> Query<T>(string commandText, object args, params string[] names);
        IEnumerable<T> Query<T>(string commandText, object args, ObjectMapper mapper, params string[] names);

        IEnumerable<T> Query<T>(Expression<Func<T, bool>> filter = null, params string[] include) where T : class;
        IEnumerable<T> Query<T>(Predicates predicates, Expression<Func<T, bool>> filter = null, params string[] include) where T : class;
        IEnumerable<T> Query<T>(OrderByClause<T> orderByClause, Expression<Func<T, bool>> filter = null, params string[] include) where T : class;
        IEnumerable<T> Query<T>(OrderByClause<T> orderByClause, Predicates predicates, Expression<Func<T, bool>> filter = null, params string[] include) where T : class;

        IEnumerable<dynamic> Query<TEntity>(Expression<Func<TEntity, object>> selector, Expression<Func<TEntity, bool>> filter = null) where TEntity : class;
        IEnumerable<dynamic> Query<TEntity>(Expression<Func<TEntity, object>> selector, OrderByClause<TEntity> orderByClause, Expression<Func<TEntity, bool>> filter = null) where TEntity : class;
        IEnumerable<dynamic> Query<TEntity>(Expression<Func<TEntity, object>> selector, Predicates predicates, Expression<Func<TEntity, bool>> filter = null) where TEntity : class;
        IEnumerable<dynamic> Query<TEntity>(Expression<Func<TEntity, object>> selector, OrderByClause<TEntity> orderByClause, Predicates predicates, Expression<Func<TEntity, bool>> filter = null) where TEntity : class;
        
        IEnumerable<TDto> Query<TEntity, TDto>(Expression<Func<TEntity, object>> selector, Expression<Func<TEntity, bool>> filter = null) where TEntity : class;
        IEnumerable<TDto> Query<TEntity, TDto>(Expression<Func<TEntity, object>> selector, OrderByClause<TEntity> orderByClause, Expression<Func<TEntity, bool>> filter = null) where TEntity : class;
        IEnumerable<TDto> Query<TEntity, TDto>(Expression<Func<TEntity, object>> selector, Predicates predicates, Expression<Func<TEntity, bool>> filter = null) where TEntity : class;
        IEnumerable<TDto> Query<TEntity, TDto>(Expression<Func<TEntity, object>> selector, OrderByClause<TEntity> orderByClause, Predicates predicates, Expression<Func<TEntity, bool>> filter = null) where TEntity : class;

        T Single<T>(Expression<Func<T, bool>> filter, params string[] include) where T : class;
        TDto Single<TEntity, TDto>(Expression<Func<TEntity, object>> selector, Expression<Func<TEntity, bool>> filter) where TEntity : class;

        int Command(string commandText, object args = null);

        object Scalar(string commandText, object args = null);

        T Scalar<T>(string commandText, object args = null);

        void Save<T>(T entity, params string[] include) where T : class;

        void Save<T>(T entity, OperationType operations, params string[] include) where T : class;

        void Save<T>(IEnumerable<T> entities, params string[] include) where T : class;

        void Save<T>(IEnumerable<T> entities, OperationType operations, params string[] include) where T : class;

        void Patch<T>(object dto) where T : class;

        void Delete<T>(T entity, params string[] include) where T : class;

        void Delete<T>(IEnumerable<T> entities, params string[] include) where T : class;

        bool ValidateModel();
    }
}