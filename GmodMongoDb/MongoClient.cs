using GmodMongoDb.Binding.Annotating;
using GmodMongoDb.Binding;
using GmodMongoDb.Binding.DataTransforming;
using GmodNET.API;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Net.Sockets;

namespace GmodMongoDb
{
    /// <summary>
    /// Exposes a MongoDB connection client to Lua.
    /// </summary>
    /// <remarks>
    /// In Lua you can get a client by using <see cref="Constructor"/> 
    /// </remarks>
    [LuaMetaTable("MongoClient")]
    public class MongoClient : LuaMetaObjectBinding, IDisposable
    {
        private readonly IMongoClient client;

        /// <inheritdoc/>
        public MongoClient(ILua lua, IMongoClient client)
            :base(lua)
        {
            this.client = client;

            ReferenceManager.Add(this);

            // TODO:
            //this.client.Watch;
            //this.client.WatchAsync;
        }

        /// <summary>
        /// Initiates a new MongoClient. Only one MongoClient should exist and it can be reused for multiple databases.
        /// </summary>
        /// <remarks>
        /// <a href="mongodb.github.io/mongo-csharp-driver/2.2/reference/driver/connecting/#mongo-client">View the relevant .NET MongoDB Driver documentation</a>
        /// </remarks>
        /// <example>
        /// Example how to call this method from Lua in order to connect to a database.
        /// <code language="Lua">
        /// client = MongoClient("mongodb://username_here:password_here@127.0.0.1:27017/database_here?retryWrites=true&amp;w=majority")
        /// </code>
        /// </example>
        /// <param name="lua"></param>
        /// <param name="connectionString">The connection string with connection information</param>
        /// <returns>The MongoClient which will interface with database</returns>
        [LuaMethod(IsConstructor = true)]
        public static MongoClient Constructor(ILua lua, string connectionString)
        {
            lua.Print($"Connecting to database. MongoClient created.");

            var url = new MongoUrl(connectionString);
            var settings = MongoClientSettings.FromUrl(url);
            settings.HeartbeatInterval = TimeSpan.FromSeconds(1);
            settings.ClusterConfigurator = builder =>
            {
                // A lingering socket causes the module to become not-unloadable.
                static void SocketConfigurator(Socket s)
                {
                    s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
                };

                builder.ConfigureTcp(tcp => tcp.With(socketConfigurator: (Action<Socket>)SocketConfigurator));
            };

            return new MongoClient(lua, new MongoDB.Driver.MongoClient(settings));
        }

        /// <summary>
        /// Disposes the MongoDB client connection
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            if (client == null)
                return;

            ClusterRegistry.Instance.UnregisterAndDisposeCluster(client.Cluster);
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
        public List<MongoDB.Bson.BsonDocument> ListDatabases()
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

        [LuaMethod("__eq")]
        public bool Equals(MongoClient other)
            => client == other.client;
    }
}
