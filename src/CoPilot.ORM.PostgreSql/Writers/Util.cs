namespace CoPilot.ORM.PostgreSql.Writers
{
    public static class Util
    {
        public static string SanitizeName(string name)
        {
            return name.Contains(" ") ? "\"" + name + "\"" : name;
        }
    }
}
