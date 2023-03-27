TEST = {}

local TEST_LOG_RESULT_PATH = "gmod_mongo_db_test_success.txt"

file.Delete(TEST_LOG_RESULT_PATH)

local closeServer = function()
	if(game.SinglePlayer()) then
		return
	end

	-- If our tests finish too early, the engine.CloseServer() function will not work. Therefore we will spam it until it works
	hook.Add("Think", "GmodMongoDb.FinishAfterTests", function()
		engine.CloseServer()
	end)
end

TEST.assert = function(expression, errorMessage, ...)
	local args = {...}
	local result, err = pcall(function()
		assert(expression, errorMessage, unpack(args))
	end)

	if(not result) then
		ErrorNoHalt(err .. "\n")
		
		closeServer()
	end
end

local successfullyFinishTest = function()
	file.Write(TEST_LOG_RESULT_PATH, "1")

	MsgC(Color(0, 255, 0), "[GmodMongoDb] Successfully finished test\n")

	closeServer()
end

MsgC(Color(255, 255, 0), "[GmodMongoDb] Starting tests...\n")

--[[
	Test start
]]--

require("dotnet")

if(GMOD_MONGODB_DEV_PATH ~= nil) then
	dotnet.load(GMOD_MONGODB_DEV_PATH)
else
	dotnet.load("GmodMongoDb")
end

TEST.assert(include("tests/base.lua"), "Failed tests in tests/base.lua")
TEST.assert(include("tests/client.lua"), "Failed tests in tests/client.lua")
TEST.assert(include("tests/database.lua"), "Failed tests in tests/database.lua")
TEST.assert(include("tests/collection.lua"), "Failed tests in tests/collection.lua")

-- If we encounter no errors during the test, we will write our success result to a file
successfullyFinishTest()