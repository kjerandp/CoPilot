# CoPilot

CoPilot is an object relational mapper (ORM). In its core it works like a micro ORM, but it has some neat and powerful features added on top of it, which makes it more of a "middle ground" ORM that sits between most micro ORM and fully featured, enterprise level ORMs. CoPilot is built with performance and flexibillity in mind, and was specifically designed to avoid any leakage into other (non-data access) layers. 

**Key features:**  
* Map POCO models to tables including relationships (one-to-many and many-to-one relationships). 
* Author and execute SQL statements for CRUD operations. Related entities can be included for all operations, as long as they have a singular key defined. 
* Mapping of data from queries or stored procedures to dynamic objects or POCO classes. 
* Natural, function based, querying syntax when working with mapped entities.
* Projecting of data to anonymous or defined classes (only referenced columns will be included in the queries)
* Solves the "1+N"-problem by fetching child records in a single query and then merging the data with the parent records when mapped
* Perform mulitple write operations, including bulk commands, as a unit-of-work (database transaction)
* Transform values from and to the database by associating a `ValueAdapter` to relevant POCO properties. Examples of use cases is to serialize/deserialize to and from json, joining/splitting collections of primitive values, converting from/to enums etc.
* Use lookup tables - meaning that the value of a property can be used to lookup a key value in another table, and then pass that value to the mapped table and then do the same in reverse. Can be handy if you for instance want to use enums in your POCOs, but you want to enforce a foreign key constraint to another table for the mapped column.
* Generate scripts to build database from model configurations
* Support for multiple Ado.Net providers (currently implementations for Ms Sql Server and MySql exists)
* Validate configuration against database schema

## Install
Install the database provider you want to use:

```
Install-Package CoPilot.ORM.SqlServer -Pre
```
or

```
Install-Package CoPilot.ORM.MySql -Pre
```
## How to use
CoPilot aims to be as simple and intuitive as possible to use and most features are available from a single interface called `IDb`. Please look for examples in the test projects, especially the BandSampleTests.cs

### Quick examples

Showing most of the functions available when writing context queries with the new syntax introduced in v2:
```
var bands = _db.From<Band>()
    .Where(r => !r.Name.StartsWith("B") && !r.Name.StartsWith("L")) 
    .Include("BandMembers")
    .OrderBy(r => r.Name)
    .ThenBy(r => r.Formed, Ordering.Descending)
    .ThenBy(r => r.Id)
    .Skip(1)
    .Take(20)
    .Distinct()
    .AsEnumerable();
```
Mapping result to anonymous type (projection):
```
var bands = _db.From<Band>()
    .Select(r => new { 
		BandId = r.Id, 
		BandName = r.Name, 
		Nationality = r.Based.Country.Name 
	})
    .OrderBy(r => r.Nationality)
	.Take(50)
    .ToArray();
```
More complex projection:
```
var band = _db.From<Band>().Where(r => r.Id == 5).Select(r => 
    new {
        BandName = r.Name,
        Discography = r.Recordings.SelectMany(b => b.AlbumTracks.Select(t => 
        new {
            Album = t.Album.Title,
            t.Album.Released,
            Song = b.SongTitle,
            b.Recorded,
            t.TrackNumber,
            Duration = $"{b.Duration.Minutes}:{b.Duration.Seconds}"   
        })).OrderBy(x => x.Album).ThenBy(x => x.TrackNumber)
    }
).Single();
```

Executing and mapping stored procedure:
```
var recordings = _db.Query<Recording>(
    "Get_Recordings_CTE",							//stored procedure name
    new { recorded = new DateTime(2017, 5, 1) },	//arguments (for parameter @recorded)
    "Base", "Base.AlbumTracks"						//naming of record sets
);
```
Inserting new record with transaction support:
```
using (var writer = new DbWriter(_db))
{
	try {
		var testBand = new Band
		{
			Formed = DateTime.Today,
			Name = "Test Band",
			Based = writer.GetReader().FindByKey<City>(1)
		};

		writer.Save(testBand);
		writer.Commit();
	} catch (Exception ex){
		writer.Rollback();
		//error handling
	}
}
```
Bulk inserting example:
```
const int insertCount = 10000;

using (var writer = new DbWriter(_db))
{
    var dt = new DateTime(1980,1,1);
    writer.PrepareCommand("insert into BAND (city_id,band_name,band_formed) values (@cityId, @bandName, @formed)", new {cityId=0, bandName=string.Empty, formed=dt});

    for (var i = 0; i < insertCount; i++)
    {
        writer.Command(new {cityId = 1, bandName = "Lazy Bulk Band " + i, formed = dt.AddDays(i)});
    }

    writer.Commit();
}
```