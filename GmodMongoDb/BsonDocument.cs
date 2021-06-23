using GmodMongoDb.Binding.Annotating;
using GmodMongoDb.Binding;
using GmodMongoDb.Binding.DataTransforming;
using GmodNET.API;
using MongoDB.Bson;
using System;

namespace GmodMongoDb
{
    /// <summary>
    /// Exposes a MongoDB BSON Document to Lua.
    /// </summary>
    /// <remarks>
    /// In Lua you can get a BSON document by querying a collection using <see cref="MongoCollection.Find(BsonDocument)"/>.
    /// You can also generate your own BSON document from a table by using <see cref="BsonDocument.Constructor(ILua, BsonDocument)"/>
    /// </remarks>
    [LuaMetaTable("MongoBsonDocument")]
    public class BsonDocument : LuaMetaObjectBinding
    {
        /// <summary>
        /// The native MongoDB BSON Document object
        /// </summary>
        public MongoDB.Bson.BsonDocument RawBsonDocument { get; set; }

        private LuaFunctionReference cachedReference;

        /// <inheritdoc/>
        public BsonDocument(ILua lua, MongoDB.Bson.BsonDocument document)
            : base(lua)
        {
            this.RawBsonDocument = document;
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

        /// <summary>
        /// Creates a new BSON Document from the provided Lua table.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="table">The table to use to build a BSON Document</param>
        /// <returns></returns>
        /// <remarks><see cref="DataTransforming.BetweenBsonDocumentAndTable.TryParse(ILua, out BsonDocument, int, bool)"/> will automatically handle this conversion.</remarks>
        [LuaStatic(IsInitializer = true)]
        public static BsonDocument Constructor(ILua lua, BsonDocument table)
        {
            return table;
        }

        [LuaStatic]
        public static BsonDocument FromLuaTable(ILua lua, LuaTableReference table)
        {
            var rawDocument = new MongoDB.Bson.BsonDocument();

            table.ForEach((key, value) => {
                try 
                { 
                    rawDocument.Add(key.ToString(), BsonTypeMapper.MapToBsonValue(value));
                }
                catch (ArgumentException) 
                {
                    lua.Print($"Warning: skipped key `{key}` since `{value}` cannot be converted to a BSON value.");
                }
            });

            return new BsonDocument(lua, rawDocument);
        }

        /// <summary>
        /// Creates a new BSON Document from the provided json.
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="json"></param>
        /// <returns></returns>
        [LuaStatic]
        public static BsonDocument FromJson(ILua lua, string json)
        {
            return new BsonDocument(lua, MongoDB.Bson.BsonDocument.Parse(json));
        }

        /// <summary>
        /// Converts the BSON Document to a JSON string
        /// </summary>
        /// <returns>The JSON representation of this BSON Document</returns>
        [LuaMethod]
        public string ToJson()
            => RawBsonDocument.ToJson();

        [LuaMethod("__index")]
        public object Index(string key)
        {
            object result = null;

            if (this.RawBsonDocument.TryGetValue(key, out BsonValue bsonValue))
                result = BsonTypeMapper.MapToDotNetValue(bsonValue);

            if (result != null)
                return result;

            if (this.MetaTableTypeId == null)
                return null;

            lua.PushMetaTable((int) this.MetaTableTypeId);
            lua.GetField(-1, key);

            result = TypeTools.PullType(lua, -1);
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

            foreach (string k in this.RawBsonDocument.Names)
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
                object nextValue = BsonTypeMapper.MapToDotNetValue(this.RawBsonDocument[nextKey]);

                lua.PushString(nextKey);
                stack++;

                stack += TypeTools.PushType(lua, nextValue?.GetType(), nextValue);
            }

            return stack;
        }

        /// <summary>
        /// Call this function instead of using <c language="Lua">pairs</c>.
        /// </summary>
        /// <remarks>
        /// The __pairs metamethod is implemented, but won't work in Garry's Mod. If Garry's Mod updates to a version >= Lua 5.2 it will.
        /// Read more about pairs in <a href="https://www.lua.org/manual/5.3/manual.html#pdf-next">the Lua manual</a>
        /// </remarks>
        /// <example>
        /// You can loop over the values in a BSON Document like so:
        /// <code language="Lua">
        /// for key, value in bsonDocument:Pairs() do
        ///     print(key, value)
        /// end
        /// </code>
        /// </example>
        /// <returns>Multiple values: the next function that iterates the object, the object/table itself and nil</returns>
        [LuaMethod("__pairs")] // Won't work until Garry's Mod updates from Lua 5.1 => 5.2
        [LuaMethod("Pairs")]
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

        /// <summary>
        /// Converts the BSON Document to a json string
        /// </summary>
        /// <returns>The JSON representation of this BSON Document</returns>
        [LuaMethod("__tostring")]
        public override string ToString()
            => ToJson();

        /*
         * Comparison operators
         */
        // TODO: All below are untested
        [LuaMethod("__lt")]
        public bool LessThan(BsonDocument other)
            => RawBsonDocument < other.RawBsonDocument;

        [LuaMethod("__le")]
        public bool LessOrEqual(BsonDocument other)
            => RawBsonDocument <= other.RawBsonDocument;

        [LuaMethod("__eq")]
        public bool Equals(BsonDocument other)
            => RawBsonDocument == other.RawBsonDocument;
    }
}
