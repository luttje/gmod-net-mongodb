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
    public class BetweenTableAndBsonDocument : BaseLuaValueTransformer<MongoBsonDocument>
    {
        /// <summary>
        /// Create a <see cref="MongoBsonDocument"/> object table with Lua metatable for the given BsonDocument. Pushes the object table to the stack.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="rawDocument">The true BsonDocument to encapsulate</param>
        private static void CreateLuaBsonDocument(ILua lua, BsonDocument rawDocument)
        {
            MongoBsonDocument document = new(lua, rawDocument);

            BindingHelper.GenerateUserDataFromObject(lua, document);
        }

        public override int Convert(ILua lua, MongoBsonDocument results)
        {
            return 0;
        }

        public override MongoBsonDocument Parse(ILua lua)
        {
            throw new NotImplementedException();
        }
    }
}
