using AVFoundation;
using BarcodeScanner.Mobile.Platforms.iOS;
using CoreFoundation;
using CoreVideo;
using Foundation;
using System.Runtime.InteropServices;
using UIKit;

namespace BarcodeScanner.Mobile;

public partial class CameraViewHandler
{
    private AVCaptureVideoPreviewLayer videoPreviewLayer;
    private AVCaptureVideoDataOutput videoDataOutput;
    private AVCaptureSession captureSession;
    private AVCaptureDevice captureDevice;
    private AVCaptureInput captureInput;
    private DispatchQueue queue;
    private UICameraPreview previewView;

    protected override UIView CreatePlatformView()
    {
        captureSession = new AVCaptureSession
        {
            SessionPreset = GetCaptureSessionResolution()
        };
        queue = new DispatchQueue("BarcodeScannerQueue", new DispatchQueue.Attributes()
        {
            QualityOfService = DispatchQualityOfService.UserInitiated
        });
        videoPreviewLayer = new AVCaptureVideoPreviewLayer(captureSession)
        {
            VideoGravity = AVLayerVideoGravity.ResizeAspectFill
        };
        previewView = new UICameraPreview(videoPreviewLayer);
        previewView.AddGestureRecognizer(new UITapGestureRecognizer(FocusOnTap));
        //_previewView.AddGestureRecognizer(new UIPinchGestureRecognizer(ZoomOnPinch));
        return previewView;
    }

    private void Start()
    {
        if (captureSession is not null)
        {
            if (captureSession.Running)
            {
                captureSession.StopRunning();
            }

            UpdateCamera();
            UpdateAnalyzer();
            UpdateResolution();
            UpdateTorch();
            UpdateZoom();

            captureSession.StartRunning();
        }
    }

    private void Stop()
    {
        if (captureSession is not null)
        {
            DisableTorchIfNeeded();

            if (captureSession.Running)
            {
                captureSession.StopRunning();
            }
        }
    }

    private void HandleCameraEnabled()
    {
        if (VirtualView is not null)
        {
            if (VirtualView.CameraEnabled)
            {
                Start();
            }
            else
            {
                Stop();
            }
        }
    }

    private void UpdateResolution()
    {
        if (captureSession is not null)
        {
            captureSession.BeginConfiguration();
            captureSession.SessionPreset = GetCaptureSessionResolution();
            captureSession.CommitConfiguration();
        }
    }
    private void UpdateAnalyzer()
    {
        if (captureSession is not null)
        {
            captureSession.BeginConfiguration();

            if (videoDataOutput is not null && captureSession.Outputs.Length > 0 && captureSession.Outputs.Contains(videoDataOutput))
            {
                captureSession.RemoveOutput(videoDataOutput);
                videoDataOutput.Dispose();
                videoDataOutput = null;
            }

            videoDataOutput = new AVCaptureVideoDataOutput()
            {
                AlwaysDiscardsLateVideoFrames = true,
                WeakVideoSettings = new CVPixelBufferAttributes { PixelFormatType = CVPixelFormatType.CV32BGRA }.Dictionary
            };

            videoDataOutput.SetSampleBufferDelegate(new BarcodeAnalyzer(VirtualView), queue);

            if (captureSession.CanAddOutput(videoDataOutput))
                captureSession.AddOutput(videoDataOutput);

            captureSession.CommitConfiguration();
        }
    }
    private void UpdateCamera()
    {
        if (captureSession is not null)
        {
            captureSession.BeginConfiguration();

            if (captureInput is not null && captureSession.Inputs.Length > 0 && captureSession.Inputs.Contains(captureInput))
            {
                captureSession.RemoveInput(captureInput);
                captureInput.Dispose();
                captureInput = null;
            }

            if (captureDevice is not null)
            {
                captureDevice.Dispose();
                captureDevice = null;
            }

            captureDevice = AVCaptureDevice.GetDefaultDevice(
                AVCaptureDeviceType.BuiltInWideAngleCamera,
                AVMediaTypes.Video,
                VirtualView.CameraFacing == CameraFacing.FRONT ? AVCaptureDevicePosition.Front : AVCaptureDevicePosition.Back
             );

            if (captureDevice.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
            {
                CaptureDeviceLock(() => captureDevice.FocusMode = AVCaptureFocusMode.ContinuousAutoFocus);
            }

            captureInput = new AVCaptureDeviceInput(captureDevice, out _);

            if (captureSession.CanAddInput(captureInput))
            {
                captureSession.AddInput(captureInput);
            }

            captureSession.CommitConfiguration();
        }
    }

    private void UpdateZoom()
    {
        if (captureDevice is not null)
        {
            CaptureDeviceLock(() =>
            {
                double min = captureDevice.MinAvailableVideoZoomFactor;
                double max = captureDevice.MaxAvailableVideoZoomFactor;
                double currentZoom = VirtualView.Zoom;

                captureDevice.VideoZoomFactor = new NFloat(Math.Clamp(currentZoom, min, max));
            });
        }
    }

    private void UpdateTorch()
    {
        if (captureDevice is not null && captureDevice.HasTorch && captureDevice.TorchAvailable)
        {
            CaptureDeviceLock(() => captureDevice.TorchMode = VirtualView.TorchOn ? AVCaptureTorchMode.On : AVCaptureTorchMode.Off);
        }
    }

    private void FocusOnTap(UITapGestureRecognizer tapRecognizer)
    {
        if (captureDevice is not null && VirtualView.TapToFocusEnabled && captureDevice.FocusPointOfInterestSupported)
        {
            CaptureDeviceLock(() => captureDevice.FocusPointOfInterest = videoPreviewLayer.CaptureDevicePointOfInterestForPoint(tapRecognizer.LocationInView(previewView)));

            if (captureDevice.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
            {
                CaptureDeviceLock(() => captureDevice.FocusMode = AVCaptureFocusMode.ContinuousAutoFocus);
            }
        }
    }

    private void DisableTorchIfNeeded()
    {
        if (captureDevice is not null && captureDevice.TorchMode == AVCaptureTorchMode.On)
        {
            CaptureDeviceLock(() => captureDevice.TorchMode = AVCaptureTorchMode.Off);
        }
    }

    private NSString GetCaptureSessionResolution()
    {
        var captureQuality = VirtualView.CaptureQuality;
        return captureQuality switch
        {
            CaptureQuality.LOWEST => AVCaptureSession.Preset352x288,
            CaptureQuality.LOW => AVCaptureSession.Preset640x480,
            CaptureQuality.MEDIUM => AVCaptureSession.Preset1280x720,
            CaptureQuality.HIGH => AVCaptureSession.Preset1920x1080,
            CaptureQuality.HIGHEST => AVCaptureSession.Preset3840x2160,
            _ => throw new ArgumentOutOfRangeException(nameof(VirtualView.CaptureQuality))
        };
    }
    private void CaptureDeviceLock(Action handler)
    {
        if (captureDevice.LockForConfiguration(out _))
        {
            try
            {
                handler();
            }
            catch (Exception)
            {

            }
            finally
            {
                captureDevice.UnlockForConfiguration();
            }
        }
    }
}