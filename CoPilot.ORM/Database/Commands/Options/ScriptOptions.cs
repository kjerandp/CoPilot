namespace CoPilot.ORM.Database.Commands.Options
{
    public class ScriptOptions
    {
        public bool Parameterize { get; set; }
        public bool EnableIdentityInsert { get; set; }
        public bool UseNvar { get; set; }
        public bool SelectScopeIdentity { get; set; }

        public static ScriptOptions Default()
        {
            return new ScriptOptions { Parameterize = true, UseNvar = true, SelectScopeIdentity = true};
        }
    }
}
