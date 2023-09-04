using Android.Gms.Tasks;

namespace BarcodeScanner.Mobile.Platforms.Android;

internal class TaskCompleteListener : Java.Lang.Object, IOnCompleteListener
{
    private readonly TaskCompletionSource<Java.Lang.Object> taskCompletionSource;

    public TaskCompleteListener(TaskCompletionSource<Java.Lang.Object> tcs)
    {
        taskCompletionSource = tcs;
    }

    public void OnComplete(global::Android.Gms.Tasks.Task task)
    {
        if (task.IsCanceled)
        {
            taskCompletionSource.SetCanceled();
        }
        else if (task.IsSuccessful)
        {
            taskCompletionSource.SetResult(task.Result);
        }
        else
        {
            taskCompletionSource.SetException(task.Exception);
        }
    }
}
