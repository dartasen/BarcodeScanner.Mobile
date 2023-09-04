using Android.Content;
using Android.Hardware.Camera2;
using Android.Util;
using AndroidX.Camera.Camera2.InterOp;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Core.Content;
using AndroidX.Lifecycle;
using BarcodeScanner.Mobile.Platforms.Android;
using Google.Common.Util.Concurrent;
using Java.Lang;
using Java.Util.Concurrent;
using Exception = System.Exception;

namespace BarcodeScanner.Mobile;

public partial class CameraViewHandler
{
    private bool isDisposed;

    private IListenableFuture cameraFuture;
    private IExecutorService cameraExecutor;

    private ICamera camera;

    private PreviewView previewView;

    protected override PreviewView CreatePlatformView()
    {
        previewView = new PreviewView(Context);
        return previewView;
    }


    private void Connect()
    {
        cameraExecutor = Executors.NewSingleThreadExecutor();
        cameraFuture = ProcessCameraProvider.GetInstance(Context);
        cameraFuture.AddListener(new Runnable(CameraCallback), ContextCompat.GetMainExecutor(Context));
    }

    private void CameraCallback()
    {
        if (isDisposed) return;
        // Used to bind the lifecycle of cameras to the lifecycle owner
        if (cameraFuture?.Get() is not ProcessCameraProvider cameraProvider) return;

        // Preview
        Preview.Builder previewBuilder = new();
        Preview preview = previewBuilder.Build();
        preview.SetSurfaceProvider(previewView.SurfaceProvider);

        ImageAnalysis.Builder imageAnalyzerBuilder = new();
        // Frame by frame analyze
        if (VirtualView.RequestedFPS.HasValue)
        {
            Camera2Interop.Extender ext = new(imageAnalyzerBuilder);
            ext.SetCaptureRequestOption(CaptureRequest.ControlAeMode, 0);
            ext.SetCaptureRequestOption(CaptureRequest.ControlAeTargetFpsRange, new Android.Util.Range((int)VirtualView.RequestedFPS.Value, (int)VirtualView.RequestedFPS.Value));
        }

        //https://developers.google.com/ml-kit/vision/barcode-scanning/android#input-image-guidelines
        ImageAnalysis imageAnalyzer = imageAnalyzerBuilder
                            .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest) //<!-- only one image will be delivered for analysis at a time
                            .SetTargetResolution(TargetResolution())
                            .Build();

        imageAnalyzer.SetAnalyzer(cameraExecutor, new BarcodeAnalyzer(VirtualView));

        CameraSelector cameraSelector = SelectCamera(cameraProvider);

        try
        {
            // Unbind use cases before rebinding
            cameraProvider.UnbindAll();

            // Searching for lifecycle owner
            // There can be context wrapper instead of context it self, so we have to check it.
            ILifecycleOwner lifecycleOwner = Context as ILifecycleOwner ?? (Context as ContextWrapper)?.BaseContext as ILifecycleOwner;
            if (lifecycleOwner == null)
            {
                throw new Exception("Unable to find lifecycle owner");
            }

            // Bind use cases to camera
            camera = cameraProvider.BindToLifecycle(lifecycleOwner, cameraSelector, preview, imageAnalyzer);

            HandleCustomPreviewSize(preview);
            HandleTorch();
            HandleZoom();
            HandleAutoFocus();
        }
        catch (Exception exc)
        {
            Log.Debug(nameof(CameraCallback), "Use case binding failed", exc);
        }
    }
 
    private CameraSelector SelectCamera(ProcessCameraProvider cameraProvider)
    {
        if (VirtualView.CameraFacing == CameraFacing.FRONT)
        {
            if (cameraProvider.HasCamera(CameraSelector.DefaultFrontCamera))
            {
                return CameraSelector.DefaultFrontCamera;
            }

            throw new NotSupportedException("Front camera is not supported in this device");
        }

        if (cameraProvider.HasCamera(CameraSelector.DefaultBackCamera))
        {
            return CameraSelector.DefaultBackCamera;
        }

        throw new NotSupportedException("Back camera is not supported in this device");
    }

    private Android.Util.Size TargetResolution()
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

    /// <summary>
    /// Logic from https://stackoverflow.com/a/66659592/9032777
    /// Focus every 3s
    /// </summary>
    public async void HandleAutoFocus()
    {
        while (true)
        {
            try
            {
                await Task.Delay(3000);

                if (camera == null || previewView == null)
                {
                    continue;
                }

                float x = previewView.GetX() + previewView.Width / 2f;
                float y = previewView.GetY() + previewView.Height / 2f;

                MeteringPointFactory pointFactory = previewView.MeteringPointFactory;
                float afPointWidth = 1.0f / 6.0f;  // 1/6 total area
                float aePointWidth = afPointWidth * 1.5f;
                MeteringPoint afPoint = pointFactory.CreatePoint(x, y, afPointWidth);
                MeteringPoint aePoint = pointFactory.CreatePoint(x, y, aePointWidth);

                camera.CameraControl.StartFocusAndMetering(
                    new FocusMeteringAction.Builder(
                        afPoint,
                        FocusMeteringAction.FlagAf
                    )
                    .AddPoint(aePoint, FocusMeteringAction.FlagAe).Build());
            }
            catch (Exception)
            {
                // Ignore
            }
        }
    }

    public void HandleTorch()
    {
        if (camera == null || VirtualView == null || !camera.CameraInfo.HasFlashUnit) return;
       
        camera.CameraControl.EnableTorch(VirtualView.TorchOn);
    }

    public void HandleZoom()
    {
        if (camera == null || VirtualView == null) return;

        camera.CameraControl.SetLinearZoom(VirtualView.Zoom);
    }

    private bool IsTorchOn()
    {
        if (camera == null || !camera.CameraInfo.HasFlashUnit) return false;

        return (int)camera.CameraInfo.TorchState?.Value == TorchState.On;
    }

    private void DisableTorchIfNeeded()
    {
        if (camera == null || !camera.CameraInfo.HasFlashUnit || (int)camera.CameraInfo.TorchState?.Value != TorchState.On)
        {
            return;
        }

        camera.CameraControl.EnableTorch(false);
    }

    private void HandleCustomPreviewSize(Preview preview)
    {
        if (VirtualView.PreviewWidth.HasValue && VirtualView.PreviewHeight.HasValue)
        {
            var width = VirtualView.PreviewWidth.Value;
            var height = VirtualView.PreviewHeight.Value;
            preview.UpdateSuggestedResolution(new Android.Util.Size(width, height));
        }
    }

    private void Dispose()
    {
        if (isDisposed) return;

        DisableTorchIfNeeded();

        cameraExecutor?.Shutdown();
        cameraExecutor?.Dispose();
        cameraExecutor = null;

        ClearCameraProvider();

        cameraFuture?.Cancel(true);
        cameraFuture?.Dispose();
        cameraFuture = null;

        isDisposed = true;
    }
 
    private void ClearCameraProvider()
    {
        try
        {
            // Used to bind the lifecycle of cameras to the lifecycle owner
            if (cameraFuture?.Get() is not ProcessCameraProvider cameraProvider)
            {
                return;
            }

            cameraProvider?.UnbindAll();
            cameraProvider?.Dispose();
        }
        catch (Exception ex)
        {
            Log.Debug($"{nameof(CameraViewHandler)}-{nameof(ClearCameraProvider)}", ex.ToString());
        }
    }
}
