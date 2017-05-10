using System.Collections.Generic;
using System.Linq;

namespace CoPilot.ORM.IntegrationTests.Models.BandSample
{
    public class Country
    {
        public Country() { }

        public Country(string name, params City[] cities)
        {
            Name = name;
            if (cities != null)
            {
                Cities = cities.ToList();
            }
        }
        public int Id { get; set; }
        public string Name { get; set; }
        public List<City> Cities { get; set; }
    }
}