using Android.App;
using Android.Content;
using System;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Demo
{
    static class SarAwaiter
    {
        static int _next = 1000;
        static readonly System.Collections.Concurrent.ConcurrentDictionary<int,
            TaskCompletionSource<Intent>> _pending = new System.Collections.Concurrent.ConcurrentDictionary<int,
            TaskCompletionSource<Intent>>();

        public static Task<Intent> StartForResultAsync(this Activity a, Intent i)
        {
            var rc = System.Threading.Interlocked.Increment(ref _next);
            var tcs = new TaskCompletionSource<Intent>();
            _pending[rc] = tcs;
            a.StartActivityForResult(i, rc);
            return tcs.Task;
        }

        public static bool Complete(int rc, Result code, Intent? data)
        {
            if (_pending.TryRemove(rc, out var tcs))
            {
                if (code == Result.Ok && data != null) tcs.TrySetResult(data);
                else tcs.TrySetException(new OperationCanceledException());
                return true;
            }
            return false;
        }
    }
}
