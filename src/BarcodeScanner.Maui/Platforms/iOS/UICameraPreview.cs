using AVFoundation;
using UIKit;

namespace BarcodeScanner.Mobile.Platforms.iOS;

internal class UICameraPreview : UIView
{
    private readonly AVCaptureVideoPreviewLayer previewLayer;

    public UICameraPreview(AVCaptureVideoPreviewLayer layer) : base()
    {
        previewLayer = layer;
        previewLayer.Frame = Layer.Bounds;
        Layer.AddSublayer(previewLayer);
    }

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();
        previewLayer.Frame = Layer.Bounds;

        AVCaptureConnection connection = previewLayer.Connection;
        if (connection == null) return;

        AVCaptureVideoOrientation videoOrientation = UIDevice.CurrentDevice.Orientation switch
        {
            UIDeviceOrientation.LandscapeLeft => AVCaptureVideoOrientation.LandscapeRight,
            UIDeviceOrientation.LandscapeRight => AVCaptureVideoOrientation.LandscapeLeft,
            UIDeviceOrientation.PortraitUpsideDown => AVCaptureVideoOrientation.PortraitUpsideDown,
            _ => AVCaptureVideoOrientation.Portrait
        };

        if (connection.SupportsVideoOrientation)
        {
            connection.VideoOrientation = videoOrientation;
        }
    }
}