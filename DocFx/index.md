# Gmod MongoDB Wrapper

GmodMongoDb is a Garry's Mod module that provides a wrapper around the [MongoDB C# Driver](https://mongodb.github.io/mongo-csharp-driver/2.19).

## Usage

### Connecting
First load this module using the Gmod&period;NET function `dotnet.load`:
```lua
dotnet.load("GmodMongoDb")
```

Instantiate a client using a valid MongoDB connection string:
```lua
client = MongoDB.Driver.MongoClient("mongodb://myusername:superdupersecretpassword@127.0.0.1:27017/myappname?retryWrites=true&w=majority")
```
*You should only have a single client. You can re-use that for multiple different databases.*

### Fetching data
Get a database object, then get a collection object from it:
```lua
local database = client:GetDatabase("myappname")
local collection = database:GetCollection(
  GenericType(MongoDB.Bson.BsonDocument), 
  "players"
)
```
Since we are binding to C# methods and classes, some of those may be [generic](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/generics). For that reason you may need to supply a generic type using the `GenericType` function. Any class that has a constructor can be used as a generic type.

You can filter using a BSON Document or function. 

#### Using a Lua table to filter:
```lua
local filterDocument = MongoDB.Bson.BsonDocument.Parse(
  util.TableToJSON({
    _id = "STEAM_0:1:123456"
  })
)
local filter = MongoDB.Driver.BsonDocumentFilterDefinition(filterDocument)
local amount = collection:CountDocuments(filter)
print(amount)
```

#### Using a JSON string to filter:
```lua
local filterDocument = MongoDB.Bson.BsonDocument.Parse("{_id: 'STEAM_0:1:123456'}")
local filter = MongoDB.Driver.BsonDocumentFilterDefinition(filterDocument)
local amount = collection:CountDocuments(filter)
print(amount)
```

**Using a function to filter is not supported.**

### Inserting data

#### Using a JSON string to insert data:
```lua
local newDocument = MongoDB.Bson.BsonDocument.Parse(
  util.TableToJSON({
    name = "Jane Doe",
    age = 28,
    alive = true,
  })
)
collection:InsertOne(newDocument)
```

## Differences with the MongoDB .NET Driver

This module automatically binds to version `2.19` of the MongoDB .NET Driver. You can find [it's API documentation here](
https://mongodb.github.io/mongo-csharp-driver/2.19/apidocs/).

Most of the functionality and usage in Lua should be identical to it's C# variant. The only differences are caused by Lua missing some features that C# has (like generics, classes and constructors).

This means that besides our documentation you may find [the .NET MongoDB Driver API Reference](https://mongodb.github.io/mongo-csharp-driver/2.19/apidocs/) helpful.

> ### Some examples of differences
> 
> #### Constructors
> *In C#: `var client = new MongoClient(connectionString)`*
> 
> In GmodMongoDb Lua objects are instantiated by calling the type as a function:
> ```lua
> local client = MongoDB.Driver.MongoClient(connectionString)
> ```
>
> #### Getting a database from a client
> *In C#: `var database = client.GetDatabase("dbName")`*
>
> In GmodMongoDb Lua:
> ```lua
> local database = client:GetDatabase("dbName")
> ```
> 
> #### Generics
> *In C#: `var collection = client.GetCollection<MongoDB.Bson.BsonDocument>("collectionName")`*
>
> In GmodMongoDb Lua generics are passed as the first arguments to a function, using the `GenericType` helper function:
> ```lua
> local collection = client:GetCollection(GenericType(MongoDB.Bson.BsonDocument), "collectionName")
> ```
>
> When a generic type argument can be inferred from the type of a parameter, you can omit the `GenericType` call: 
> ```lua
> local filterDocument = MongoDB.Bson.BsonDocument.Parse("{_id: 'STEAM_0:1:123456'}")
> -- These are both valid notations:
> local a = MongoDB.Driver.BsonDocumentFilterDefinition(GenericType(MongoDB.Bson.BsonDocument), filterDocument)
> local b = MongoDB.Driver.BsonDocumentFilterDefinition(filterDocument) -- can be inferred because filterDocument is of the type MongoDB.Bson.BsonDocument
> ```
