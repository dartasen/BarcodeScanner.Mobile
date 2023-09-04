using AndroidX.Camera.Core;
using AndroidX.Lifecycle;

namespace BarcodeScanner.Mobile.Platforms.Android;

internal class TorchStateObserver : Java.Lang.Object, IObserver
{
    private readonly ICameraView cameraView;

    public TorchStateObserver(ICameraView cameraView)
    {
        this.cameraView = cameraView;
    }

    public void OnChanged(Java.Lang.Object state)
    {
        cameraView.TorchOn = (int)state == TorchState.On;
    }
}
