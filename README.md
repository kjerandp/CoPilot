# CoPilot

CoPilot is an object relational mapper (ORM). In its core it works like a micro ORM, but it has some neat and powerful features added on top of it. 

Note: This is work in progress to enable support for different ado.net database providers. Currently Sql Server and MySql providers are available as seperate nuget packages.

**Key features:**  
* Map POCO models to tables including relationships (one-to-many and many-to-one relationships). 
* Author and execute SQL statements for CRUD operations. Related entities can be included for all operations, as long as they have a singular key defined. 
* Mapping of data from queries or stored procedures to dynamic objects or POCO classes. 
* Mapped POCO models allow for a natural, function based, querying syntax and also mapping a subset of columns to anonymous types.
* Solves the "1+N"-problem by fetching child records in a single query and then merging the data with the parent records when mapped
* Perform mulitple write operations, including bulk commands, as a unit-of-work (database transaction)
* Transform values from and to the database by associating a `ValueAdapter` to relevant POCO properties. Examples of use cases is to serialize/deserialize to and from json, joining/splitting collections of primitive values, converting from/to enums etc.
* Use lookup tables - meaning that the value of a property can be used to lookup a key value in another table, and then pass that value to the mapped table and then do the same in reverse. Can be handy if you for instance want to use enums in your POCOs, but you want to enforce a foreign key constraint to another table for the mapped column.
* Generate scripts to build database from model configurations
* Validate configuration against database schema

## Install
Install the database provider you want to use:

```
Install-Package CoPilot.ORM.Providers.SqlServer -Pre
```
or

```
Install-Package CoPilot.ORM.Providers.MySql -Pre
```
## How to use
CoPilot aims to be as simple and intuitive as possible to use and most features are available from a single interface called `IDb`. 
