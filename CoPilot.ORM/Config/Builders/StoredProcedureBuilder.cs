using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Config.Builders
{
    /// <summary>
    /// Builder class for configuring stored procedures
    /// </summary>
    public class StoredProcedureBuilder : BaseBuilder
    {
        private readonly DbStoredProcedure _proc;

        public StoredProcedureBuilder(DbModel model, DbStoredProcedure proc): base(model)
        {
            _proc = proc;
        }

        /// <summary>
        /// Add parameters to stored procedure configuration
        /// </summary>
        /// <param name="parameters">Parameter definition <see cref="DbParameter"/></param>
        public void Parameters(params DbParameter[] parameters)
        {
            _proc.Parameters.AddRange(parameters);
        }
    }
}