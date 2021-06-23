using GmodMongoDb.Binding;
using GmodMongoDb.Binding.DataTransforming;
using GmodNET.API;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GmodMongoDb.DataTransforming
{
    /// <summary>
    /// Transformers from a native BsonDocument to a Lua table or vice versa.
    /// </summary>
    public sealed class BetweenBsonDocumentAndTable : LuaValueTransformer<MongoBsonDocument>
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

        /// <inheritdoc/>
        public override int Convert(ILua lua, MongoBsonDocument document)
        {
            CreateLuaBsonDocument(lua, document.BsonDocument);
            return 1;
        }

        /// <inheritdoc/>
        public override bool TryParse(ILua lua, out MongoBsonDocument document, int stackPos = -1, bool forceKeepOnStack = false)
        {
            var table = new LuaTableReference(lua, stackPos, forceKeepOnStack);

            document = MongoBsonDocument.FromLuaTable(lua, table);

            return true;
        }
    }
}
