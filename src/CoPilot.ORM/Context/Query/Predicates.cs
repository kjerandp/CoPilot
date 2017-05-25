namespace CoPilot.ORM.Context.Query
{
    /// <summary>
    /// Set predicates to use with query (DISTINCT, TOP, SKIP and TAKE)
    /// </summary>
    public class Predicates
    {
        public bool Distinct { get; set; }
        public int? Top { get; set; }
        /// <summary>
        /// Requires an order by clause
        /// </summary>
        public int? Skip { get; set; }
        /// <summary>
        /// Requires an order by clause
        /// </summary>
        public int? Take { get; set; }
    }


}
