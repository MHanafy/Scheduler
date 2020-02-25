# Scheduler
A .Net scheduler that handles parallaization and adaptive sleeping on inactivity, can be used to develop long running workers and is specially suited for pulling scenarios

Scheduler takes care of two things:
1. Running a task in parallel, resolving the instance for each thread
2. Sleeping in a progressive fashion when no processing is needed; i.e. when a worker process finds no items to process

This helps building worker roles that need to pull periodically, the sleeping pattern would minimize the impact of the server allowing for more efficient execution.

The level of concurrency and the sleeping behaviour are configurable, To get started, just create a scheduler and either call `Start()` or `StartAndBlock()`

Scheduler is designed to be used via DI, and using the Options pattern
```C#
var scheduler = serviceProvider.GetService<IScheduler>();
//Since we're using Start, the code won't block, and scheduler will keep executing as long as it's in scope
scheduler.Start(async () =>
{
    Console.WriteLine($"{DateTime.Now:hh:mm:ss.ff} Settings-based scheduler");
    //Simulate some work
    await Task.Delay(5000);
    return true;
});
```

We can still create a scheduler without using the service provider, a service provider is still needed to resolve types when calling Start<T>
```C#
var options = new OptionsWrapper<SchedulerSettings>(new SchedulerSettings
{
    ImmediateResume = true, //causes the scheduler to immediately call the function again when it returns true
    Concurrency = 1, //runs the function on a single thread
    InitialSleepTime = 100, //when the function returns false, scheduler will wait for 100ms before calling it again; this number is then multiplied until MaxSleepTime is reached
    MaxSleepTime = 450 //time between calls can't exceed this value, in this case if the function keeps returning false wait times would be 100, 200, 400, then 450 instead of 800
});

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
```

Have a look at the samples project for more information