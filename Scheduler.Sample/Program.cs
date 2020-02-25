using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MHanafy.Scheduling.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = SetupDi();
            /* Scheduler is designed to be used via DI, and using the Options pattern
             * Let's Start a scheduler using ServiceProvider, which will use the settings.json file for configuration.
             * Modify settings.json and reload to see different behaviour
             */
            var scheduler = serviceProvider.GetService<IScheduler>();
            //Since we're using Start, the code won't block, and scheduler will keep executing as long as it's in scope
            scheduler.Start(async () =>
            {
                Console.WriteLine($"{DateTime.Now:hh:mm:ss.ff} Settings-based scheduler");
                //Simulate some work
                await Task.Delay(5000);
                return true;
            });

            
            //We can still create a scheduler without using the service provider, a service provider is still needed to resolve types when calling Start<T>
            var options = new OptionsWrapper<SchedulerSettings>(new SchedulerSettings
            {
                ImmediateResume = true, //causes the scheduler to immediately call the function again when it returns true
                Concurrency = 1, //runs the function on a single thread
                InitialSleepTime = 100, //when the function returns false, scheduler will wait for 100ms before calling it again; this number is then multiplied until MaxSleepTime is reached
                MaxSleepTime = 450 //time between calls can't exceed this value, in this case if the function keeps returning false wait times would be 100, 200, 400, then 450 instead of 800
            });

            var manualScheduler = new Scheduler(options, serviceProvider);
            manualScheduler.Start(async () =>
            {
                //Since concurrency is set to 1, we expect this message twice every 100 milliseconds
                Console.WriteLine($"{DateTime.Now:hh:mm:ss.ff} Manual non-blocking scheduler ");
                //returning false would cause the scheduler to wait according to the wait pattern mentioned above
                return false;
            });

            /* Let's use the same settings, but will change concurrency to 3 to have three different threads.
             * this time we'll resolve an instance of a test type; you can control which instance is returned by changing DI resolution (i.e. Transient, Scoped or Singleton)
             * In this example, we've set the type to Scoped, hence we'll get three different instances of the Test class
             */
            options.Value.Concurrency = 3; //this won't impact the manualScheduler as settings are only read in the constructor
            var blockingScheduler = new Scheduler(options, serviceProvider);
            //This is a blocking call, so the program will keep executing and any code beyond this call won't be reached until the program is terminated
            blockingScheduler.StartAndBlock<Test>(async (Test t) =>
            {
                //here we're using the instance resolved by the scheduler for this thread.
                Console.WriteLine($"{DateTime.Now:hh:mm:ss.ff} Blocking scheduler Test instance: {t.Guid}");
                //wait for a bit
                await Task.Delay(2000);
                //returning false will cause the scheduler to call the function immediately if ImmediateResume is set to true, otherwise calls after waiting for InitialSleepTime
                return true;
            });
        }

        /// <summary>
        /// A simple class to showcase how to use custom objects for each scheduler thread, typically this will be a service class
        /// </summary>
        public class Test
        {
            public readonly Guid Guid;
            public Test()
            {
                Guid = Guid.NewGuid();
            }
            }

        private static IServiceProvider SetupDi()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("settings.json")
                .Build();
            
            var serviceProvider = new ServiceCollection()
                .AddOptions()
                .Configure<SchedulerSettings>(config.GetSection("SchedulerSettings"))
                .AddScoped<IScheduler, Scheduler>()
                .AddScoped<Test>()
                .BuildServiceProvider();

            return serviceProvider;
        }
    }
}
