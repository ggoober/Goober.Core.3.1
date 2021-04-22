using System.Threading;
using System.Threading.Tasks;

namespace Goober.Core.Extensions
{
    public static class TaskExtensions
    {
        public static TResult RunSync<TResult>(this Task<TResult> task)
        {
            var taskFactory = new
                TaskFactory(CancellationToken.None,
                        TaskCreationOptions.None,
                        TaskContinuationOptions.None,
                        TaskScheduler.Default);

            return taskFactory.StartNew(() => task)
                .Unwrap()
                .GetAwaiter()
                .GetResult();
        }

        public static void RunSync(this Task task)
        {
            var taskFactory = new
                TaskFactory(CancellationToken.None,
                        TaskCreationOptions.None,
                        TaskContinuationOptions.None,
                        TaskScheduler.Default);

            taskFactory
                .StartNew(() => task)
                .Unwrap()
                .GetAwaiter()
                .GetResult();
        }
    }

}
