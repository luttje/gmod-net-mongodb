using GmodMongoDb.Binding.Annotating;
using GmodMongoDb.Binding;
using GmodMongoDb.Binding.DataTransforming;
using GmodNET.API;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;

namespace GmodMongoDb
{
    /// <summary>
    /// Exposes a MongoDB connection client to Lua.
    /// </summary>
    /// <remarks>
    /// In Lua you can get a client by using <see cref="Mongo.NewClient(string)"/> 
    /// </remarks>
    [LuaMetaTable("MongoClient")]
    public class MongoClient : LuaMetaObjectBinding
    {
        private readonly MongoDB.Driver.MongoClient client;

        /// <inheritdoc/>
        public MongoClient(ILua lua, MongoDB.Driver.MongoClient client)
            :base(lua)
        {
            this.client = client;

            // TODO:
            //this.client.Watch;
            //this.client.WatchAsync;
        }

        [LuaMethod]
        public void DropDatabase(string name)
            => client.DropDatabase(name);

        [LuaMethod]
        public async void DropDatabaseAsync(string name, LuaFunctionReference callback = null)
        {
            await client.DropDatabaseAsync(name);

            if(callback != null)
                callback.CallFromAsync();
        }

        [LuaMethod]
        public List<string> ListDatabaseNames()
            => client.ListDatabaseNames().ToList();

        [LuaMethod]
        public async void ListDatabaseNamesAsync(LuaFunctionReference callback)
            => callback.CallFromAsync(await (await client.ListDatabaseNamesAsync()).ToListAsync());

        [LuaMethod]
        public List<BsonDocument> ListDatabases()
            => client.ListDatabases().ToList();

        [LuaMethod]
        public async void ListDatabasesAsync(LuaFunctionReference callback)
            => callback.CallFromAsync(await (await client.ListDatabasesAsync()).ToListAsync());

        /// <summary>
        /// Queries the connection for the database object identified by the given name. If it does not exist it will create it.
        /// </summary>
        /// <remarks>
        /// <a href="mongodb.github.io/mongo-csharp-driver/2.2/reference/driver/connecting/#mongo-database">View the relevant .NET MongoDB Driver documentation</a>
        /// </remarks>
        /// <example>
        /// In Lua you can get the database by asking a <see cref="MongoClient"/> for it:
        /// <code language="Lua"><![CDATA[
        /// local database = client:GetDatabase("database_name")
        /// ]]></code>
        /// </example>
        /// <param name="name">The database name</param>
        /// <returns>The database object</returns>
        [LuaMethod]
        public MongoDatabase GetDatabase(string name)
            => new(lua, client.GetDatabase(name));
    }
}
