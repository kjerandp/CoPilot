using System;

namespace CoPilot.ORM.IntegrationTests.Models.BandSample
{
    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime DateOfBirth { get; set; }
        public City City { get; set; }
    }
}
