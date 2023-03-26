TEST.collection = TEST.database:GetCollection(GenericType(MongoDB.Bson.BsonDocument), "collection_test")

assert(TEST.collection ~= nil, "MongoDB.Driver.MongoCollection is nil")

local filterMatchAll = MongoDB.Driver["ExpressionFilterDefinition`1"](GenericType(MongoDB.Bson.BsonDocument), function(document)
  return true 
end)

assert(filterMatchAll ~= nil, "MongoDB.Driver.ExpressionFilterDefinition is nil")

--[[
  Deletes
]]--

local deleteResult = TEST.collection:DeleteMany(filterMatchAll)

assert(deleteResult ~= nil, "MongoDB.Driver.DeleteResult is nil")
assert(type(deleteResult.DeletedCount) == "number", "MongoDB.Driver.DeleteResult.DeletedCount is not a number")
assert(type(deleteResult.IsAcknowledged) == "boolean", "MongoDB.Driver.DeleteResult.IsAcknowledged is not a boolean")

--[[
  Scalar operations
]]--

local count = TEST.collection:Count(filterMatchAll)

assert(type(count) == "number", "MongoDB.Driver.MongoCollection:Count() did not return a number")
assert(count == 0, "MongoDB.Driver.MongoCollection:Count() did not return 0")

local estimatedDocumentCount = TEST.collection:EstimatedDocumentCount()

assert(type(estimatedDocumentCount) == "number", "MongoDB.Driver.MongoCollection:EstimatedDocumentCount() did not return a number")
assert(estimatedDocumentCount == 0, "MongoDB.Driver.MongoCollection:EstimatedDocumentCount() did not return 0")

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

assert(insertResult ~= nil, "MongoDB.Driver.InsertOneResult is nil")
assert(type(insertResult.IsAcknowledged) == "boolean", "MongoDB.Driver.InsertOneResult.IsAcknowledged is not a boolean")
assert(type(insertResult.InsertedId) == "userdata", "MongoDB.Driver.InsertOneResult.InsertedId is not a userdata")

local newDocuments = MongoDB.Bson.BsonDocument.Parse(util.TableToJSON({
  {
    name = "Jane Doe",
    age = 28,
    alive = true,
  },
  {
    name = "John Doe",
    age = 29,
    alive = true,
  },
}))

local insertManyResult = TEST.collection:InsertMany(newDocuments)

assert(insertManyResult ~= nil, "MongoDB.Driver.InsertManyResult is nil")
assert(type(insertManyResult.IsAcknowledged) == "boolean", "MongoDB.Driver.InsertManyResult.IsAcknowledged is not a boolean")
assert(type(insertManyResult.InsertedIds) == "table", "MongoDB.Driver.InsertManyResult.InsertedIds is not a table")

--[[
  Filters
]]--

local filterMatchVip = MongoDB.Driver["ExpressionFilterDefinition`1"](GenericType(MongoDB.Bson.BsonDocument), function(document)
  return document.name == vip.name
end)

assert(filterMatchVip ~= nil, "MongoDB.Driver.ExpressionFilterDefinition is nil")

assert(TEST.collection:Count(filterMatchVip) == 1, "MongoDB.Driver.MongoCollection:Count() did not return 1")

--[[
  Updates
]]--

local update = MongoDB.Driver["UpdateDefinition`1"](GenericType(MongoDB.Bson.BsonDocument), function(document)
  document.age = 34
  return document
end)

assert(update ~= nil, "MongoDB.Driver.UpdateDefinition is nil")

local updateResult = TEST.collection:UpdateOne(filterMatchVip, update)

assert(updateResult ~= nil, "MongoDB.Driver.UpdateResult is nil")
assert(type(updateResult.IsAcknowledged) == "boolean", "MongoDB.Driver.UpdateResult.IsAcknowledged is not a boolean")
assert(type(updateResult.MatchedCount) == "number", "MongoDB.Driver.UpdateResult.MatchedCount is not a number")
assert(type(updateResult.ModifiedCount) == "number", "MongoDB.Driver.UpdateResult.ModifiedCount is not a number")

--[[
  Finds
]]--

local findSyncResult = TEST.collection:FindSync(GenericType(MongoDB.Bson.BsonDocument), filterMatchVip)

assert(findSyncResult ~= nil, "MongoDB.Driver.IAsyncCursor is nil")

local findResult = findSyncResult:ToList()

assert(findResult ~= nil, "MongoDB.Driver.List is nil")
assert(type(findResult.Count) == "number", "MongoDB.Driver.List.Count is not a number")
assert(findResult.Count == 1, "MongoDB.Driver.List.Count is not 1")
