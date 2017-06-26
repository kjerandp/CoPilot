using CoPilot.ORM.Model;

namespace CoPilot.ORM.PostgreSql
{
    public static class Extensions
    {
        public static IDb CreateDb(this DbModel model, string connectionString)
        {
            return model.CreateDb(connectionString, new PostgreSqlProvider());
        }

    }
}
