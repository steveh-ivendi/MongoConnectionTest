using System;
using System.Collections.Generic;
using MongoDB.Driver;
using Serilog;

namespace PollyRetry.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console().CreateLogger();

            try
            {
                Log.Logger.Debug("Starting to get collection");

                var mongoUrl = MongoUrl.Create("mongodb://127.0.0.1:27017/quoteware-bc-local");
                var mongoClient = new MongoClient(mongoUrl);
                var mongoDatabase = mongoClient.GetDatabase(mongoUrl.DatabaseName);
                var collection = mongoDatabase.GetCollection<ProductRateReadModel>("ProductRateReadModel");

                Log.Logger.Information(collection.EstimatedDocumentCount() < 1 ? "Collection is empty" : $"Collection has an estimated {collection.EstimatedDocumentCount()} documents");
            }
            catch(Exception e)
            {
                Log.Logger.Error(e, "Failed to get collection");
            }
        }
    }

    public class ProductRateReadModel
    {
        public Guid Id {get; set;}
    }
}
