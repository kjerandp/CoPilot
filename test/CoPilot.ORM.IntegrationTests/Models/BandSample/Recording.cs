using System;
using System.Collections.Generic;

namespace CoPilot.ORM.IntegrationTests.Models.BandSample
{
    public class Recording
    {
        public int Id { get; set; }
        public string SongTitle { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime Recorded { get; set; }
        public MusicGenre Genre { get; set; }
        public List<AlbumTrack> AlbumTracks { get; set; }
        public Band Band { get; set; }
    }
}