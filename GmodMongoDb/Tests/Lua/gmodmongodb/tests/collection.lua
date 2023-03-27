TEST.collection = TEST.database:GetCollection(GenericType(MongoDB.Bson.BsonDocument), "collection_test")

TEST.assert(TEST.collection ~= nil, "MongoDB.Driver.MongoCollection is nil")

local filterMatchAll = MongoDB.Driver.EmptyFilterDefinition(GenericType(MongoDB.Bson.BsonDocument))

TEST.assert(filterMatchAll ~= nil, "MongoDB.Driver.EmptyFilterDefinition is nil")

--[[
  Deletes
]]--

local deleteResult = TEST.collection:DeleteMany(filterMatchAll)

TEST.assert(deleteResult ~= nil, "MongoDB.Driver.DeleteResult is nil")
TEST.assert(type(deleteResult.DeletedCount) == "number", "MongoDB.Driver.DeleteResult.DeletedCount is not a number")
TEST.assert(type(deleteResult.IsAcknowledged) == "boolean", "MongoDB.Driver.DeleteResult.IsAcknowledged is not a boolean")

local deleteResult = TEST.collection:DeleteMany(filterMatchAll)

TEST.assert(deleteResult ~= nil, "MongoDB.Driver.DeleteResult is nil")
TEST.assert(type(deleteResult.DeletedCount) == "number", "MongoDB.Driver.DeleteResult.DeletedCount is not a number")
TEST.assert(type(deleteResult.IsAcknowledged) == "boolean", "MongoDB.Driver.DeleteResult.IsAcknowledged is not a boolean")

--[[
  Scalar operations
]]--

local count = TEST.collection:CountDocuments(filterMatchAll)

TEST.assert(type(count) == "number", "MongoDB.Driver.MongoCollection:CountDocuments() did not return a number")
TEST.assert(count == 0, "MongoDB.Driver.MongoCollection:CountDocuments() did not return 0, but " .. count)

local estimatedDocumentCount = TEST.collection:EstimatedDocumentCount()

TEST.assert(type(estimatedDocumentCount) == "number", "MongoDB.Driver.MongoCollection:EstimatedDocumentCount() did not return a number")
TEST.assert(estimatedDocumentCount == 0, "MongoDB.Driver.MongoCollection:EstimatedDocumentCount() did not return 0, but " .. estimatedDocumentCount)

--[[
  Inserts
]]--

local vip = {
  name = "A.V. Uniquename",
  age = 33,
  alive = true,
}
local newDocument = MongoDB.Bson.BsonDocument.Parse(util.TableToJSON(vip))

local insertResult = TEST.collection:InsertOne(newDocument)

TEST.assert(insertResult == nil, "MongoDB.Driver.InsertOneResult is not nil")

-- Fails because we have not yet implemented handling table to List conversion
-- local newDocuments = {
--   MongoDB.Bson.BsonDocument.Parse(util.TableToJSON({
--     name = "Jane Doe",
--     age = 28,
--     alive = true,
--   })),
--   MongoDB.Bson.BsonDocument.Parse(util.TableToJSON({
--     name = "John Doe",
--     age = 29,
--     alive = true,
--   })),
-- }

-- local insertManyResult = TEST.collection:InsertMany(newDocuments)

-- TEST.assert(insertManyResult == nil, "MongoDB.Driver.InsertManyResult is not nil")

--[[
  Filters
]]--

local filterMatchVipDocument = MongoDB.Bson.BsonDocument.Parse(
  util.TableToJSON({
    name = vip.name
  })
)
local filterMatchVip = MongoDB.Driver.BsonDocumentFilterDefinition(filterMatchVipDocument)

TEST.assert(filterMatchVip ~= nil, "MongoDB.Driver.BsonDocumentFilterDefinition is nil")

local count = TEST.collection:CountDocuments(filterMatchVip)
TEST.assert(count == 1, "MongoDB.Driver.MongoCollection:CountDocuments() did not return 1, but " .. count)

-- Providing a function to a filter (or in any place an Expression is expected) is not yet supported. This is because MongoDB will attempt
-- to convert the function to an expression tree. 
-- local filterMatchAll = MongoDB.Driver.ExpressionFilterDefinition(GenericType(MongoDB.Bson.BsonDocument), function(document)
--   return true 
-- end)

-- TEST.assert(filterMatchAll ~= nil, "MongoDB.Driver.ExpressionFilterDefinition is nil")

--[[
  Updates
]]--

local updateDocument = MongoDB.Bson.BsonDocument.Parse(
  util.TableToJSON({
    ["$set"] = {
      age = 34
    }
  })
)
local update = MongoDB.Driver.BsonDocumentUpdateDefinition(updateDocument)

TEST.assert(update ~= nil, "MongoDB.Driver.BsonDocumentUpdateDefinition is nil")

local updateResult = TEST.collection:UpdateOne(filterMatchVip, update)

TEST.assert(updateResult ~= nil, "MongoDB.Driver.UpdateResult is nil")
TEST.assert(type(updateResult.IsAcknowledged) == "boolean", "MongoDB.Driver.UpdateResult.IsAcknowledged is not a boolean")
TEST.assert(type(updateResult.MatchedCount) == "number", "MongoDB.Driver.UpdateResult.MatchedCount is not a number")
TEST.assert(type(updateResult.ModifiedCount) == "number", "MongoDB.Driver.UpdateResult.ModifiedCount is not a number")

local update = MongoDB.Driver.JsonUpdateDefinition(
  GenericType(MongoDB.Bson.BsonDocument),
  util.TableToJSON({
    ["$set"] = {
      age = 35
    }
  })
)

TEST.assert(update ~= nil, "MongoDB.Driver.JsonUpdateDefinition is nil")

local updateResult = TEST.collection:UpdateOne(filterMatchVip, update)

TEST.assert(updateResult ~= nil, "MongoDB.Driver.UpdateResult is nil")
TEST.assert(type(updateResult.IsAcknowledged) == "boolean", "MongoDB.Driver.UpdateResult.IsAcknowledged is not a boolean")
TEST.assert(type(updateResult.MatchedCount) == "number", "MongoDB.Driver.UpdateResult.MatchedCount is not a number")
TEST.assert(type(updateResult.ModifiedCount) == "number", "MongoDB.Driver.UpdateResult.ModifiedCount is not a number")

--[[
  Finds
]]--

local findSyncResult = TEST.collection:FindSync(GenericType(MongoDB.Bson.BsonDocument), filterMatchVip)

TEST.assert(findSyncResult ~= nil, "MongoDB.Driver.IAsyncCursor is nil")

while findSyncResult.Current ~= nil do
  local currentDocument = findSyncResult.Current:ToBsonDocument()

  TEST.assert(currentDocument ~= nil, "MongoDB.Driver.IAsyncCursor.Current.ToBsonDocument is nil")
  TEST.assert(currentDocument["name"]:AsString() == vip.name, "MongoDB.Driver.IAsyncCursor.Current.ToBsonDocument.name is not " .. vip.name .. ", but " .. currentDocument["name"]:AsString())
  TEST.assert(currentDocument["age"]:AsInt32() == vip.age, "MongoDB.Driver.IAsyncCursor.Current.ToBsonDocument.age is not " .. vip.age .. ", but " .. currentDocument["age"]:AsInt32())
  TEST.assert(currentDocument["alive"]:AsBoolean() == vip.alive, "MongoDB.Driver.IAsyncCursor.Current.ToBsonDocument.alive is not " .. tostring(vip.alive) .. ", but " .. tostring(currentDocument["alive"]:AsBoolean()))

  findSyncResult:MoveNext()
end

-- End of tests
return true