using GmodMongoDb.Binding;
using GmodNET.API;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GmodMongoDb.Binding.DataTransforming
{
    public sealed class BetweenTableAndBsonDocument : LuaValueTransformer<MongoBsonDocument>
    {
        /// <summary>
        /// Create a <see cref="MongoBsonDocument"/> object table with Lua metatable for the given BsonDocument. Pushes the object table to the stack.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="rawDocument">The true BsonDocument to encapsulate</param>
        private static void CreateLuaBsonDocument(ILua lua, BsonDocument rawDocument)
        {
            MongoBsonDocument document = new(lua, rawDocument);

            TypeConverter.GenerateUserDataFromObject(lua, document);
        }

        public override int Convert(ILua lua, MongoBsonDocument document)
        {
            CreateLuaBsonDocument(lua, document.BsonDocument);
            return 1;
        }

        public override bool TryParse(ILua lua, out MongoBsonDocument document, int stackPos = -1, bool forceKeepOnStack = false)
        {
            var table = new LuaTableReference(lua, -1, forceKeepOnStack);

            document = MongoBsonDocument.FromLuaTable(lua, table);

            return true;
        }
    }
}
