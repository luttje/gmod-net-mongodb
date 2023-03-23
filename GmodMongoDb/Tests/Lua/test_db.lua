-- lua_openscript test_db.lua

print("GmodMongoDb", "load()")
require("dotnet")
dotnet.load("GmodMongoDb")

test_client = MongoClient("mongodb://localhost:27017/repo_test?retryWrites=true&w=majority")
local db = test_client:GetDatabase("repo_test")
local collection = db:GetCollection("collection_test")

print(db) -- Note how the userdata changes when we ask the collection for it's database below

local filterTable = { _id = "singleplayer" }
--local filterJson = util.TableToJSON(filterTable)
local filter = MongoBsonDocument(filterTable)
print(filter, type(filter))
local results = collection:Find(filter)
PrintTable(results)

print("client", db.Client, db.Client == test_client)
print("db", collection.Database, collection.Database == db)

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