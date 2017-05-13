using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq.Expressions;
using CoPilot.ORM.Common;
using CoPilot.ORM.Context.Query;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Filtering;
using CoPilot.ORM.Mapping;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Database
{
    /// <summary>
    /// Interface for interacting with CoPilot
    /// </summary>
    public interface IDb
    {
        /// <summary>
        /// CoPilot's internal description of the database and its entities
        /// </summary>
        DbModel Model { get; }      
        /// <summary>
        /// Get a SqlConnection from the connection string provided to CoPilot
        /// </summary>
        SqlConnection Connection { get; }

        /// <summary>
        /// Query database writing a parameterized query statement or name of stored procedure
        /// </summary>
        /// <param name="commandText">Query statement or stored procedure name.<remarks>Name paramters with @-sign followed by name, ex: @id or @firstName</remarks></param>
        /// <param name="args">Anonymous object containing values for parameters in the commandText or the parameters defined in the stored procedure. 
        /// <remarks>The property names of the anonymous object must match the named paramters excluding the @-sign</remarks>
        /// </param>
        /// <param name="names">Use to name resultsets returned (optional) <remark>It is useful to name datasets when issuing multiple query statements</remark></param>
        /// <returns>General object that holds raw data returned from the query</returns>
        DbResponse Query(string commandText, object args, params string[] names);

        /// <summary>
        /// Query database writing a parameterized query statement or name of stored procedure
        /// </summary>
        /// <typeparam name="T">Type to map results to using default mapper. <remarks>Specify a POCO class for using basic mapper or 'dynamic' for dynamic mapper</remarks></typeparam>
        /// <param name="commandText">Query statement or stored procedure name.<remarks>Name paramters with @-sign followed by name, ex: @id or @firstName</remarks></param>
        /// <param name="args">Anonymous object containing values for parameters in the commandText or the parameters defined in the stored procedure. 
        /// <remarks>For stored procedures that have its parameters mapped, you can pass an object instance as long as it has matching properties for all required parameters</remarks></param>
        /// <param name="names">Use to name resultsets returned (optional)</param>
        /// <returns>Query result mapped to an IEnumerable of type T</returns>
        IEnumerable<T> Query<T>(string commandText, object args, params string[] names);

        /// <summary>
        /// Query database writing a parameterized query statement or name of stored procedure
        /// </summary>
        /// <typeparam name="T">Type to map results to using default mapper. <remarks>Specify a POCO class for using basic mapper or 'dynamic' for dynamic mapper</remarks></typeparam>
        /// <param name="commandText">Query statement or stored procedure name.<remarks>Name paramters with @-sign followed by name, ex: @id or @firstName</remarks></param>
        /// <param name="args">Anonymous object containing values for parameters in the commandText or the parameters defined in the stored procedure. 
        /// <remarks>For stored procedures that have its parameters mapped, you can pass an object instance as long as it has matching properties for all required parameters</remarks></param>
        /// <param name="mapper">Pass a mapping delegate to use. You can pass a custom mapper or use the buildt-in Basic-, Dyamic- or ContextMapper.</param>
        /// <param name="names">Use to name resultsets returned. This is required if using the context mapper. The names must then match the navigation property names that the resultset should be merged into</param>
        /// <returns>Query result mapped to an IEnumerable of type T</returns>
        IEnumerable<T> Query<T>(string commandText, object args, ObjectMapper mapper, params string[] names);

        /// <summary>
        /// Query database using a mapped POCO class to set the context. Sql statement and mapping will be fully handled by CoPilot
        /// </summary>
        /// <typeparam name="T">Mapped POCO class to set the context. <remarks>POCO class must be mapped using the DbMapper class</remarks></typeparam>
        /// <param name="filter">Filterexpression that will be translated into a WHERE clause (can be null).
        /// <remarks>Basic support for method calls on properties mapping to columns. <see cref="ExpressionDecoderConfig.MemberMethodCallConverter"/> and <see cref="ExpressionDecoderConfig"/></remarks></param>
        /// <param name="include">Array of named navigation properties that reference other mapped POCO classes. This will include data from related entities based on the configured relationships. 
        /// <remarks>Use dot notation to include multiple levels, ex: "OrderLines.Product"</remarks></param>
        /// <returns>Query result mapped to an IEnumerable of type T</returns>
        IEnumerable<T> Query<T>(Expression<Func<T, bool>> filter = null, params string[] include) where T : class;

        /// <summary>
        /// Query database using a mapped POCO class to set the context. Sql statement and mapping will be fully handled by CoPilot
        /// </summary>
        /// <typeparam name="T">Mapped POCO class to set the context. <remarks>POCO class must be mapped using the DbMapper class</remarks></typeparam>
        /// <param name="predicates">Add predicates, such as DESTINCT, TOP, SKIP and TAKE, by crteating an instance of the Predicates-class</param>
        /// <param name="filter">Filterexpression that will be translated into a WHERE clause (can be null).
        /// <remarks>Basic support for method calls on properties mapping to columns. <see cref="ExpressionDecoderConfig.MemberMethodCallConverter"/> and <see cref="ExpressionDecoderConfig"/></remarks></param>
        /// <param name="include">Array of named navigation properties that reference other mapped POCO classes. This will include data from related entities based on the configured relationships. 
        /// <remarks>Use dot notation to include multiple levels, ex: "OrderLines.Product"</remarks></param>
        /// <returns>Query result mapped to an IEnumerable of type T</returns>
        IEnumerable<T> Query<T>(Predicates predicates, Expression<Func<T, bool>> filter = null, params string[] include) where T : class;

        /// <summary>
        /// Query database using a mapped POCO class to set the context. Sql statement and mapping will be fully handled by CoPilot
        /// </summary>
        /// <typeparam name="T">Mapped POCO class to set the context. <remarks>POCO class must be mapped using the DbMapper class</remarks></typeparam>
        /// <param name="orderByClause">Create an order by clause for the query by using the OrderByClause-class</param>
        /// <param name="filter">Filterexpression that will be translated into a WHERE clause (can be null).
        /// <remarks>Basic support for method calls on properties mapping to columns. <see cref="ExpressionDecoderConfig.MemberMethodCallConverter"/> and <see cref="ExpressionDecoderConfig"/></remarks></param>
        /// <param name="include">Array of named navigation properties that reference other mapped POCO classes. This will include data from related entities based on the configured relationships. 
        /// <remarks>Use dot notation to include multiple levels, ex: "OrderLines.Product"</remarks></param>
        /// <returns>Query result mapped to an IEnumerable of type T</returns>
        IEnumerable<T> Query<T>(OrderByClause<T> orderByClause, Expression<Func<T, bool>> filter = null, params string[] include) where T : class;

        /// <summary>
        /// Query database using a mapped POCO class to set the context. Sql statement and mapping will be fully handled by CoPilot
        /// </summary>
        /// <typeparam name="T">Mapped POCO class to set the context. <remarks>POCO class must be mapped using the DbMapper class</remarks></typeparam>
        /// <param name="orderByClause">Create an order by clause for the query by using the OrderByClause-class</param>
        /// <param name="predicates">Add predicates, such as DESTINCT, TOP, SKIP and TAKE, by crteating an instance of the Predicates-class</param>
        /// <param name="filter">Filterexpression that will be translated into a WHERE clause (can be null).
        /// <remarks>Basic support for method calls on properties mapping to columns. <see cref="ExpressionDecoderConfig.MemberMethodCallConverter"/> and <see cref="ExpressionDecoderConfig"/></remarks></param>
        /// <param name="include">Array of named navigation properties that reference other mapped POCO classes. This will include data from related entities based on the configured relationships. 
        /// <remarks>Use dot notation to include multiple levels, ex: "OrderLines.Product"</remarks></param>
        /// <returns>Query result mapped to an IEnumerable of type T</returns>
        IEnumerable<T> Query<T>(OrderByClause<T> orderByClause, Predicates predicates, Expression<Func<T, bool>> filter = null, params string[] include) where T : class;

        /// <summary>
        /// Query database using a mapped POCO class to set the context. Sql statement and mapping will be fully handled by CoPilot 
        /// <remarks>The selector can be used to select a subset of columns from the context entity or its related entities (many-to-one relationships).</remarks>
        /// </summary>
        /// <typeparam name="TEntity">Mapped POCO class to set the context. <remarks>POCO class must be mapped using the DbMapper class</remarks></typeparam>
        /// <param name="selector">Expression to select a single property to be returned or a new anonymous object containing the columns wanted. You can also select a related mapped POCO class.</param>
        /// <param name="filter">Filterexpression that will be translated into a WHERE clause (can be null).
        /// <remarks>Basic support for method calls on properties mapping to columns. <see cref="ExpressionDecoderConfig.MemberMethodCallConverter"/> and <see cref="ExpressionDecoderConfig"/></remarks></param>
        /// <returns>Query result mapped to a dynamic type</returns>
        IEnumerable<dynamic> Query<TEntity>(Expression<Func<TEntity, object>> selector, Expression<Func<TEntity, bool>> filter = null) where TEntity : class;
        
        /// <summary>
        /// Query database using a mapped POCO class to set the context. Sql statement and mapping will be fully handled by CoPilot 
        /// <remarks>The selector can be used to select a subset of columns from the context entity or its related entities (many-to-one relationships).</remarks>
        /// </summary>
        /// <typeparam name="TEntity">Mapped POCO class to set the context. <remarks>POCO class must be mapped using the DbMapper class</remarks></typeparam>
        /// <param name="selector">Expression to select a single property to be returned or a new anonymous object containing the columns wanted. You can also select a related mapped POCO class.</param>
        /// <param name="orderByClause">Create an order by clause for the query by using the OrderByClause-class</param>
        /// <param name="filter">Filterexpression that will be translated into a WHERE clause (can be null).
        /// <remarks>Basic support for method calls on properties mapping to columns. <see cref="ExpressionDecoderConfig.MemberMethodCallConverter"/> and <see cref="ExpressionDecoderConfig"/></remarks></param>
        /// <returns>Query result mapped to a dynamic type</returns>
        IEnumerable<dynamic> Query<TEntity>(Expression<Func<TEntity, object>> selector, OrderByClause<TEntity> orderByClause, Expression<Func<TEntity, bool>> filter = null) where TEntity : class;
        
        /// <summary>
        /// Query database using a mapped POCO class to set the context. Sql statement and mapping will be fully handled by CoPilot 
        /// <remarks>The selector can be used to select a subset of columns from the context entity or its related entities (many-to-one relationships).</remarks>
        /// </summary>
        /// <typeparam name="TEntity">Mapped POCO class to set the context. <remarks>POCO class must be mapped using the DbMapper class</remarks></typeparam>
        /// <param name="selector">Expression to select a single property to be returned or a new anonymous object containing the columns wanted. You can also select a related mapped POCO class.</param>
        /// <param name="predicates">Add predicates, such as DESTINCT, TOP, SKIP and TAKE, by crteating an instance of the Predicates-class</param>
        /// <param name="filter">Filterexpression that will be translated into a WHERE clause (can be null).
        /// <remarks>Basic support for method calls on properties mapping to columns. <see cref="ExpressionDecoderConfig.MemberMethodCallConverter"/> and <see cref="ExpressionDecoderConfig"/></remarks></param>
        /// <returns>Query result mapped to a dynamic type</returns>
        IEnumerable<dynamic> Query<TEntity>(Expression<Func<TEntity, object>> selector, Predicates predicates, Expression<Func<TEntity, bool>> filter = null) where TEntity : class;

        /// <summary>
        /// Query database using a mapped POCO class to set the context. Sql statement and mapping will be fully handled by CoPilot 
        /// <remarks>The selector can be used to select a subset of columns from the context entity or its related entities (many-to-one relationships).</remarks>
        /// </summary>
        /// <typeparam name="TEntity">Mapped POCO class to set the context. <remarks>POCO class must be mapped using the DbMapper class</remarks></typeparam>
        /// <param name="selector">Expression to select a single property to be returned or a new anonymous object containing the columns wanted. You can also select a related mapped POCO class.</param>
        /// <param name="orderByClause">Create an order by clause for the query by using the OrderByClause-class</param>
        /// <param name="predicates">Add predicates, such as DESTINCT, TOP, SKIP and TAKE, by crteating an instance of the Predicates-class</param>
        /// <param name="filter">Filterexpression that will be translated into a WHERE clause (can be null).
        /// <remarks>Basic support for method calls on properties mapping to columns. <see cref="ExpressionDecoderConfig.MemberMethodCallConverter"/> and <see cref="ExpressionDecoderConfig"/></remarks></param>
        /// <returns>Query result mapped to a dynamic type</returns>
        IEnumerable<dynamic> Query<TEntity>(Expression<Func<TEntity, object>> selector, OrderByClause<TEntity> orderByClause, Predicates predicates, Expression<Func<TEntity, bool>> filter = null) where TEntity : class;

        /// <summary>
        /// Query database using a mapped POCO class to set the context. Sql statement and mapping will be fully handled by CoPilot 
        /// <remarks>The selector can be used to select a subset of columns from the context entity or its related entities (many-to-one relationships).</remarks>
        /// </summary>
        /// <typeparam name="TEntity">Mapped POCO class to set the context. <remarks>POCO class must be mapped using the DbMapper class</remarks></typeparam>
        /// <typeparam name="TDto">POCO class to map the query result to based on the selector</typeparam>
        /// <param name="selector">Expression to select a single property to be returned or a new anonymous object containing the columns wanted. You can also select a related mapped POCO class.</param>
        /// <param name="filter">Filterexpression that will be translated into a WHERE clause (can be null).
        /// <remarks>Basic support for method calls on properties mapping to columns. <see cref="ExpressionDecoderConfig.MemberMethodCallConverter"/> and <see cref="ExpressionDecoderConfig"/></remarks></param>
        /// <returns>Query result mapped to type of TDto</returns>
        IEnumerable<TDto> Query<TEntity, TDto>(Expression<Func<TEntity, object>> selector, Expression<Func<TEntity, bool>> filter = null) where TEntity : class;

        /// <summary>
        /// Query database using a mapped POCO class to set the context. Sql statement and mapping will be fully handled by CoPilot 
        /// <remarks>The selector can be used to select a subset of columns from the context entity or its related entities (many-to-one relationships).</remarks>
        /// </summary>
        /// <typeparam name="TEntity">Mapped POCO class to set the context. <remarks>POCO class must be mapped using the DbMapper class</remarks></typeparam>
        /// <typeparam name="TDto">POCO class to map the query result to based on the selector</typeparam>
        /// <param name="selector">Expression to select a single property to be returned or a new anonymous object containing the columns wanted. You can also select a related mapped POCO class.</param>
        /// <param name="orderByClause">Create an order by clause for the query by using the OrderByClause-class</param>
        /// <param name="filter">Filterexpression that will be translated into a WHERE clause (can be null).
        /// <remarks>Basic support for method calls on properties mapping to columns. <see cref="ExpressionDecoderConfig.MemberMethodCallConverter"/> and <see cref="ExpressionDecoderConfig"/></remarks></param>
        /// <returns>Query result mapped to type of TDto</returns>
        IEnumerable<TDto> Query<TEntity, TDto>(Expression<Func<TEntity, object>> selector, OrderByClause<TEntity> orderByClause, Expression<Func<TEntity, bool>> filter = null) where TEntity : class;

        /// <summary>
        /// Query database using a mapped POCO class to set the context. Sql statement and mapping will be fully handled by CoPilot 
        /// <remarks>The selector can be used to select a subset of columns from the context entity or its related entities (many-to-one relationships).</remarks>
        /// </summary>
        /// <typeparam name="TEntity">Mapped POCO class to set the context. <remarks>POCO class must be mapped using the DbMapper class</remarks></typeparam>
        /// <typeparam name="TDto">POCO class to map the query result to based on the selector</typeparam>
        /// <param name="selector">Expression to select a single property to be returned or a new anonymous object containing the columns wanted. You can also select a related mapped POCO class.</param>
        /// <param name="predicates">Add predicates, such as DESTINCT, TOP, SKIP and TAKE, by crteating an instance of the Predicates-class</param>
        /// <param name="filter">Filterexpression that will be translated into a WHERE clause (can be null).
        /// <remarks>Basic support for method calls on properties mapping to columns. <see cref="ExpressionDecoderConfig.MemberMethodCallConverter"/> and <see cref="ExpressionDecoderConfig"/></remarks></param>
        /// <returns>Query result mapped to type of TDto</returns>
        IEnumerable<TDto> Query<TEntity, TDto>(Expression<Func<TEntity, object>> selector, Predicates predicates, Expression<Func<TEntity, bool>> filter = null) where TEntity : class;

        /// <summary>
        /// Query database using a mapped POCO class to set the context. Sql statement and mapping will be fully handled by CoPilot 
        /// <remarks>The selector can be used to select a subset of columns from the context entity or its related entities (many-to-one relationships).</remarks>
        /// </summary>
        /// <typeparam name="TEntity">Mapped POCO class to set the context. <remarks>POCO class must be mapped using the DbMapper class</remarks></typeparam>
        /// <typeparam name="TDto">POCO class to map the query result to based on the selector</typeparam>
        /// <param name="selector">Expression to select a single property to be returned or a new anonymous object containing the columns wanted. You can also select a related mapped POCO class.</param>
        /// <param name="orderByClause">Create an order by clause for the query by using the OrderByClause-class</param>
        /// <param name="predicates">Add predicates, such as DESTINCT, TOP, SKIP and TAKE, by crteating an instance of the Predicates-class</param>
        /// <param name="filter">Filterexpression that will be translated into a WHERE clause (can be null).
        /// <remarks>Basic support for method calls on properties mapping to columns. <see cref="ExpressionDecoderConfig.MemberMethodCallConverter"/> and <see cref="ExpressionDecoderConfig"/></remarks></param>
        /// <returns>Query result mapped to type of TDto</returns>
        IEnumerable<TDto> Query<TEntity, TDto>(Expression<Func<TEntity, object>> selector, OrderByClause<TEntity> orderByClause, Predicates predicates, Expression<Func<TEntity, bool>> filter = null) where TEntity : class;

        IEnumerable<T> All<T>(params string[] include) where T : class;
        IEnumerable<T> All<T>(Predicates predicates, params string[] include) where T : class;
        IEnumerable<T> All<T>(OrderByClause<T> orderByClause, params string[] include) where T : class;
        IEnumerable<T> All<T>(OrderByClause<T> orderByClause, Predicates predicates, params string[] include) where T : class;


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