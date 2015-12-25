using System.Threading.Tasks;

namespace Updater.Utilities
{
    public static class TaskExtensions
    {
        public static TResult Await<TResult>(this Task<TResult> task)
        {
            task.Wait();
            return task.Result;
        }
    }
}
