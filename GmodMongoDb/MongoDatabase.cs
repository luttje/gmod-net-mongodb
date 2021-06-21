using GmodMongoDb.Binding.Annotating;
using GmodMongoDb.Binding;
using GmodNET.API;

namespace GmodMongoDb
{
    [LuaMetaTable("MongoDatabase")]
    public class MongoDatabase : LuaMetaObjectBinding
    {
        protected MongoDB.Driver.IMongoDatabase database;

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
            //this.database.Settings <-- property, low priority, but once made NewClient should accept this
            //this.database.Watch
            //this.database.WatchAsync
            //this.database.WithReadConcern <-- low priority
            //this.database.WithReadPreference <-- low priority
            //this.database.WithWriteConcern <-- low priority
        }

        [LuaMethod]
        public MongoCollection GetCollection(string name)
        {
            var collection = this.database.GetCollection<MongoDB.Bson.BsonDocument>(name);

            return new MongoCollection(this.lua, collection);
        }
    }
}
