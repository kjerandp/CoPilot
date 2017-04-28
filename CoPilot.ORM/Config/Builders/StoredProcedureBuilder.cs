using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Config.Builders
{
    public class StoredProcedureBuilder : BaseBuilder
    {
        private readonly DbStoredProcedure _proc;

        public StoredProcedureBuilder(DbModel model, DbStoredProcedure proc): base(model)
        {
            _proc = proc;
        }

        public void Parameters(params DbParameter[] parameters)
        {
            _proc.Parameters.AddRange(parameters);
        }
    }
}