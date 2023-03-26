-- lua_openscript db_test.lua

print("GmodMongoDb", "load()")
require("dotnet")
dotnet.unload("GmodMongoDb")
dotnet.load("GmodMongoDb")

dotnet.unload("D:/Projects/_Personal/Games/Gmod/gmod-net-mongodb/GmodMongoDb/bin/Debug/net7.0/GmodMongoDb.dll")
dotnet.load("D:/Projects/_Personal/Games/Gmod/gmod-net-mongodb/GmodMongoDb/bin/Debug/net7.0/GmodMongoDb.dll")

print(MongoDB)
print(MongoDB.Shared.HexUtils.ParseInt32("ffff"))

local testClient = MongoDB.Driver.MongoClient("mongodb://localhost:27017/repo_test?retryWrites=true&w=majority")
local database = testClient:GetDatabase("repo_test")
collection = database:GetCollection(GenericType(MongoDB.Bson.BsonDocument), "collection_test")

print(collection)

filter = MongoDB.Driver["ExpressionFilterDefinition`1"](GenericType(MongoDB.Bson.BsonDocument), function(document)
  return true 
end)

print(collection:Count(filter))
-- lua_run PrintTable(collection:Find(filter)) -- TODO: Linq extension methods

local newDocument = MongoDB.Bson.BsonDocument.Parse(util.TableToJSON({
  name = "Jane Doe",
  age = 28,
  alive = true,
}))
collection:InsertOne(newDocument)

-- print(db) -- Note how the userdata changes when we ask the collection for it's database below

-- local filterTable = { _id = "singleplayer" }
-- --local filterJson = util.TableToJSON(filterTable)
-- local filter = MongoBsonDocument(filterTable)
-- print(filter, type(filter))
-- local results = collection:Find(filter)
-- PrintTable(results)

-- print("client", db.Client, db.Client == test_client)
-- print("db", collection.Database, collection.Database == db)

-- print(results[1], type(results[1]))
-- local refindResults = collection:Find(results[1])
-- PrintTable(refindResults)

-- print(results[1] == refindResults[1])

-- local findWithBsonDocument = collection:Find(MongoBsonDocument({money = 500}))
-- PrintTable(findWithBsonDocument)

-- local document = MongoBsonDocument(util.TableToJSON(filter))
-- local findWithJsonBsonDocument = collection:Find(document)
-- PrintTable(findWithJsonBsonDocument)

--print(tostring(results[1]).."\n\n")
-- print(results[1]._id)

-- for key, value in results[1]:Pairs() do
--   print(key, value)
-- end

-- PrintTable(test_client:ListDatabaseNames())

-- print(CurTime())
-- test_client:ListDatabaseNamesAsync(function(databases)
--   print(CurTime())
--   PrintTable(databases)
-- end)

-- test_client:DropDatabaseAsync("remove_me", function()
--   print("done")
-- end)

-- local databases = test_client:ListDatabases()
-- print(databases, table.Count(databases))


-- for i, database in pairs(databases) do
--   print(i, tostring(database), print(getmetatable(database)))

--   for key, value in database:Pairs() do
--     print(key, value)
--   end
-- end