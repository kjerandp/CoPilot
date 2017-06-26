using CoPilot.ORM.Model;

namespace CoPilot.ORM.MySql
{
    public static class Extensions
    {
        public static IDb CreateDb(this DbModel model, string connectionString)
        {
            return model.CreateDb(connectionString, new MySqlProvider());
        }
        public static string QuoteIfNeeded(this string text)
        {
            if (text == null) return null;
            return text.Contains(" ") ? "`" + text + "`" : text;
        }

        public static string GetAsString(this DbTable table)
        {
            return table.TableName.QuoteIfNeeded();
        }
    }
}
