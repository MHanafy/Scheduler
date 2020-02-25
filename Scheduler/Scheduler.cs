using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MHanafy.Scheduling
{
    public class Scheduler : IScheduler, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly int _initSleepTime;
        private readonly int _maxSleepTime;
        private readonly int _concurrency;
        private readonly bool _immediateResume;
        private CancellationTokenSource _cancellationToken;
        private Task[] _tasks;
        private IServiceScope[] _scopes;
        public Scheduler(IOptions<SchedulerSettings> options, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _initSleepTime = options.Value.InitialSleepTime;
            _maxSleepTime = options.Value.MaxSleepTime;
            _concurrency = options.Value.Concurrency;
            _immediateResume = options.Value.ImmediateResume;
        }

        private void PreStart()
        {
            if(_cancellationToken !=null) throw new InvalidOperationException($"{nameof(Scheduler)} is already started, Call {nameof(Stop)} first if you want to start again");
            _cancellationToken = new CancellationTokenSource();
        }

        /// <summary>
        /// Starts the scheduler with a function that returns true/false, on a single thread.
        /// </summary>
        /// <param name="func"></param>
        public void StartSingle(Func<Task<bool>> func)
        {
            PreStart();
            _tasks = new []{Task.Run(() => Run(func, _cancellationToken.Token))};
        }

        /// <summary>
        /// Starts the scheduler with a function that returns true/false on a number of threads specified by scheduler settings
        /// </summary>
        /// <param name="func"></param>
        public void Start(Func<Task<bool>> func)
        {
            PreStart();
            _tasks = new Task[_concurrency];
            _scopes = new IServiceScope[_concurrency];
            for (var i = 0; i < _concurrency; i++)
            {
                //Invoke provided task passing the relevant instance of the given type.
                _tasks[i] = Task.Run(() => Run(func, _cancellationToken.Token));
            }
        }

        /// <summary>
        /// Starts the scheduler against multiple instances of type T, with a simple function that returns true/false against each instance.
        /// instantiation is done by the scheduler via the service provider, and the number of instances is defined by the scheduler settings
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        public void Start<T>(Func<T, Task<bool>> func)
        {
            PreStart();
            _tasks = new Task[_concurrency];
            _scopes = new IServiceScope[_concurrency];
            for (var i = 0; i < _concurrency; i++)
            {
                //Resolve the type inside its own scope to ensure scoped services work as expected
                _scopes[i] = _serviceProvider.CreateScope();
                var obj = (T)_scopes[i].ServiceProvider.GetService(typeof(T));
                //Invoke provided task passing the relevant instance of the given type.
                _tasks[i] = Task.Run(() => Run(async () => await func.Invoke(obj), _cancellationToken.Token));
            }
        }

        /// <summary>
        /// Starts given function then block indefinitely, typically used in worker applications to keep execution until worker is closed.
        /// </summary>
        /// <param name="func"></param>
        public void StartAndBlock(Func<Task<bool>> func)
        {
            Start(func);
            Task.WaitAll(_tasks);
        }

        /// <summary>
        /// Starts given function against multiple instances of type T then block indefinitely, typically used in worker applications to keep execution until worker is closed.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        public void StartAndBlock<T>(Func<T, Task<bool>> func)
        {
            Start(func);
            Task.WaitAll(_tasks);
        }

        /// <summary>
        /// Gracefully stops processing, waits for inprogress executions to complete then stops all threads
        /// </summary>
        public void Stop()
        {
            try
            {
                if (_cancellationToken == null) return;
                _cancellationToken.Cancel();
                Task.WaitAll(_tasks);
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is TaskCanceledException)
                {
                    //Swallow TaskCancelledException as it's expected here
                    return;
                }
                throw;
            }
            finally
            {
                _cancellationToken = null;
            }
        }

        private async Task Run(Func<Task<bool>> func, CancellationToken token)
        {
            var wait = _initSleepTime;
            while (true)
            {
                token.ThrowIfCancellationRequested();
                var result = await func.Invoke();
                if (result) wait = _immediateResume ? 0 : _initSleepTime;
                else
                {

                    if (wait != _maxSleepTime) wait = Math.Min(wait * 2, _maxSleepTime);
                }

                if (wait > 0) await Task.Delay(wait, token);
            }

            // ReSharper disable once FunctionNeverReturns - This is an infinite loop by design
        }

        private bool _disposing;
        public void Dispose()
        {
            if (_disposing) return;
            _disposing = true;

            //Ensure that stop is called to gracefully stop threads upon disposal
            Stop();
            for (var i = _tasks.Length - 1; i >= 0; i--)
            {
                _tasks[i].Dispose();
                _scopes?[i].Dispose();
            }
        }
    }
}
