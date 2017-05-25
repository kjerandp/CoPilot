using System.Collections.Generic;

namespace CoPilot.ORM.Tests.Models
{
    public class City
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string CityCode { get; set; }
        public List<Organization> Organizations { get; set; }
    }
}
