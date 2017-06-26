using CoPilot.ORM.Model;

namespace CoPilot.ORM.PostgreSql
{
    public static class Extensions
    {
        public static IDb CreateDb(this DbModel model, string connectionString)
        {
            return model.CreateDb(connectionString, new PostgreSqlProvider());
        }

        public static string QuoteIfNeeded(this string text)
        {
            if (text == null) return null;
            return text.Contains(" ") ? "\"" + text + "\"" : text;
        }

        public static string GetAsString(this DbTable table)
        {
            if (string.IsNullOrEmpty(table.Schema))
            {
                return table.TableName.QuoteIfNeeded();
            }
            return $"{table.Schema.QuoteIfNeeded()}.{table.TableName.QuoteIfNeeded()}";
        }

    }
}
