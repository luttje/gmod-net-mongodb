TEST = {}

local TEST_LOG_RESULT_PATH = "gmod_mongo_db_test_success.txt"

file.Delete(TEST_LOG_RESULT_PATH)

local successfullyFinishTest = function()
	file.Write(TEST_LOG_RESULT_PATH, "1")

	MsgC(Color(0, 255, 0), "Successfully finished test\n")
end

MsgC(Color(255, 255, 0), "Starting test...\n")

--[[
	Test start
]]--

require("dotnet")

if(GMOD_MONGODB_DEV_PATH ~= nil) then
	dotnet.load(GMOD_MONGODB_DEV_PATH)
else
	dotnet.load("GmodMongoDb")
end

assert(include("tests/base.lua"), "Failed tests in tests/base.lua")
assert(include("tests/client.lua"), "Failed tests in tests/client.lua")
assert(include("tests/database.lua"), "Failed tests in tests/database.lua")
assert(include("tests/collection.lua"), "Failed tests in tests/collection.lua")

-- If we encounter no errors during the test, we will write our success result to a file
successfullyFinishTest()