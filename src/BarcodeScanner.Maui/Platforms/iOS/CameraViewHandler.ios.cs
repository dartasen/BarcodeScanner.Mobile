using AVFoundation;
using BarcodeScanner.Mobile.Platforms.iOS;
using CoreVideo;
using Foundation;
using System.Runtime.InteropServices;

namespace BarcodeScanner.Mobile;

public partial class CameraViewHandler
{
    public event EventHandler IsScanningChanged;

    private AVCaptureVideoPreviewLayer VideoPreviewLayer;
    private AVCaptureDevice CaptureDevice;
    private AVCaptureInput CaptureInput = null;
    private CaptureVideoDelegate CaptureVideoDelegate;

    public AVCaptureSession CaptureSession { get; private set; }
    public AVCaptureVideoDataOutput VideoDataOutput { get; set; }

    bool IsUpdatingTorch { get; set; }

    bool IsUpdatingZoom { get; set; }

    UICameraPreview _uiCameraPerview;
    protected override UICameraPreview CreatePlatformView()
    {
        CaptureSession = new AVCaptureSession
        {
            SessionPreset = AVCaptureSession.Preset640x480
        };

        VideoPreviewLayer = new AVCaptureVideoPreviewLayer(CaptureSession)
        {
            VideoGravity = AVLayerVideoGravity.ResizeAspectFill
        };

        _uiCameraPerview = new UICameraPreview(VideoPreviewLayer);
        return _uiCameraPerview;
    }

    public void Connect()
    {
        if (DeviceInfo.Current.DeviceType == DeviceType.Virtual)
            return;

        ChangeCameraFacing();
        ChangeCameraQuality();

        if (VideoDataOutput == null)
        {
            VideoDataOutput = new AVCaptureVideoDataOutput
            {
                AlwaysDiscardsLateVideoFrames = true,
                WeakVideoSettings = new CVPixelBufferAttributes { PixelFormatType = CVPixelFormatType.CV32BGRA }
             .Dictionary
            };

            if (CaptureVideoDelegate == null)
            {
                CaptureVideoDelegate = new CaptureVideoDelegate(VirtualView);
                CaptureVideoDelegate.OnDetected += (eventArg) =>
                {
                    PlatformView.InvokeOnMainThread(() =>
                    {
                        //CaptureSession.StopRunning();
                        this.VirtualView?.TriggerOnDetected(eventArg.BarcodeResults, eventArg.ImageData);
                    });
                };
            }
            VideoDataOutput.AlwaysDiscardsLateVideoFrames = true;
            VideoDataOutput.SetSampleBufferDelegate(CaptureVideoDelegate, CoreFoundation.DispatchQueue.MainQueue);
        }

        CaptureSession.AddOutput(VideoDataOutput);

        CaptureSession.StartRunning();
        HandleTorch();
        SetFocusMode();
    }

    public void Dispose()
    {
        //Stop the capture session if not null
        try
        {
            if (CaptureDevice != null)
            {
                CaptureDevice.Dispose();
                CaptureDevice = null;
            }

            if (CaptureInput != null)
            {
                CaptureInput.Dispose();
                CaptureInput = null;
            }

            if (CaptureSession != null)
            {
                CaptureSession.RemoveOutput(VideoDataOutput);
                CaptureSession.StopRunning();
            }

            if (VideoDataOutput != null)
            {
                VideoDataOutput.Dispose();
                VideoDataOutput = null;
            }
        }
        catch
        {
            // Ignore
        }
    }

    public NSString GetCaptureSessionResolution(CaptureQuality captureQuality)
    {
        return captureQuality switch
        {
            CaptureQuality.LOWEST => AVCaptureSession.Preset352x288,
            CaptureQuality.LOW => AVCaptureSession.Preset640x480,
            CaptureQuality.MEDIUM => AVCaptureSession.Preset1280x720,
            CaptureQuality.HIGH => AVCaptureSession.Preset1920x1080,
            CaptureQuality.HIGHEST => AVCaptureSession.Preset3840x2160,
            _ => throw new ArgumentOutOfRangeException(nameof(captureQuality))
        };
    }

    public void SetFocusMode(AVCaptureFocusMode focusMode = AVCaptureFocusMode.ContinuousAutoFocus)
    {
        Application.Current.Dispatcher.Dispatch(() =>
        {
            AVCaptureDevice videoDevice = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video);
            if (videoDevice == null) return;

            videoDevice.LockForConfiguration(out NSError error);
            if (error == null)
            {
                videoDevice.FocusMode = focusMode;
            }
            videoDevice.UnlockForConfiguration();
        });
    }

    public bool IsTorchOn()
    {
        try
        {
            if (CaptureDevice == null || !CaptureDevice.HasTorch || !CaptureDevice.TorchAvailable)
            {
                return false;
            }

            return CaptureDevice.TorchMode == AVCaptureTorchMode.On;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"iOS IsTorchOn error : {ex.Message}, StackTrace : {ex.StackTrace}");
        }

        return false;
    }

    public void HandleTorch()
    {
        Application.Current.Dispatcher.Dispatch(() =>
        {
            if (IsUpdatingTorch) return;
            if (CaptureDevice == null || !CaptureDevice.HasTorch || !CaptureDevice.TorchAvailable) return;

            IsUpdatingTorch = true;

            CaptureDevice.LockForConfiguration(out NSError error);
            if (error == null)
            {
                if (VirtualView.TorchOn == true)
                {
                    CaptureDevice.SetTorchModeLevel(1.0f, out error);
                }
                else
                {
                    CaptureDevice.TorchMode = AVCaptureTorchMode.Off;
                }
            }
            CaptureDevice.UnlockForConfiguration();

            IsUpdatingTorch = false;
        });
    }

    public void HandleZoom()
    {
        Application.Current.Dispatcher.Dispatch(() =>
        {
            if (IsUpdatingZoom) return;
            if (CaptureDevice == null) return;

            IsUpdatingZoom = true;

            CaptureDevice.LockForConfiguration(out NSError error);
            if (error == null)
            {
                double min = CaptureDevice.MinAvailableVideoZoomFactor;
                double max = CaptureDevice.MaxAvailableVideoZoomFactor;
                double currentZoom = VirtualView.Zoom;

                CaptureDevice.VideoZoomFactor = new NFloat(Math.Clamp(currentZoom, min, max));

            }
            CaptureDevice.UnlockForConfiguration();

            IsUpdatingZoom = false;
        });
    }

    public void ChangeCameraFacing()
    {
        if (CaptureSession != null)
        {
            CaptureSession.BeginConfiguration();

            // Clean old input
            if (CaptureInput != null && CaptureSession.Inputs.Contains(CaptureInput))
            {
                CaptureSession.RemoveInput(CaptureInput);
                CaptureInput.Dispose();
                CaptureInput = null;
            }

            // Clean old device
            if (CaptureDevice != null)
            {
                CaptureDevice.Dispose();
                CaptureDevice = null;
            }

            AVCaptureDeviceDiscoverySession deviceDiscovery = AVCaptureDeviceDiscoverySession.Create(
                new AVCaptureDeviceType[] { AVCaptureDeviceType.BuiltInWideAngleCamera, AVCaptureDeviceType.BuiltInDualCamera, AVCaptureDeviceType.BuiltInTripleCamera },
                AVMediaTypes.Video,
                AVCaptureDevicePosition.Unspecified
            );

            foreach (AVCaptureDevice device in deviceDiscovery.Devices)
            {
                if (VirtualView.CameraFacing == CameraFacing.FRONT &&
                    device.Position == AVCaptureDevicePosition.Front)
                {
                    CaptureDevice = device;
                    break;
                }
                else if (VirtualView.CameraFacing == CameraFacing.BACK && device.Position == AVCaptureDevicePosition.Back)
                {
                    CaptureDevice = device;
                    break;
                }
            }

            if (CaptureDevice == null)
            {
                throw new NotSupportedException("The selected camera is not supported on this device");
            }

            CaptureInput = new AVCaptureDeviceInput(CaptureDevice, out _);
            CaptureSession.AddInput(CaptureInput);
            CaptureSession.CommitConfiguration();
        }
    }
    public void ChangeCameraQuality()
    {
        AVCaptureInput input = CaptureSession.Inputs?.FirstOrDefault();

        if (input != null && CaptureSession != null)
        {
            CaptureSession.BeginConfiguration();
            CaptureSession.SessionPreset = GetCaptureSessionResolution(VirtualView.CaptureQuality);
            CaptureSession.CommitConfiguration();
        }
    }
}