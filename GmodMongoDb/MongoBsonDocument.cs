using GmodMongoDb.Binding.Annotating;
using GmodMongoDb.Binding;
using GmodMongoDb.Binding.DataTransforming;
using GmodNET.API;
using MongoDB.Bson;
using System;

namespace GmodMongoDb
{
    [LuaMetaTable("MongoBsonDocument")]
    public class MongoBsonDocument : LuaMetaObjectBinding
    {
        public BsonDocument BsonDocument { get; set; }
        private LuaFunctionReference cachedReference;

        public MongoBsonDocument(ILua lua, BsonDocument document)
            : base(lua)
        {
            this.BsonDocument = document;
            cachedReference = null;

            // TODO:
            //this.BsonDocument.Add
            //this.BsonDocument.AddRange
            //this.BsonDocument.Clear
            //this.BsonDocument.Clone
            //this.BsonDocument.CompareTo
            //this.BsonDocument.CompareTypeTo
            //this.BsonDocument.Contains
            //this.BsonDocument.ContainsValue
            //this.BsonDocument.DeepClone
            //this.BsonDocument.ElementCount <-- property
            //this.BsonDocument.Elements <-- property
            //this.BsonDocument.GetElement
            //this.BsonDocument.GetEnumerator
            //this.BsonDocument.GetValue
            //this.BsonDocument.IndexOfName
            //this.BsonDocument.InsertAt
            //all this.BsonDocument.Is* Properties
            //this.BsonDocument.Merge
            //this.BsonDocument.Names <-- property
            //this.BsonDocument.Remove
            //this.BsonDocument.RemoveAt
            //this.BsonDocument.RemoveElement
            //this.BsonDocument.Set
            //this.BsonDocument.SetElement
            //all this.BsonDocument.To* Methods
            //this.BsonDocument.TryGetElement
            //this.BsonDocument.TryGetValue
            //this.BsonDocument.Values <-- property
        }
        internal static MongoBsonDocument FromLuaTable(ILua lua, LuaTableReference filterTable)
        {
            var rawDocument = new BsonDocument();

            filterTable.ForEach((key, value) => rawDocument.Add(key.ToString(), BsonTypeMapper.MapToBsonValue(value)));

            return new MongoBsonDocument(lua, rawDocument);
        }

        [LuaMethod("__index")]
        public object Index(string key)
        {
            object result = null;

            if (this.BsonDocument.TryGetValue(key, out BsonValue bsonValue))
                result = BsonTypeMapper.MapToDotNetValue(bsonValue);

            if (result != null)
                return result;

            if (this.MetaTableTypeId == null)
                return null;

            lua.PushMetaTable((int) this.MetaTableTypeId);
            lua.GetField(-1, key);

            result = BindingHelper.PullType(lua, -1);
            lua.Pop(1); // pop the metatable

            return result;
        }

        private int NextKey(ILua lua)
        {
            _ = new LuaReference(lua, 1); // this pops the table
            string key = lua.GetString(1);

            // Pop the key if it's there
            lua.Pop(key != string.Empty ? 1 : 0);

            int stack = 0;

            string nextKey = null;

            foreach (string k in this.BsonDocument.Names)
            {
                // Stop if we've reached the next key
                if (k == key)
                    break;

                nextKey = k;

                // Stop if no key was specified (so we want the first item)
                if (key == string.Empty)
                    break;
            }

            if (nextKey != null)
            {
                object nextValue = BsonTypeMapper.MapToDotNetValue(this.BsonDocument[nextKey]);

                lua.PushString(nextKey);
                stack++;

                stack += BindingHelper.PushType(lua, nextValue?.GetType(), nextValue);
            }

            return stack;
        }

        [LuaMethod("__pairs")] // Won't work until Garry's Mod updates from Lua 5.1 => 5.2
        [LuaMethod("Pairs")] // for key, value in mongoBsonDocument:Pairs() do
        public object[] Pairs()
        {
            if(this.cachedReference == null)
            {
                lua.PushManagedFunction(NextKey);
                this.cachedReference = new LuaFunctionReference(lua);
            }

            return new object[]{
                this.cachedReference,
                (int)this.Reference,
                null
            }; // next, t, nil
        }

        [LuaMethod("__tostring")]
        public override string ToString()
        {
            return this.BsonDocument.ToJson();
        }

        /*
         * Comparison operators
         */
        // TODO: All below are untested
        [LuaMethod("__lt")]
        public bool LessThan(MongoBsonDocument other)
        {
            return this.BsonDocument < other.BsonDocument;
        }

        [LuaMethod("__le")]
        public bool LessOrEqual(MongoBsonDocument other)
        {
            return this.BsonDocument <= other.BsonDocument;
        }

        [LuaMethod("__eq")]
        public bool Equals(MongoBsonDocument other)
        {
            return this.BsonDocument == other.BsonDocument;
        }
    }
}
