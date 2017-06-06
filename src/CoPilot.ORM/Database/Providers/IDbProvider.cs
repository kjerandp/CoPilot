using System.Data;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Database.Commands.Query.Strategies;
using CoPilot.ORM.Database.Commands.SqlWriters;

namespace CoPilot.ORM.Database.Providers
{
    public interface IDbProvider
    {
        ICreateStatementWriter CreateStatementWriter { get; }
        IQueryBuilder QueryBuilder { get; }
        QueryStrategySelector QueryStrategySelector { get; }
        ISelectStatementWriter SelectStatementWriter { get; }
        IInsertStatementWriter InsertStatementWriter { get; }
        IUpdateStatementWriter UpdateStatementWriter { get; }
        IDeleteStatementWriter DeleteStatementWriter { get; }
        ICommonScriptingTasks CommonScriptingTasks { get; }


        DbResponse ExecuteQuery(IDbConnection connection, DbRequest cmd, params string[] names);
        DbResponse ExecuteQuery(DbRequest cmd, params string[] names);
        int ExecuteNonQuery(IDbConnection connection, DbRequest cmd);
        int ExecuteNonQuery(DbRequest cmd);
        void PrepareNonQuery(DbRequest cmd);
        int ReRunCommand(IDbCommand command, object args);
        object ExecuteScalar(IDbConnection connection, DbRequest cmd);
        object ExecuteScalar(DbRequest cmd);

        string GetDataTypeAsString(DbDataType dataType, int size = 0);
        bool DataTypeHasSize(DbDataType dataType);
        
        IDbConnection CreateConnection(string connectionString);
        IDbCommand CreateCommand(string connectionString, int timeout=0);
        IDbCommand CreateCommand(IDbConnection connection = null, int timeout=0);


    }
}
