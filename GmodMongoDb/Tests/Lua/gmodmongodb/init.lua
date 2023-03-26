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

include("tests/base.lua")
include("tests/client.lua")
include("tests/database.lua")
include("tests/collection.lua")

-- If we encounter no errors during the test, we will write our success result to a file
successfullyFinishTest()