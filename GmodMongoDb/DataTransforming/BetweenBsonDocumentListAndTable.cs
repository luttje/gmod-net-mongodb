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
    public sealed class BetweenBsonDocumentListAndTable : LuaValueTransformer<List<MongoDB.Bson.BsonDocument>>
    {
        /// <summary>
        /// Create a <see cref="BsonDocument"/> object table with Lua metatable for the given BsonDocument. Pushes the object table to the stack.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="rawDocument">The true BsonDocument to encapsulate</param>
        private static void CreateLuaBsonDocument(ILua lua, MongoDB.Bson.BsonDocument rawDocument)
        {
            BsonDocument document = new(lua, rawDocument);

            TypeTools.GenerateUserDataFromObject(lua, document);
        }

        /// <inheritdoc/>
        public override int Convert(ILua lua, List<MongoDB.Bson.BsonDocument> results)
        {
            lua.CreateTable();
            int i = 1;

            foreach (MongoDB.Bson.BsonDocument document in results)
            {
                CreateLuaBsonDocument(lua, document);

                lua.PushNumber(i++);
                lua.Insert(-2);
                lua.RawSet(-3);
            }

            return 1;
        }

        /// <inheritdoc/>
        public override bool TryParse(ILua lua, out List<MongoDB.Bson.BsonDocument> value, int stackPos = -1, bool forceKeepOnStack = false)
        {
            throw new NotImplementedException();
        }
    }
}
