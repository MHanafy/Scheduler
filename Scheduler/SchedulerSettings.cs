namespace MHanafy.Scheduling
{
    public class SchedulerSettings
    {
        /// <summary>
        /// The number of milliseconds to sleep for, when then function returns false; this number is doubled for every subsequent false return.
        /// </summary>
        public int InitialSleepTime { get; set; }
        /// <summary>
        /// The maximum number of milliseconds to sleep for
        /// </summary>
        public int MaxSleepTime { get; set; }

        /// <summary>
        /// Defines the number of concurrent threads when using Start Of T, defaults to 10
        /// </summary>
        public int Concurrency { get; set; } = 10;

        /// <summary>
        /// Controls whether the scheduler will immediately call back the function when it returns true, or will wait for InitialSleepTime
        /// </summary>
        public bool ImmediateResume { get; set; } = false;
    }
}