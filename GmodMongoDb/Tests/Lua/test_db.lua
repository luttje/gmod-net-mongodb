-- lua_openscript test_db.lua
-- lua_run dotnet.load("GmodMongoDb")
-- lua_run dotnet.unload("GmodMongoDb")

print("GmodMongoDb", "load()")

dotnet.load("GmodMongoDb")

-- print(MongoClient)
-- mdbClient = MongoClient("mongodb://bootlegger:395kjkh20jhq5wH65qwa5AST@127.0.0.1:27017/revolt?retryWrites=true&w=majority")

-- print(mdbClient)
-- print(mdbClient.Cluster)
-- PrintTable(mdbClient)

print(MongoClient, type(MongoClient), BsonDocument, type(BsonDocument))

local client = MongoClient("mongodb://bootlegger:395kjkh20jhq5wH65qwa5AST@127.0.0.1:27017/revolt?retryWrites=true&w=majority")
local db = client:GetDatabase("revolt")
local collection = db:GetCollection{BsonDocument}("bootlegrp_players")

print(db)

local filterTable = { _id = "singleplayer" }
--local filterJson = util.TableToJSON(filterTable)
local filter = BsonDocument(util.TableToJSON(filterTable))
--print(filter, type(filter))
--local results = collection:Find(filter)
--PrintTable(results)

-- print("client", db.Client, db.Client == client)
-- print("db", collection.Database, collection.Database == db)

-- print(results[1], type(results[1]))
-- local refindResults = collection:Find(results[1])
-- PrintTable(refindResults)

-- print(results[1] == refindResults[1])

-- local findWithBsonDocument = collection:Find(BsonDocument({money = 500}))
-- PrintTable(findWithBsonDocument)

-- local document = BsonDocument(util.TableToJSON(filter))
-- local findWithJsonBsonDocument = collection:Find(document)
-- PrintTable(findWithJsonBsonDocument)

--print(tostring(results[1]).."\n\n")
-- print(results[1]._id)

-- for key, value in results[1]:Pairs() do
--   print(key, value)
-- end

-- PrintTable(client:ListDatabaseNames())

-- print(CurTime())
-- client:ListDatabaseNamesAsync(function(databases)
--   print(CurTime())
--   PrintTable(databases)
-- end)

-- client:DropDatabaseAsync("remove_me", function()
--   print("done")
-- end)

-- local databases = client:ListDatabases()
-- print(databases, table.Count(databases))


-- for i, database in pairs(databases) do
--   print(i, tostring(database), print(getmetatable(database)))

--   for key, value in database:Pairs() do
--     print(key, value)
--   end
-- end