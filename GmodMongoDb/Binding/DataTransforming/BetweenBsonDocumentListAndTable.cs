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
    public class BetweenBsonDocumentListAndTable : BaseLuaValueTransformer<List<BsonDocument>>
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

        public override int Convert(ILua lua, List<BsonDocument> results)
        {
            lua.CreateTable();
            int i = 1;

            foreach (BsonDocument document in results)
            {
                CreateLuaBsonDocument(lua, document);

                lua.PushNumber(i++);
                lua.Insert(-2);
                lua.RawSet(-3);
            }

            return 1;
        }

        public override List<BsonDocument> Parse(ILua lua)
        {
            throw new NotImplementedException();
        }
    }
}
