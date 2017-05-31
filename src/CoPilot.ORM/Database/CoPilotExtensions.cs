using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using CoPilot.ORM.Common;
using CoPilot.ORM.Context.Query;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Filtering;
using CoPilot.ORM.Helpers;

namespace CoPilot.ORM.Database
{
    public static class CoPilotExtensions
    {
        /// <summary>
        /// Query database writing a parameterized query statement or name of stored procedure
        /// </summary>
        /// <typeparam name="T">Type to map results to using default mapper. <remarks>Specify a POCO class for using basic mapper or 'dynamic' for dynamic mapper</remarks></typeparam>
        /// <param name="db"></param>
        /// <param name="commandText">Query statement or stored procedure name.<remarks>Name paramters with @-sign followed by name, ex: @id or @firstName</remarks></param>
        /// <param name="args">Anonymous object containing values for parameters in the commandText or the parameters defined in the stored procedure. 
        /// <remarks>For stored procedures that have its parameters mapped, you can pass an object instance as long as it has matching properties for all required parameters</remarks></param>
        /// <param name="names">Use to name resultsets returned (optional)</param>
        /// <returns>Query result mapped to an IEnumerable of type T</returns>
        public static IEnumerable<T> Query<T>(this IDb db, string commandText, object args, params string[] names)
        {
            return db.Query<T>(commandText, args, null, names);
        }

        /// <summary>
        /// Query database using a mapped POCO class to set the context. Sql statement and mapping will be fully handled by CoPilot
        /// </summary>
        /// <typeparam name="T">Mapped POCO class to set the context. <remarks>POCO class must be mapped using the DbMapper class</remarks></typeparam>
        /// <param name="db"></param>
        /// <param name="filter">Filterexpression that will be translated into a WHERE clause (can be null).
        /// <remarks>Basic support for method calls on properties mapping to columns. <see cref="ExpressionDecoderConfig.MemberMethodCallConverter"/> and <see cref="ExpressionDecoderConfig"/></remarks></param>
        /// <param name="include">Array of named navigation properties that reference other mapped POCO classes. This will include data from related entities based on the configured relationships. 
        /// <remarks>Use dot notation to include multiple levels, ex: "OrderLines.Product"</remarks></param>
        /// <returns>Query result mapped to an IEnumerable of type T</returns>
        public static IEnumerable<T> Query<T>(this IDb db, Expression<Func<T, bool>> filter = null, params string[] include) where T : class
        {
            return db.Query(null, null, filter, include);
        }

        /// <summary>
        /// Query database using a mapped POCO class to set the context. Sql statement and mapping will be fully handled by CoPilot
        /// </summary>
        /// <typeparam name="T">Mapped POCO class to set the context. <remarks>POCO class must be mapped using the DbMapper class</remarks></typeparam>
        /// <param name="db"></param>
        /// <param name="predicates">Add predicates, such as DESTINCT, TOP, SKIP and TAKE, by crteating an instance of the Predicates-class</param>
        /// <param name="filter">Filterexpression that will be translated into a WHERE clause (can be null).
        /// <remarks>Basic support for method calls on properties mapping to columns. <see cref="ExpressionDecoderConfig.MemberMethodCallConverter"/> and <see cref="ExpressionDecoderConfig"/></remarks></param>
        /// <param name="include">Array of named navigation properties that reference other mapped POCO classes. This will include data from related entities based on the configured relationships. 
        /// <remarks>Use dot notation to include multiple levels, ex: "OrderLines.Product"</remarks></param>
        /// <returns>Query result mapped to an IEnumerable of type T</returns>
        public static IEnumerable<T> Query<T>(this IDb db, Predicates predicates, Expression<Func<T, bool>> filter = null, params string[] include) where T : class
        {
            return db.Query(null, predicates, filter, include);
        }

        /// <summary>
        /// Query database using a mapped POCO class to set the context. Sql statement and mapping will be fully handled by CoPilot
        /// </summary>
        /// <typeparam name="T">Mapped POCO class to set the context. <remarks>POCO class must be mapped using the DbMapper class</remarks></typeparam>
        /// <param name="db"></param>
        /// <param name="orderByClause">Create an order by clause for the query by using the OrderByClause-class</param>
        /// <param name="filter">Filterexpression that will be translated into a WHERE clause (can be null).
        /// <remarks>Basic support for method calls on properties mapping to columns. <see cref="ExpressionDecoderConfig.MemberMethodCallConverter"/> and <see cref="ExpressionDecoderConfig"/></remarks></param>
        /// <param name="include">Array of named navigation properties that reference other mapped POCO classes. This will include data from related entities based on the configured relationships. 
        /// <remarks>Use dot notation to include multiple levels, ex: "OrderLines.Product"</remarks></param>
        /// <returns>Query result mapped to an IEnumerable of type T</returns>
        public static IEnumerable<T> Query<T>(this IDb db, OrderByClause<T> orderByClause, Expression<Func<T, bool>> filter = null, params string[] include) where T : class
        {
            return db.Query(orderByClause, null, filter, include);
        }

        /// <summary>
        /// Like Query, but without the filter option.
        /// </summary>
        /// <typeparam name="T">Mapped POCO class to set the context. <remarks>POCO class must be mapped using the DbMapper class</remarks></typeparam>
        /// <param name="db"></param>
        /// <param name="include">Array of named navigation properties that reference other mapped POCO classes. This will include data from related entities based on the configured relationships. 
        /// <remarks>Use dot notation to include multiple levels, ex: "OrderLines.Product"</remarks></param>
        /// <returns>Query result mapped to type of T</returns>
        public static IEnumerable<T> All<T>(this IDb db, params string[] include) where T : class
        {
            return db.Query<T>(null, include);
        }

        /// <summary>
        /// Like Query, but without the filter option.
        /// </summary>
        /// <typeparam name="T">Mapped POCO class to set the context. <remarks>POCO class must be mapped using the DbMapper class</remarks></typeparam>
        /// <param name="db"></param>
        /// <param name="predicates">Add predicates, such as DESTINCT, TOP, SKIP and TAKE, by crteating an instance of the Predicates-class</param>
        /// <param name="include">Array of named navigation properties that reference other mapped POCO classes. This will include data from related entities based on the configured relationships. 
        /// <remarks>Use dot notation to include multiple levels, ex: "OrderLines.Product"</remarks></param>
        /// <returns>Query result mapped to type of T</returns>
        public static IEnumerable<T> All<T>(this IDb db, Predicates predicates, params string[] include) where T : class
        {
            return db.Query<T>(predicates, null, include);
        }

        /// <summary>
        /// Like Query, but without the filter option.
        /// </summary>
        /// <typeparam name="T">Mapped POCO class to set the context. <remarks>POCO class must be mapped using the DbMapper class</remarks></typeparam>
        /// <param name="db"></param>
        /// <param name="orderByClause">Create an order by clause for the query by using the OrderByClause-class</param>
        /// <param name="include">Array of named navigation properties that reference other mapped POCO classes. This will include data from related entities based on the configured relationships. 
        /// <remarks>Use dot notation to include multiple levels, ex: "OrderLines.Product"</remarks></param>
        /// <returns>Query result mapped to type of T</returns>
        public static IEnumerable<T> All<T>(this IDb db, OrderByClause<T> orderByClause, params string[] include) where T : class
        {
            return db.Query(orderByClause, null, include);
        }

        /// <summary>
        /// Like Query, but without the filter option.
        /// </summary>
        /// <typeparam name="T">Mapped POCO class to set the context. <remarks>POCO class must be mapped using the DbMapper class</remarks></typeparam>
        /// <param name="db"></param>
        /// <param name="orderByClause">Create an order by clause for the query by using the OrderByClause-class</param>
        /// <param name="predicates">Add predicates, such as DESTINCT, TOP, SKIP and TAKE, by crteating an instance of the Predicates-class</param>
        /// <param name="include">Array of named navigation properties that reference other mapped POCO classes. This will include data from related entities based on the configured relationships. 
        /// <remarks>Use dot notation to include multiple levels, ex: "OrderLines.Product"</remarks></param>
        /// <returns>Query result mapped to type of T</returns>
        public static IEnumerable<T> All<T>(this IDb db, OrderByClause<T> orderByClause, Predicates predicates, params string[] include) where T : class
        {
            return db.Query(orderByClause, predicates, null, include);
        }

        /// <summary>
        /// Like Query, besides it will attempt to return single or default.
        /// </summary>
        /// <typeparam name="T">Mapped POCO class to set the context. <remarks>POCO class must be mapped using the DbMapper class</remarks></typeparam>
        /// <param name="db"></param>
        /// <param name="filter">Filterexpression that will be translated into a WHERE clause (can be null).
        /// <remarks>Basic support for method calls on properties mapping to columns. <see cref="ExpressionDecoderConfig.MemberMethodCallConverter"/> and <see cref="ExpressionDecoderConfig"/></remarks></param>
        /// <param name="include">Array of named navigation properties that reference other mapped POCO classes. This will include data from related entities based on the configured relationships. 
        /// <remarks>Use dot notation to include multiple levels, ex: "OrderLines.Product"</remarks></param>
        /// <returns>Query result mapped to type of T</returns>
        public static T Single<T>(this IDb db, Expression<Func<T, bool>> filter, params string[] include) where T : class
        {
            return db.Query(filter, include).SingleOrDefault();
        }

        /// <summary>
        /// Query database using a mapped POCO class to set the context. Sql statement and mapping will be fully handled by CoPilot 
        /// <remarks>The selector can be used to select a subset of columns from the context entity or its related entities (many-to-one relationships).</remarks>
        /// </summary>
        /// <typeparam name="TEntity">Mapped POCO class to set the context. <remarks>POCO class must be mapped using the DbMapper class</remarks></typeparam>
        /// <param name="db"></param>
        /// <param name="selector">Expression to select a single property to be returned or a new anonymous object containing the columns wanted. You can also select a related mapped POCO class.</param>
        /// <param name="filter">Filterexpression that will be translated into a WHERE clause (can be null).
        /// <remarks>Basic support for method calls on properties mapping to columns. <see cref="ExpressionDecoderConfig.MemberMethodCallConverter"/> and <see cref="ExpressionDecoderConfig"/></remarks></param>
        /// <returns>Query result mapped to a dynamic type</returns>
        public static IEnumerable<dynamic> Query<TEntity>(this IDb db, Expression<Func<TEntity, object>> selector, Expression<Func<TEntity, bool>> filter = null) where TEntity : class
        {
            return db.Query(selector, null, null, filter);
        }

        /// <summary>
        /// Query database using a mapped POCO class to set the context. Sql statement and mapping will be fully handled by CoPilot 
        /// <remarks>The selector can be used to select a subset of columns from the context entity or its related entities (many-to-one relationships).</remarks>
        /// </summary>
        /// <typeparam name="TEntity">Mapped POCO class to set the context. <remarks>POCO class must be mapped using the DbMapper class</remarks></typeparam>
        /// <param name="db"></param>
        /// <param name="selector">Expression to select a single property to be returned or a new anonymous object containing the columns wanted. You can also select a related mapped POCO class.</param>
        /// <param name="orderByClause">Create an order by clause for the query by using the OrderByClause-class</param>
        /// <param name="filter">Filterexpression that will be translated into a WHERE clause (can be null).
        /// <remarks>Basic support for method calls on properties mapping to columns. <see cref="ExpressionDecoderConfig.MemberMethodCallConverter"/> and <see cref="ExpressionDecoderConfig"/></remarks></param>
        /// <returns>Query result mapped to a dynamic type</returns>
        public static IEnumerable<dynamic> Query<TEntity>(this IDb db, Expression<Func<TEntity, object>> selector, OrderByClause<TEntity> orderByClause, Expression<Func<TEntity, bool>> filter = null) where TEntity : class
        {
            return db.Query(selector, orderByClause, null, filter);
        }

        /// <summary>
        /// Query database using a mapped POCO class to set the context. Sql statement and mapping will be fully handled by CoPilot 
        /// <remarks>The selector can be used to select a subset of columns from the context entity or its related entities (many-to-one relationships).</remarks>
        /// </summary>
        /// <typeparam name="TEntity">Mapped POCO class to set the context. <remarks>POCO class must be mapped using the DbMapper class</remarks></typeparam>
        /// <param name="db"></param>
        /// <param name="selector">Expression to select a single property to be returned or a new anonymous object containing the columns wanted. You can also select a related mapped POCO class.</param>
        /// <param name="predicates">Add predicates, such as DESTINCT, TOP, SKIP and TAKE, by crteating an instance of the Predicates-class</param>
        /// <param name="filter">Filterexpression that will be translated into a WHERE clause (can be null).
        /// <remarks>Basic support for method calls on properties mapping to columns. <see cref="ExpressionDecoderConfig.MemberMethodCallConverter"/> and <see cref="ExpressionDecoderConfig"/></remarks></param>
        /// <returns>Query result mapped to a dynamic type</returns>
        public static IEnumerable<dynamic> Query<TEntity>(this IDb db, Expression<Func<TEntity, object>> selector, Predicates predicates, Expression<Func<TEntity, bool>> filter = null) where TEntity : class
        {
            return db.Query(selector, null, predicates, filter);
        }

        /// <summary>
        /// Query database using a mapped POCO class to set the context. Sql statement and mapping will be fully handled by CoPilot 
        /// <remarks>The selector can be used to select a subset of columns from the context entity or its related entities (many-to-one relationships).</remarks>
        /// </summary>
        /// <typeparam name="TEntity">Mapped POCO class to set the context. <remarks>POCO class must be mapped using the DbMapper class</remarks></typeparam>
        /// <param name="db"></param>
        /// <param name="selector">Expression to select a single property to be returned or a new anonymous object containing the columns wanted. You can also select a related mapped POCO class.</param>
        /// <param name="orderByClause">Create an order by clause for the query by using the OrderByClause-class</param>
        /// <param name="predicates">Add predicates, such as DESTINCT, TOP, SKIP and TAKE, by crteating an instance of the Predicates-class</param>
        /// <param name="filter">Filterexpression that will be translated into a WHERE clause (can be null).
        /// <remarks>Basic support for method calls on properties mapping to columns. <see cref="ExpressionDecoderConfig.MemberMethodCallConverter"/> and <see cref="ExpressionDecoderConfig"/></remarks></param>
        /// <returns>Query result mapped to a dynamic type</returns>
        public static IEnumerable<dynamic> Query<TEntity>(this IDb db, Expression<Func<TEntity, object>> selector, OrderByClause<TEntity> orderByClause, Predicates predicates,
            Expression<Func<TEntity, bool>> filter = null) where TEntity : class
        {
            return db.Query<TEntity, object>(selector, orderByClause, predicates, filter);
        }

        /// <summary>
        /// Query database using a mapped POCO class to set the context. Sql statement and mapping will be fully handled by CoPilot 
        /// <remarks>The selector can be used to select a subset of columns from the context entity or its related entities (many-to-one relationships).</remarks>
        /// </summary>
        /// <typeparam name="TEntity">Mapped POCO class to set the context. <remarks>POCO class must be mapped using the DbMapper class</remarks></typeparam>
        /// <typeparam name="TDto">POCO class to map the query result to based on the selector</typeparam>
        /// <param name="db"></param>
        /// <param name="selector">Expression to select a single property to be returned or a new anonymous object containing the columns wanted. You can also select a related mapped POCO class.</param>
        /// <param name="orderByClause">Create an order by clause for the query by using the OrderByClause-class</param>
        /// <param name="filter">Filterexpression that will be translated into a WHERE clause (can be null).
        /// <remarks>Basic support for method calls on properties mapping to columns. <see cref="ExpressionDecoderConfig.MemberMethodCallConverter"/> and <see cref="ExpressionDecoderConfig"/></remarks></param>
        /// <returns>Query result mapped to type of TDto</returns>
        public static IEnumerable<TDto> Query<TEntity, TDto>(this IDb db, Expression<Func<TEntity, object>> selector, OrderByClause<TEntity> orderByClause, Expression<Func<TEntity, bool>> filter = null) where TEntity : class
        {
            return db.Query<TEntity, TDto>(selector, orderByClause, null, filter);
        }

        /// <summary>
        /// Query database using a mapped POCO class to set the context. Sql statement and mapping will be fully handled by CoPilot 
        /// <remarks>The selector can be used to select a subset of columns from the context entity or its related entities (many-to-one relationships).</remarks>
        /// </summary>
        /// <typeparam name="TEntity">Mapped POCO class to set the context. <remarks>POCO class must be mapped using the DbMapper class</remarks></typeparam>
        /// <typeparam name="TDto">POCO class to map the query result to based on the selector</typeparam>
        /// <param name="db"></param>
        /// <param name="selector">Expression to select a single property to be returned or a new anonymous object containing the columns wanted. You can also select a related mapped POCO class.</param>
        /// <param name="predicates">Add predicates, such as DESTINCT, TOP, SKIP and TAKE, by crteating an instance of the Predicates-class</param>
        /// <param name="filter">Filterexpression that will be translated into a WHERE clause (can be null).
        /// <remarks>Basic support for method calls on properties mapping to columns. <see cref="ExpressionDecoderConfig.MemberMethodCallConverter"/> and <see cref="ExpressionDecoderConfig"/></remarks></param>
        /// <returns>Query result mapped to type of TDto</returns>
        public static IEnumerable<TDto> Query<TEntity, TDto>(this IDb db, Expression<Func<TEntity, object>> selector, Predicates predicates, Expression<Func<TEntity, bool>> filter = null) where TEntity : class
        {
            return db.Query<TEntity, TDto>(selector, null, predicates, filter);
        }

        /// <summary>
        /// Query database using a mapped POCO class to set the context. Sql statement and mapping will be fully handled by CoPilot 
        /// <remarks>The selector can be used to select a subset of columns from the context entity or its related entities (many-to-one relationships).</remarks>
        /// </summary>
        /// <typeparam name="TEntity">Mapped POCO class to set the context. <remarks>POCO class must be mapped using the DbMapper class</remarks></typeparam>
        /// <typeparam name="TDto">POCO class to map the query result to based on the selector</typeparam>
        /// <param name="db"></param>
        /// <param name="selector">Expression to select a single property to be returned or a new anonymous object containing the columns wanted. You can also select a related mapped POCO class.</param>
        /// <param name="filter">Filterexpression that will be translated into a WHERE clause (can be null).
        /// <remarks>Basic support for method calls on properties mapping to columns. <see cref="ExpressionDecoderConfig.MemberMethodCallConverter"/> and <see cref="ExpressionDecoderConfig"/></remarks></param>
        /// <returns>Query result mapped to type of TDto</returns>
        public static IEnumerable<TDto> Query<TEntity, TDto>(this IDb db, Expression<Func<TEntity,object>> selector, Expression<Func<TEntity, bool>> filter = null) where TEntity : class
        {
            return db.Query<TEntity, TDto>(selector, null, null, filter);
        }

        /// <summary>
        /// Like Query, besides it will attempt to return single or default.
        /// <remarks>The selector can be used to select a subset of columns from the context entity or its related entities (many-to-one relationships).</remarks>
        /// </summary>
        /// <typeparam name="TEntity">Mapped POCO class to set the context. <remarks>POCO class must be mapped using the DbMapper class</remarks></typeparam>
        /// <typeparam name="TDto">POCO class to map the query result to based on the selector</typeparam>
        /// <param name="db"></param>
        /// <param name="selector">Expression to select a single property to be returned or a new anonymous object containing the columns wanted. You can also select a related mapped POCO class.</param>
        /// <param name="filter">Filterexpression that will be translated into a WHERE clause (can be null).
        /// <remarks>Basic support for method calls on properties mapping to columns. <see cref="ExpressionDecoderConfig.MemberMethodCallConverter"/> and <see cref="ExpressionDecoderConfig"/></remarks></param>
        /// <returns>Query result mapped to type of TDto</returns>
        public static TDto Single<TEntity, TDto>(this IDb db, Expression<Func<TEntity, object>> selector, Expression<Func<TEntity, bool>> filter) where TEntity : class
        {
            return db.Query<TEntity, TDto>(selector, filter).SingleOrDefault();
        }

        /// <summary>
        /// Issues a scalar command/query to the database by writing a parameterized statement or name of stored procedure
        /// </summary>
        /// <param name="db"></param>
        /// <param name="commandText">Scalar statement or stored procedure name.<remarks>Name paramters with @-sign followed by name, ex: @id or @firstName</remarks></param>
        /// <param name="args">Anonymous object containing values for parameters in the commandText or the parameters defined in the stored procedure. 
        /// <remarks>The property names of the anonymous object must match the named paramters excluding the @-sign</remarks>
        /// </param>
        /// <returns>Single value returned from statement converted to type of T</returns>
        public static T Scalar<T>(this IDb db, string commandText, object args = null)
        {
            object convertedValue;
            ReflectionHelper.ConvertValueToType(typeof(T), db.Scalar(commandText, args), out convertedValue);

            return (T)convertedValue;
        }

        /// <summary>
        /// Will issue an insert or an update statement depending on if the primary key has a default value or not. 
        /// <remarks>If the entity does not have a singular primary key you need to use the explicit Insert and Update methods in the DbWriter class <see cref="DbWriter"/></remarks>
        /// </summary>
        /// <typeparam name="T">Mapped POCO class to set the context. <remarks>POCO class must be mapped using the DbMapper class</remarks></typeparam>
        /// <param name="db"></param>
        /// <param name="entity">The object instance of the entitity to save</param>
        /// <param name="include">Array of named navigation properties that reference other mapped POCO classes. Included entities will also be inserted/updated. 
        /// <remarks>child records that exist in the db, but not in the instance being updated, will be deleted for one-to many relationships. Foreign key relationships will be attempted to be set to NULL if the entity being updated has a NULL value for a referenced entity in the include list</remarks></param>
        public static void Save<T>(this IDb db, T entity, params string[] include) where T : class
        {
            db.Save(entity, (OperationType.Insert | OperationType.Update | OperationType.Delete), include);
        }

        /// <summary>
        /// Batch version of Save for single entity 
        /// <remarks>If the entity does not have a singular primary key you need to use the explicit Insert and Update methods in the DbWriter class <see cref="DbWriter"/></remarks>
        /// </summary>
        /// <typeparam name="T">Mapped POCO class to set the context. <remarks>POCO class must be mapped using the DbMapper class</remarks></typeparam>
        /// <param name="db"></param>
        /// <param name="entities">The collection of object instances to save</param>
        /// <param name="include">Array of named navigation properties that reference other mapped POCO classes. Included entities will also be inserted/updated. 
        /// <remarks>child records that exist in the db, but not in the instance being updated, will be deleted for one-to many relationships. Foreign key relationships will be attempted to be set to NULL if the entity being updated has a NULL value for a referenced entity in the include list</remarks></param>
        public static void Save<T>(this IDb db, IEnumerable<T> entities, params string[] include) where T : class
        {
            db.Save(entities, (OperationType.Insert | OperationType.Update | OperationType.Delete), include);
        }
    }
}
