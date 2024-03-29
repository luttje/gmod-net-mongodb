--[[
  Common
]]--

TEST.assert(type(MongoDB) == "table", "MongoDB is not a table")

--[[
  Constants
]]--
TEST.assert(type(GMOD_MONGODB_KEY_CLASS_TYPE) == "string", "GMOD_MONGODB_KEY_CLASS_TYPE is not a string")
TEST.assert(type(GMOD_MONGODB_KEY_INSTANCE_ID) == "string", "GMOD_MONGODB_KEY_INSTANCE_ID is not a string")
TEST.assert(type(GMOD_MONGODB_KEY_INSTANCE_TYPE) == "string", "GMOD_MONGODB_KEY_INSTANCE_TYPE is not a string")
TEST.assert(type(GMOD_MONGODB_KEY_TYPE_META_TABLES) == "string", "GMOD_MONGODB_KEY_TYPE_META_TABLES is not a string")

--[[
  GenericType
]]--

TEST.assert(type(GenericType) == "function", "GenericType is not a function")

local genericType0 = GenericType(MongoDB.Bson.BsonDocument)

TEST.assert(type(genericType0[GMOD_MONGODB_KEY_INSTANCE_ID]) == "string", "GenericType does not have KEY_INSTANCE_ID")
TEST.assert(type(genericType0[GMOD_MONGODB_KEY_INSTANCE_TYPE]) == "string", "GenericType does not have KEY_INSTANCE_TYPE")

local genericType1 = GenericType(MongoDB.Bson.BsonDouble)

TEST.assert(genericType0[GMOD_MONGODB_KEY_INSTANCE_ID] ~= genericType1[GMOD_MONGODB_KEY_INSTANCE_ID], "GenericType KEY_INSTANCE_ID is not unique")

--[[
  MongoDB.Shared.HexUtils.ParseInt32
]]--
local hexTests = {
  -- Positive numbers:
  { "0", 0 }, { "1", 1 },
  { "00", 0 }, { "01", 1 },
  { "10", 16 }, { "FF", 255 },
  { "100", 256 }, { "FFF", 4095 },
  { "1000", 4096 }, { "ffff", 65535 },
  { "10000", 65536 }, { "FFFFF", 1048575 },
  { "100000", 1048576 }, { "FFFFFF", 16777215 },
  { "1000000", 16777216 }, { "FFFFFFF", 268435455 },
  { "10000000", 268435456 }, 
  { "0FFFFFFF", 268435455 }, { "00000000", 0 },
  -- Negative numbers:
  { "FFFFFFFF", -1 }, { "FFFFFFFE", -2 },
  { "FFFFFFF1", -15 }, { "FFFFFFF0", -16 }, 
  { "FFFFFF0F", -241 }, { "FFFFFF00", -256 },
  { "FFFFF0FF", -3841 }, { "FFFFF000", -4096 },
  { "FFFF0FFF", -61441 }, { "FFFF0000", -65536 },
  { "FFF0FFFF", -983041 }, { "FFF00000", -1048576 },
  { "FF0FFFFF", -15728641 }, { "FF000000", -16777216 },
  { "F0FFFFFF", -251658241 }, { "F0000000", -268435456 },
}

for _, test in ipairs(hexTests) do
  local hex = test[1]
  local expected = test[2]
  local actual = MongoDB.Shared.HexUtils.ParseInt32(hex)
  TEST.assert(actual == expected, "HexUtils.ParseInt32 failed for hex: " .. hex .. " expected: " .. expected .. " actual: " .. actual)
end

-- End of tests
return true