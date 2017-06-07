using System.Collections.Generic;

namespace CoPilot.ORM.IntegrationTests.MySql.WorldModels
{
    public class City
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string CountryCode { get; set; }
        public string District { get; set; }
        public int Population { get; set; }
    }

    public class Country
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Continent { get; set; }
        public string Region { get; set; }
        public float SurfaceArea { get; set; }
        public List<CountryLanguage> Languages { get; set; }
        public List<City> Cities { get; set; }
    }

    public class CountryLanguage
    {
        public string CountryCode { get; set; }
        public string Language { get; set; }
        public bool IsOfficial { get; set; }
        public float Percentage { get; set; }
        public Country Country { get; set; }
    }
}
