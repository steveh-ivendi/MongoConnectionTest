using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using Polly;
using Polly.Timeout;
using Serilog;

namespace PollyRetry.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console().CreateLogger();

            var timeoutPolicy = Policy.TimeoutAsync(30, TimeoutStrategy.Pessimistic);

            try
            {
                Log.Logger.Debug("Starting to get collection");

                var mongoUrl = MongoUrl.Create("mongodb://127.0.0.1:27017/quoteware-bc-local");
                //var mongoUrl = MongoUrl.Create("mongodb://127.0.0.1:27017/my-test-database");
                var mongoClient = new MongoClient(mongoUrl);

                var result = await timeoutPolicy.ExecuteAndCaptureAsync(async () => {
                    if(await ProbeForMongoDbConnection(mongoClient, mongoUrl.DatabaseName, 60, 5000))
                    {
                        var mongoDatabase = mongoClient.GetDatabase(mongoUrl.DatabaseName);
                        var collection = mongoDatabase.GetCollection<ProductRateReadModel>("ProductRateReadModel");

                        Log.Logger.Information(collection.EstimatedDocumentCount() < 1 ? 
                            "Collection is empty" : 
                            $"Collection has an estimated {collection.EstimatedDocumentCount()} documents");

                        return collection.EstimatedDocumentCount();
                    }
                    else
                    {
                        Log.Logger.Warning("Connection to database is not working");

                        return long.MinValue;
                    }
                });

                if(result.Outcome == OutcomeType.Failure)
                {
                    Log.Logger.Error($"ExceptionType: {result.ExceptionType}; FaultType: {result.FaultType}; FinalException: {result.FinalException}");
                }
                else
                {
                    Log.Logger.Information($"Got the value {result.Result}");
                }
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "Failed to get collection");
            }
        }

        // Adapted from https://stackoverflow.com/a/47777725/747649
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
