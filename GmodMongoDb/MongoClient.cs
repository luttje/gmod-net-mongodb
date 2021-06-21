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
    [LuaMetaTable("MongoClient")]
    public class MongoClient : LuaMetaObjectBinding
    {
        protected MongoDB.Driver.MongoClient client;

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
        {
            this.client.DropDatabase(name);
        }

        [LuaMethod]
        public async void DropDatabaseAsync(string name, LuaFunctionReference callback = null)
        {
            await this.client.DropDatabaseAsync(name);

            if(callback != null)
                callback.CallFromAsync();
        }

        [LuaMethod]
        public List<string> ListDatabaseNames()
        {
            var databases = this.client.ListDatabaseNames();

            return databases.ToList();
        }

        [LuaMethod]
        public async void ListDatabaseNamesAsync(LuaFunctionReference callback)
        {
            var databases = await this.client.ListDatabaseNamesAsync();
            var databaseNames = await databases.ToListAsync();

            callback.CallFromAsync(databaseNames);
        }

        [LuaMethod]
        public List<BsonDocument> ListDatabases()
        {
            var databases = this.client.ListDatabases();

            return databases.ToList();
        }

        [LuaMethod]
        public async void ListDatabasesAsync(LuaFunctionReference callback)
        {
            var databases = await this.client.ListDatabasesAsync();
            var databasesList = await databases.ToListAsync();

            callback.CallFromAsync(databasesList);
        }

        [LuaMethod]
        public MongoDatabase GetDatabase(string name)
        {
            var database = this.client.GetDatabase(name);

            return new MongoDatabase(this.lua, database);
        }
    }
}
