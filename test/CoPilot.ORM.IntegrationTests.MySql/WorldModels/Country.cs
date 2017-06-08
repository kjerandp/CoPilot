using System.Collections.Generic;

namespace CoPilot.ORM.IntegrationTests.MySql.WorldModels
{
    public class Country
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Continent { get; set; }
        public string Region { get; set; }
        public double SurfaceArea { get; set; }
        public List<CountryLanguage> Languages { get; set; }
        public List<City> Cities { get; set; }
    }
}