﻿using GmodNET.API;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using GmodMongoDb.Binding.DataTransforming;
using GmodMongoDb.Binding.Annotating;
using MongoDB.Bson;
using GmodMongoDb.Binding;

namespace GmodMongoDb
{
    /// <summary>
    /// Exposes a MongoDB collection to Lua.
    /// </summary>
    /// <remarks>
    /// In Lua you can get the collection by using <see cref="MongoDatabase.GetCollection(string)"/> 
    /// </remarks>
    [LuaMetaTable("MongoCollection")]
    public class MongoCollection : LuaMetaObjectBinding
    {
        private readonly IMongoCollection<BsonDocument> collection;

        /// <inheritdoc/>
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

        [LuaMethod]
        [LuaMethod("FindSync")]
        public List<MongoDB.Bson.BsonDocument> Find(MongoBsonDocument filter)
            => collection.Find(filter.BsonDocument).ToList();

        // TODO: All below are untested
        [LuaMethod]
        public async void FindAsync(MongoBsonDocument filter, LuaFunctionReference callback)
            => callback.CallFromAsync(await (await collection.FindAsync(filter.BsonDocument)).ToListAsync());

        [LuaMethod]
        public MongoBsonDocument FindOneAndDelete(MongoBsonDocument filter)
            => new(lua, collection.FindOneAndDelete(filter.BsonDocument));

        [LuaMethod]
        public async void FindOneAndDeleteAsync(MongoBsonDocument filter, LuaFunctionReference callback)
            => callback.CallFromAsync(await collection.FindOneAndDeleteAsync(filter.BsonDocument));


        [LuaMethod]
        public MongoBsonDocument FindOneAndReplace(MongoBsonDocument filter, BsonDocument replacement)
            => new(lua, collection.FindOneAndReplace(filter.BsonDocument, replacement));

        [LuaMethod]
        public async void FindOneAndReplaceAsync(MongoBsonDocument filter, BsonDocument replacement, LuaFunctionReference callback)
            => callback.CallFromAsync(await collection.FindOneAndReplaceAsync(filter.BsonDocument, replacement));

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
