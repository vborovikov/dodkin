namespace Dodkin.Service.Delivery;

sealed class AsyncManualResetEvent
{
    private volatile TaskCompletionSource<bool> taskSource = new();

    public Task WaitAsync() => this.taskSource.Task;

    public void Set() => this.taskSource.TrySetResult(true);

    public void Reset()
    {
        while (true)
        {
            var tcs = this.taskSource;
            if (!tcs.Task.IsCompleted ||
                Interlocked.CompareExchange(ref this.taskSource, new TaskCompletionSource<bool>(), tcs) == tcs)
            {
                return;
            }
        }
    }
}