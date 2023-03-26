TEST.client = MongoDB.Driver.MongoClient("mongodb://localhost:27017/repo_test?retryWrites=true&w=majority")

assert(TEST.client ~= nil, "MongoDB.Driver.MongoClient is nil")


-- End of tests
return true