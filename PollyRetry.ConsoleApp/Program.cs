using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using Serilog;

namespace PollyRetry.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console().CreateLogger();

            try
            {
                Log.Logger.Debug("Starting to get collection");

                var mongoUrl = MongoUrl.Create("mongodb://127.0.0.1:27017/quoteware-bc-local");
                var mongoClient = new MongoClient(mongoUrl);

                if(await ProbeForMongoDbConnection(mongoClient, mongoUrl.DatabaseName, 60, 500))
                {
                    var mongoDatabase = mongoClient.GetDatabase(mongoUrl.DatabaseName);
                    var collection = mongoDatabase.GetCollection<ProductRateReadModel>("ProductRateReadModel");

                    Log.Logger.Information(collection.EstimatedDocumentCount() < 1 ? 
                        "Collection is empty" : 
                        $"Collection has an estimated {collection.EstimatedDocumentCount()} documents");
                }
                else
                {
                    Log.Logger.Warning("Connection to database is not working");
                }
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "Failed to get collection");
            }
        }
        private static async Task<bool> ProbeForMongoDbConnection(MongoClient mongoClient, string dbName, int retryCount = 6, int waitMilliseconds = 1666)
        {
            var isAlive = false;

            for (var k = 0; k < retryCount; k++)
            {
                mongoClient.GetDatabase(dbName);
                var server = mongoClient.Cluster.Description.Servers.FirstOrDefault();
                isAlive = (server != null &&
                            server.HeartbeatException == null &&
                            server.State == MongoDB.Driver.Core.Servers.ServerState.Connected);
                if (isAlive)
                {
                    break;
                }
                
                Log.Logger.Debug("Attempt {attempt}, waiting for {wait}ms", k, waitMilliseconds);
                await Task.Delay(TimeSpan.FromMilliseconds(waitMilliseconds));
            }

            return isAlive;
        }
    }

    public class ProductRateReadModel
    {
        public Guid Id { get; set; }
    }
}
