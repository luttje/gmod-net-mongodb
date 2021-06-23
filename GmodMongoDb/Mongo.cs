using GmodMongoDb.Binding.Annotating;
using GmodMongoDb.Binding;
using GmodNET.API;
using MongoDB.Driver;
using System;
using System.Net.Sockets;

namespace GmodMongoDb
{
    /// <summary>
    /// The main Mongo library form which you can instantiate MongoDB objects like the connection client (<see cref="Mongo.NewClient(string)"/>).
    /// </summary>
    [LuaMetaTable("Mongo")]
    public class Mongo : LuaMetaObjectBinding, IDisposable
    {
        private static Mongo instance;
        private MongoDB.Driver.MongoClient client;

        /// <inheritdoc/>
        public Mongo(ILua lua)
            : base(lua)
        {
            if (instance != null)
                throw new InvalidOperationException("A Mongo instance already existed! You shouldn't load this module twice.");

            ReferenceManager.Add(this);

            instance = this;
        }

        /// <summary>
        /// Initiates a new MongoClient. Only one MongoClient should exist and it should be reused.
        /// </summary>
        /// <remarks>
        /// <a href="mongodb.github.io/mongo-csharp-driver/2.2/reference/driver/connecting/#mongo-client">View the relevant .NET MongoDB Driver documentation</a>
        /// </remarks>
        /// <example>
        /// Example how to call this method from Lua in order to connect to a database.
        /// <code language="Lua">
        /// client = mongo.NewClient("mongodb://username_here:password_here@127.0.0.1:27017/database_here?retryWrites=true&amp;w=majority")
        /// </code>
        /// </example>
        /// <param name="connectionString">The connection string with connection information</param>
        /// <returns>The MongoClient which will interface with database</returns>
        [LuaMethod]
        public static MongoClient NewClient(string connectionString)
        {
            return instance.CreateNewClient(connectionString);
        }

        /// <summary>
        /// Creates a new BSON Document from the provided Lua table.
        /// </summary>
        /// <param name="table">The table to use to build a BSON Document</param>
        /// <returns></returns>
        /// <remarks><see cref="DataTransforming.BetweenBsonDocumentAndTable.TryParse(ILua, out MongoBsonDocument, int, bool)"/> will automatically handle this conversion.</remarks>
        [LuaMethod]
        public static MongoBsonDocument NewBsonDocument(MongoBsonDocument table)
        {
            return table;
        }

        /// <summary>
        /// Creates a new BSON Document from the provided json.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [LuaMethod]
        public static MongoBsonDocument NewBsonDocument(string json)
        {
            return MongoBsonDocument.FromJson(instance.lua, json);
        }

        private MongoClient CreateNewClient(string connectionString)
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

        /// <summary>
        /// Disposes the MongoDB client connection
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            if (this.client == null)
                return;

            ClusterRegistry.Instance.UnregisterAndDisposeCluster(this.client.Cluster);
        }
    }
}
