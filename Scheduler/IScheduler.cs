using System;
using System.Threading.Tasks;

namespace MHanafy.Scheduling
{
    public interface IScheduler
    {
        /// <summary>
        /// Starts the scheduler with a function that returns true/false, on a single thread.
        /// </summary>
        /// <param name="func"></param>
        void StartSingle(Func<Task<bool>> func);
        
        /// <summary>
        /// Starts the scheduler with a function that returns true/false on a number of threads specified by scheduler settings
        /// </summary>
        /// <param name="func"></param>
        void Start(Func<Task<bool>> func);
        
        /// <summary>
        /// Starts the scheduler against multiple instances of type T, with a simple function that returns true/false against each instance.
        /// instantiation is done by the scheduler via the service provider, and the number of instances is defined by the scheduler settings
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        void Start<T>(Func<T, Task<bool>> func);
        
        /// <summary>
        /// Starts given function then block indefinitely, typically used in worker applications to keep execution until worker is closed.
        /// </summary>
        /// <param name="func"></param>
        void StartAndBlock(Func<Task<bool>> func);

        /// <summary>
        /// Starts given function against multiple instances of type T then block indefinitely, typically used in worker applications to keep execution until worker is closed.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        void StartAndBlock<T>(Func<T, Task<bool>> func);
        
        /// <summary>
        /// Gracefully stops processing, waits for inprogress executions to complete then stops all threads
        /// </summary>
        void Stop();
    }
}