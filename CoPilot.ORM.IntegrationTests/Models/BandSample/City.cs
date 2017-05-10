namespace CoPilot.ORM.IntegrationTests.Models.BandSample
{
    public class City
    {
        public City() { }

        public City(string name)
        {
            Name = name;
        }
        public int Id { get; set; }
        public string Name { get; set; }
        public Country Country { get; set; }
    }
}