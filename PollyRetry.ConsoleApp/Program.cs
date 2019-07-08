using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;
using Serilog;

namespace PollyRetry.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console().CreateLogger();

            var timeoutList = new List<TimeSpan>{
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(8),
                TimeSpan.FromSeconds(13),
                TimeSpan.FromSeconds(21),
                TimeSpan.FromSeconds(34)
            };

            Log.Logger.Information("Starting...");

            var count = 0;

            var answer = await Policy.Handle<Exception>().WaitAndRetryAsync(timeoutList, (e, ts, i, c) => {
                if (e is TimeoutException)
                {
                    Log.Logger.Error($"Timed out! Timespan: {ts}; Retry: {i}; Context: {c}");
                }
                else
                {
                    Log.Logger.Error(e, $"Timespan: {ts}; Retry: {i}; Context: {c}");
                }
            })
            .ExecuteAndCaptureAsync(async () => {
                if(count++ < 10)
                {
                    if (count % 2 == 0)
                    {
                        throw new ArgumentNullException("myParam");
                    }

                    if (count % 5 == 0)
                    {
                        throw new ArithmeticException("something awful")
                    }

                    throw new TimeoutException("No, you're the victim");
                }

                await Task.Delay(TimeSpan.FromSeconds(30));
                return 42;
            });

            if(answer.Outcome == OutcomeType.Successful)
            {
                Log.Logger.Information($"Received answer: {answer.Result}");
            }
            else
            {
                Log.Logger.Fatal($"No answer returned: {answer.FaultType}");
            }
        }
    }
}
