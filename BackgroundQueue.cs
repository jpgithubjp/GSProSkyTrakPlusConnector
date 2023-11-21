using System;
using System.Threading;
using System.Threading.Tasks;

namespace SkytrakOpenAPI
{
    public class BackgroundQueue
    {
        private Task previousTask = Task.FromResult<bool>(true);
        private object key = new();

        public Task QueueTask(Action action)
        {
            object obj = this.key;
            Task result;
            lock (obj)
            {
                // TODO
//                this.previousTask = this.previousTask.ContinueWith(delegate (Task<bool>)
//                {
//                    action();
//                }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
                result = this.previousTask;
            }
            return result;
        }

        public Task<T> QueueTask<T>(Func<T> work)
        {
            object obj = this.key;
            Task<T> result;
            lock (obj)
            {
                result = (Task<T>)(this.previousTask = this.previousTask.ContinueWith<T>(
                    (Task t) => work(), CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default));
            }
            return result;
        }
    }
}
