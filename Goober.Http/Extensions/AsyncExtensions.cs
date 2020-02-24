using System.Threading;
using System.Threading.Tasks;

namespace Goober.Http.Extensions
{
    public static class AsyncExtensions
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
    }
}
