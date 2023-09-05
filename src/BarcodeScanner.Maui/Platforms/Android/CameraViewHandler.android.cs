using Android.Content;
using AndroidX.Camera.Core;
using AndroidX.Camera.View;
using AndroidX.Lifecycle;
using BarcodeScanner.Mobile.Platforms.Android;
using Java.Util.Concurrent;
using Exception = System.Exception;

namespace BarcodeScanner.Mobile;

public partial class CameraViewHandler
{
    private IExecutorService cameraExecutor;
    private PreviewView previewView;
    private LifecycleCameraController cameraController;

    protected override PreviewView CreatePlatformView()
    {
        cameraExecutor = Executors.NewSingleThreadExecutor();
        cameraController = new LifecycleCameraController(Context)
        {
            TapToFocusEnabled = VirtualView.TapToFocusEnabled,
            PinchToZoomEnabled = VirtualView.PinchToZoomEnabled
        };
        previewView = new PreviewView(Context)
        {
            Controller = cameraController
        };
        return previewView;
    }

    private void Start()
    {
        if (cameraController is not null)
        {
            cameraController.Unbind();

            ILifecycleOwner lifecycleOwner = null;
            if (Context is ILifecycleOwner)
                lifecycleOwner = Context as ILifecycleOwner;
            else if ((Context as ContextWrapper)?.BaseContext is ILifecycleOwner)
                lifecycleOwner = (Context as ContextWrapper)?.BaseContext as ILifecycleOwner;
            else if (Platform.CurrentActivity is ILifecycleOwner)
                lifecycleOwner = Platform.CurrentActivity as ILifecycleOwner;

            if (lifecycleOwner == null)
                throw new Exception("Unable to find lifecycle owner");

            UpdateResolution();
            UpdateCamera();
            UpdateAnalyzer();
            UpdateTorch();
            UpdateZoom();

            cameraController.BindToLifecycle(lifecycleOwner);
        }
    }

    private void Stop()
    {
        if (cameraController is not null)
        {
            cameraController.EnableTorch(false);
            cameraController.SetLinearZoom(1f);
            cameraController.Unbind();
        }
    }

    private void HandleCameraEnabled()
    {
        //Delay to let transition animation finish
        //https://stackoverflow.com/a/67765792
        if (VirtualView is not null)
        {
            if (VirtualView.CameraEnabled)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(200);
                    MainThread.BeginInvokeOnMainThread(Start);
                });
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(Stop);
            }
        }
    }

    //https://developer.android.com/reference/androidx/camera/mlkit/vision/MlKitAnalyzer
    private void UpdateAnalyzer()
    {
        if (cameraExecutor is not null && cameraController is not null)
        {
            cameraController.ClearImageAnalysisAnalyzer();
            cameraController.SetImageAnalysisAnalyzer(cameraExecutor, new BarcodeAnalyzer(VirtualView));
            cameraController.ImageAnalysisBackpressureStrategy = ImageAnalysis.StrategyKeepOnlyLatest;
        }
    }

    private void UpdateCamera()
    {
        if (cameraController is not null)
        {
            if (VirtualView.CameraFacing == CameraFacing.FRONT)
            {
                cameraController.CameraSelector = CameraSelector.DefaultFrontCamera;
            }
            else
            {
                cameraController.CameraSelector = CameraSelector.DefaultBackCamera;
            }
        }
    }

    private void UpdateZoom()
    {
        cameraController?.SetLinearZoom(VirtualView.Zoom);
    }

    private void UpdateResolution()
    {
        if (cameraController is not null)
        {
            cameraController.ImageAnalysisTargetSize = new CameraController.OutputSize(TargetResolution());
        }
    }

    private void UpdateTorch()
    {
        cameraController?.EnableTorch(VirtualView.TorchOn);
    }

    private Android.Util.Size TargetResolution()
    {
        if (DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait)
        {
            return VirtualView.CaptureQuality switch
            {
                CaptureQuality.LOWEST => new Android.Util.Size(288, 352),
                CaptureQuality.LOW => new Android.Util.Size(480, 640),
                CaptureQuality.MEDIUM => new Android.Util.Size(720, 1280),
                CaptureQuality.HIGH => new Android.Util.Size(1080, 1920),
                CaptureQuality.HIGHEST => new Android.Util.Size(2160, 3840),
                _ => throw new ArgumentOutOfRangeException(nameof(CaptureQuality))
            };
        }
        else
        {
            return VirtualView.CaptureQuality switch
            {
                CaptureQuality.LOWEST => new Android.Util.Size(352, 288),
                CaptureQuality.LOW => new Android.Util.Size(640, 480),
                CaptureQuality.MEDIUM => new Android.Util.Size(1280, 720),
                CaptureQuality.HIGH => new Android.Util.Size(1920, 1080),
                CaptureQuality.HIGHEST => new Android.Util.Size(3840, 2160),
                _ => throw new ArgumentOutOfRangeException(nameof(CaptureQuality))
            };
        }
    }
}