using CoPilot.ORM.Config;
using CoPilot.ORM.IntegrationTests.Models.BandSample;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.IntegrationTests.Config
{
    public class BandSampleConfig
    {
        public const string DbName = "BandsSample";

        public static DbModel CreateModel()
        {
            var mapper = new DbMapper();

            mapper.Map<Country>("COUNTRY");
            mapper.Map<MusicGenre>("MUSIC_GENRE");
            mapper.Map<Album>("ALBUM");

            var cityMap = mapper.Map<City>("CITY");
            var personMap = mapper.Map<Person>("PERSON");
            var bandMap = mapper.Map<Band>("BAND");
            var bandMemberMap = mapper.Map<BandMember>("BAND_MEMBER");
            var recordingMap = mapper.Map<Recording>("RECORDING");
            var albumTrackMap = mapper.Map<AlbumTrack>("ALBUM_TRACK");

            cityMap.HasOne(r => r.Country, "~COUNTRY_ID").InverseKeyMember(r => r.Cities);

            personMap.HasOne(r => r.City, "~CITY_ID");

            bandMap.HasOne(r => r.Based, "CITY_ID");

            bandMemberMap.HasOne(r => r.Person, "~PERSON_ID");
            bandMemberMap.HasOne(r => r.Band, "~BAND_ID").InverseKeyMember(r => r.BandMembers);

            recordingMap.HasOne(r => r.Genre, "~GENRE_ID").InverseKeyMember(r => r.Recordings);
            recordingMap.HasOne(r => r.Band, "~BAND_ID").InverseKeyMember(r => r.Recordings);

            albumTrackMap.HasOne(r => r.Recording, "~RECORDING_ID").InverseKeyMember(r => r.AlbumTracks);
            albumTrackMap.HasOne(r => r.Album, "~ALBUM_ID").InverseKeyMember(r => r.Tracks);

            return mapper.CreateModel();
        }
    }
}
