using GmodMongoDb.Binding.Annotating;
using GmodMongoDb.Binding;
using GmodNET.API;

namespace GmodMongoDb
{
    /// <summary>
    /// Exposes a MongoDB database to Lua.
    /// </summary>
    /// <remarks>
    /// In Lua you can get the collection by using <see cref="MongoClient.GetDatabase(string)"/> 
    /// </remarks>
    [LuaMetaTable("MongoDatabase")]
    public class MongoDatabase : LuaMetaObjectBinding
    {
        private readonly MongoDB.Driver.IMongoDatabase database;

        [LuaProperty]
        public MongoClient Client => new(lua, database.Client);

        /// <inheritdoc/>
        public MongoDatabase(ILua lua, MongoDB.Driver.IMongoDatabase database)
            : base(lua)
        {
            this.database = database;

            // TODO:
            //this.database.DropCollection
            //this.database.Aggregate<TResult>
            //this.database.AggregateAsync
            //this.database.AggregateToCollection<TResult>
            //this.database.AggregateToCollectionAsync
            //this.database.Client <-- property, would require some modifications to the Binding namespace
            //this.database.CreateCollection
            //this.database.CreateCollectionAsync
            //this.database.CreateView<TDocument,TResult>
            //this.database.CreateViewAsync
            //this.database.DatabaseNamespace <-- property
            //this.database.DropCollection
            //this.database.DropCollectionAsync
            //this.database.ListCollectionNames
            //this.database.ListCollectionNamesAsync
            //this.database.ListCollections
            //this.database.ListCollectionsAsync
            //this.database.RenameCollection
            //this.database.RenameCollectionAsync
            //this.database.RunCommand
            //this.database.RunCommandAsync
            //this.database.Settings <-- property, low priority, but once made MongoClient.Constructor should accept this
            //this.database.Watch
            //this.database.WatchAsync
            //this.database.WithReadConcern <-- low priority
            //this.database.WithReadPreference <-- low priority
            //this.database.WithWriteConcern <-- low priority
        }

        /// <summary>
        /// Fetches a MongoDB Collection from this database
        /// </summary>
        /// <example>
        /// This is how you can get the collection in Lua:
        /// <code language="Lua"><![CDATA[
        /// local collection = database:GetCollection("collection_name")
        /// ]]></code>
        /// </example>
        /// <param name="name">The name of the collection</param>
        /// <returns>The retrieved collection</returns>
        [LuaMethod]
        public MongoCollection GetCollection(string name)
            => new(lua, database.GetCollection<MongoDB.Bson.BsonDocument>(name));

        [LuaMethod("__eq")]
        public bool Equals(MongoDatabase other)
            => database == other.database;
    }
}
