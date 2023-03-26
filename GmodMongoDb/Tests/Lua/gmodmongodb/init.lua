TEST = {}

local TEST_LOG_RESULT_PATH = "gmod_mongo_db_test_result.txt"

file.Write(TEST_LOG_RESULT_PATH, "0")

local successfullyFinishTest = function()
	file.Write(TEST_LOG_RESULT_PATH, "1")

	MsgC(Color(0, 255, 0), "Successfully finished test")
end

MsgC(Color(255, 255, 0), "Starting test...")

--[[
	Test start
]]--

require("dotnet")

dotnet.load("GmodMongoDb")

assert(include("tests/base.lua"), "Failed tests in tests/base.lua")
assert(include("tests/client.lua"), "Failed tests in tests/client.lua")
assert(include("tests/database.lua"), "Failed tests in tests/database.lua")
assert(include("tests/collection.lua"), "Failed tests in tests/collection.lua")

-- If we encounter no errors during the test, we will write our success result to a file
successfullyFinishTest()