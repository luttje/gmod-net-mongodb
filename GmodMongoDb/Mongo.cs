using GmodMongoDb.Binding.Annotating;
using GmodMongoDb.Binding;
using GmodNET.API;
using MongoDB.Driver;
using System;
using System.Net.Sockets;

namespace GmodMongoDb
{
    /// <summary>
    /// The main Mongo library form which you can instantiate a new client.
    /// </summary>
    [LuaMetaTable("Mongo")]
    public class Mongo : LuaMetaObjectBinding, IDisposable
    {
        private MongoDB.Driver.MongoClient client;

        public Mongo(ILua lua)
            : base(lua)
        {
            ReferenceManager.Add(this);
        }

        /// <summary>
        /// Initiates a new MongoClient. Only one MongoClient should exist and it should be reused.
        /// Learn more at <a href="http://mongodb.github.io/mongo-csharp-driver/2.12/reference/driver/connecting/#mongo-client">the .NET Driver MongoClient documentation</a>
        /// </summary>
        /// <example>
        /// Example how to call this method from Lua in order to connect to a database.
        /// <code language="Lua">
        /// client = mongo:NewClient("mongodb://username_here:password_here@127.0.0.1:27017/database_here?retryWrites=true&amp;w=majority")
        /// </code>
        /// </example>
        /// <param name="connectionString">The connection string with connection information</param>
        /// <returns>The MongoClient which will interface with database</returns>
        [LuaMethod]
        public MongoClient NewClient(string connectionString)
        {
            lua.Print($"Connecting to database. MongoClient created.");

            var url = new MongoUrl(connectionString);
            var settings = MongoClientSettings.FromUrl(url);
            settings.HeartbeatInterval = TimeSpan.FromSeconds(1);
            settings.ClusterConfigurator = builder =>
            {
                // A lingering socket causes the module to become not-unloadable.
                static void SocketConfigurator(Socket s) {
                    s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
                };

                builder.ConfigureTcp(tcp => tcp.With(socketConfigurator: (Action<Socket>)SocketConfigurator));
            };
            client = new MongoDB.Driver.MongoClient(settings);

            return new MongoClient(this.lua, client);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            if (this.client == null)
                return;

            ClusterRegistry.Instance.UnregisterAndDisposeCluster(this.client.Cluster);
        }
    }
}
