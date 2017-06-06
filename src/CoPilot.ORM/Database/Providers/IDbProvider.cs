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
        ISelectStatementWriter SelectStatementWriter { get; }
        IInsertStatementWriter InsertStatementWriter { get; }
        IUpdateStatementWriter UpdateStatementWriter { get; }
        IDeleteStatementWriter DeleteStatementWriter { get; }
        ICommonScriptingTasks CommonScriptingTasks { get; }
        QueryStrategySelector QueryStrategySelector { get; }

        DbResponse ExecuteQuery(DbRequest cmd, params string[] names);
        int ExecuteNonQuery(DbRequest cmd);
        void PrepareNonQuery(DbRequest cmd);
        int ReRunCommand(IDbCommand command, object args);
        object ExecuteScalar(DbRequest cmd);

        string GetDataTypeAsString(DbDataType dataType, int size = 0);
        
        IDbConnection CreateConnection(string connectionString);
        IDbCommand CreateCommand(IDbConnection connection = null, int timeout=0);
        
        bool ValidateModel(IDb db);
    }
}
