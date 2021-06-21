-- lua_openscript test_db.lua

function load()
  print("GmodMongoDb", "load()")

  dotnet.load("GmodMongoDb")

  test_client = mongo:NewClient("mongodb://bootlegger:395kjkh20jhq5wH65qwa5AST@127.0.0.1:27017/revolt?retryWrites=true&w=majority")
  local db = test_client:GetDatabase("revolt")
  local collection = db:GetCollection("bootlegrp_players")

  local filter = { _id = "singleplayer" }
  local results = collection:Find(util.TableToJSON(filter))
  PrintTable(results)

  print(results[1], type(results[1]))
  local refindResults = collection:Find(results[1])
  PrintTable(refindResults)

  print(results[1] == refindResults[1])
  
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
end

load()