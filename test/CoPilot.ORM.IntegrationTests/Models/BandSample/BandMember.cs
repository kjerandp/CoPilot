using System;

namespace CoPilot.ORM.IntegrationTests.Models.BandSample
{
    public class BandMember
    {
        public int Id { get; set; }
        public Person Person { get; set; }
        public Band Band { get; set; }
        public string Instrument { get; set; }
        public string ArtistName { get; set; }
        public DateTime Joined { get; set; }
        public DateTime? Left { get; set; }

    }
}