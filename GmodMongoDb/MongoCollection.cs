using GmodNET.API;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using GmodMongoDb.Binding.DataTransforming;
using GmodMongoDb.Binding.Annotating;
using MongoDB.Bson;
using GmodMongoDb.Binding;

namespace GmodMongoDb
{
    [LuaMetaTable("MongoCollection")]
    public class MongoCollection : LuaMetaObjectBinding
    {
        protected MongoDB.Driver.IMongoCollection<MongoDB.Bson.BsonDocument> collection;

        public MongoCollection(ILua lua, MongoDB.Driver.IMongoCollection<MongoDB.Bson.BsonDocument> collection)
            : base(lua)
        {
            this.collection = collection;

            // TODO:
            //this.collection.Aggregate<TResult>
            //this.collection.AggregateAsync
            //this.collection.AggregateToCollection<TResult>
            //this.collection.AggregateToCollectionAsync
            //this.collection.AsQueryable <-- Seems complex
            //this.collection.BulkWrite
            //this.collection.BulkWriteAsync
            //this.collection.CollectionNamespace <-- property
            //this.collection.CountDocuments
            //this.collection.CountDocumentsAsync
            //this.collection.Database <-- property
            //this.collection.DeleteMany
            //this.collection.DeleteManyAsync
            //this.collection.DeleteOne
            //this.collection.DeleteOneAsync
            //this.collection.Distinct
            //this.collection.DistinctAsync
            //this.collection.DocumentSerializer <-- property, low priority
            //this.collection.EstimatedDocumentCount
            //this.collection.EstimatedDocumentCountAsync
            //this.collection.Indexes <-- property
            //this.collection.InsertMany
            //this.collection.InsertManyAsync
            //this.collection.InsertOne
            //this.collection.InsertOneAsync
            //this.collection.MapReduce
            //this.collection.MapReduceAsync
            //this.collection.OfType <-- low priority
            //this.collection.ReplaceOne
            //this.collection.ReplaceOneAsync
            //this.collection.Settings <-- property, low priority
            //this.collection.ToBson
            //this.collection.ToBsonDocument
            //this.collection.ToJson
            //override this.collection.ToString
            //this.collection.UpdateMany
            //this.collection.UpdateManyAsync
            //this.collection.UpdateOne
            //this.collection.UpdateOneAsync
            //this.collection.Watch
            //this.collection.WatchAsync
            //this.database.WithReadConcern <-- low priority
            //this.database.WithReadPreference <-- low priority
            //this.database.WithWriteConcern <-- low priority
        }

        [LuaMethod(IsOverloaded = true)]
        [LuaMethod("FindSync", IsOverloaded = true)]
        public List<MongoDB.Bson.BsonDocument> Find(string filterJson)
        {
            var results = collection.Find(BsonDocument.Parse(filterJson));

            return results.ToList();
        }

        [LuaMethod(IsOverloaded = true)]
        [LuaMethod("FindSync", IsOverloaded = true)]
        public List<MongoDB.Bson.BsonDocument> Find(LuaTableReference filterTable)
        {
            return Find(MongoBsonDocument.FromLuaTable(lua, filterTable));
        }

        [LuaMethod(IsOverloaded = true)]
        [LuaMethod("FindSync", IsOverloaded = true)]
        public List<MongoDB.Bson.BsonDocument> Find(MongoBsonDocument filter)
        {
            var results = collection.Find(filter.BsonDocument);

            return results.ToList();
        }

        // TODO: All below are untested
        [LuaMethod]
        public async void FindAsync(string filterJson, LuaFunctionReference callback)
        {
            var results = await collection.FindAsync(BsonDocument.Parse(filterJson));
            var resultsList = await results.ToListAsync();

            callback.CallFromAsync(resultsList);
        }

        [LuaMethod]
        public MongoBsonDocument FindOneAndDelete(string filterJson)
        {
            var result = collection.FindOneAndDelete(BsonDocument.Parse(filterJson));

            return new MongoBsonDocument(lua, result);
        }

        [LuaMethod]
        public async void FindOneAndDeleteAsync(string filterJson, LuaFunctionReference callback)
        {
            var result = await collection.FindOneAndDeleteAsync(BsonDocument.Parse(filterJson));

            callback.CallFromAsync(result);
        }


        [LuaMethod]
        public MongoBsonDocument FindOneAndReplace(string filterJson, BsonDocument replacement)
        {
            var result = collection.FindOneAndReplace(BsonDocument.Parse(filterJson), replacement);

            return new MongoBsonDocument(lua, result);
        }

        [LuaMethod]
        public async void FindOneAndReplaceAsync(string filterJson, BsonDocument replacement, LuaFunctionReference callback)
        {
            var result = await collection.FindOneAndReplaceAsync(BsonDocument.Parse(filterJson), replacement);

            callback.CallFromAsync(result);
        }

        // TODO:
        //[LuaMethod]
        //public MongoBsonDocument FindOneAndUpdate(string filterJson, UpdateDefinition<BsonDocument> updateDefinition)
        //{
        //    var result = collection.FindOneAndUpdate(BsonDocument.Parse(filterJson), updateDefinition);

        //    return new MongoBsonDocument(lua, result);
        //}

        //[LuaMethod]
        //public async void FindOneAndUpdateAsync(string filterJson, UpdateDefinition<BsonDocument> updateDefinition, LuaFunctionReference callback)
        //{
        //    var result = await collection.FindOneAndUpdateAsync(BsonDocument.Parse(filterJson), updateDefinition);

        //    callback.CallFromAsync(result);
        //}
    }
}
