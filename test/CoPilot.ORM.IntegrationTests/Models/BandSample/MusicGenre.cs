using System.Collections.Generic;

namespace CoPilot.ORM.IntegrationTests.Models.BandSample
{
    public class MusicGenre
    {
        public MusicGenre() { }

        public MusicGenre(string name)
        {
            Name = name;
        }
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Recording> Recordings { get; set; }
    }
}