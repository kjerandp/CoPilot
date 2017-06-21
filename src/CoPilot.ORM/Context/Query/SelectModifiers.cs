namespace CoPilot.ORM.Context.Query
{
    /// <summary>
    /// Set SelectModifiers to use with query (DISTINCT, SKIP and TAKE)
    /// </summary>
    public class SelectModifiers
    {
        public bool Distinct { get; set; }

        public int? Skip { get; set; }

        public int? Take { get; set; }
    }

  
}
