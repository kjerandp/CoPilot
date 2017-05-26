using CoPilot.ORM.Config.DataTypes;
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

        public StoredProcedureBuilder AddParameter(string name, DbDataType dataType, object defaultValue = null, bool canBeNull = true, bool isOutput = false)
        {
            _proc.Parameters.Add(new DbParameter(name, dataType, defaultValue, canBeNull, isOutput));
            return this;
        }

        public StoredProcedureBuilder AddParameter(string name, DbDataType dataType, int maxSize, object defaultValue = null, bool canBeNull = true, bool isOutput = false)
        {
            _proc.Parameters.Add(new DbParameter(name, dataType, defaultValue, canBeNull, isOutput) {Size = maxSize});
            return this;
        }
        public StoredProcedureBuilder AddParameter(string name, DbDataType dataType, NumberPrecision numberPrecision, object defaultValue = null, bool canBeNull = true, bool isOutput = false)
        {
            _proc.Parameters.Add(new DbParameter(name, dataType, defaultValue, canBeNull, isOutput) { NumberPrecision = numberPrecision });
            return this;
        }
    }
}