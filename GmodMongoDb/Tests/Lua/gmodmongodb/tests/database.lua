TEST.database = TEST.client:GetDatabase("test_repo")

assert(TEST.database ~= nil, "MongoDB.Driver.MongoDatabase is nil")


-- End of tests
return true