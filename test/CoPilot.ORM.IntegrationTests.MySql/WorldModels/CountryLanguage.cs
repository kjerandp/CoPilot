namespace CoPilot.ORM.IntegrationTests.MySql.WorldModels
{
    public class CountryLanguage
    {
        public string CountryCode { get; set; }
        public string Language { get; set; }
        public bool IsOfficial { get; set; }
        public float Percentage { get; set; }
        public Country Country { get; set; }
    }
}