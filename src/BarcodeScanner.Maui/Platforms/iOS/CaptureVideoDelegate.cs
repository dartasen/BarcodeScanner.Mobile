using AVFoundation;
using CoreGraphics;
using CoreMedia;
using CoreVideo;
using MLKit.BarcodeScanning;
using Foundation;
using AudioToolbox;
using UIKit;
using MLKit.Core;

namespace BarcodeScanner.Mobile.Platforms.iOS;

public class CaptureVideoDelegate : AVCaptureVideoDataOutputSampleBufferDelegate
{
    private readonly ICameraView cameraView;

    public event Action<OnDetectedEventArg> OnDetected;
    private readonly MLKit.BarcodeScanning.BarcodeScanner barcodeDetector;
    private readonly UIImageOrientation orientation = UIImageOrientation.Up;
    private long lastAnalysisTime = DateTimeOffset.MinValue.ToUnixTimeMilliseconds();
    private long lastRunTime = DateTimeOffset.MinValue.ToUnixTimeMilliseconds();

    public CaptureVideoDelegate(ICameraView cameraView)
    {
        this.cameraView = cameraView;
        if (this.cameraView != null)
        {
            if (this.cameraView.ScanInterval < 100)
                this.cameraView.ScanInterval = 500;
        }

        BarcodeScannerOptions options = new(Configuration.BarcodeDetectorSupportFormat);
        barcodeDetector = MLKit.BarcodeScanning.BarcodeScanner.BarcodeScannerWithOptions(options);
        orientation = GetUIImageOrientation();
    }

    private UIImageOrientation GetUIImageOrientation()
    {
        var orientation = UIImageOrientation.Up;
        // Using back-facing camera
        var devicePosition = AVCaptureDevicePosition.Back;
        var deviceOrientation = UIDevice.CurrentDevice.Orientation;
        switch (deviceOrientation)
        {
            case UIDeviceOrientation.Portrait:
                orientation = devicePosition == AVCaptureDevicePosition.Front ? UIImageOrientation.LeftMirrored : UIImageOrientation.Right;
                break;
            case UIDeviceOrientation.LandscapeLeft:
                orientation = devicePosition == AVCaptureDevicePosition.Front ? UIImageOrientation.DownMirrored : UIImageOrientation.Up;
                break;
            case UIDeviceOrientation.PortraitUpsideDown:
                orientation = devicePosition == AVCaptureDevicePosition.Front ? UIImageOrientation.RightMirrored : UIImageOrientation.Left;
                break;
            case UIDeviceOrientation.LandscapeRight:
                orientation = devicePosition == AVCaptureDevicePosition.Front ? UIImageOrientation.UpMirrored : UIImageOrientation.Down;
                break;
            case UIDeviceOrientation.FaceUp:
            case UIDeviceOrientation.FaceDown:
            case UIDeviceOrientation.Unknown:
                orientation = UIImageOrientation.Right;
                break;
        }

        return orientation;
    }

    private static UIImage GetImageFromSampleBuffer(CMSampleBuffer sampleBuffer, UIImageOrientation? orientation)
    {
        // Get a pixel buffer from the sample buffer
        using CVPixelBuffer pixelBuffer = sampleBuffer.GetImageBuffer() as CVPixelBuffer;

        // Lock the base address
        if (pixelBuffer != null)
        {
            pixelBuffer.Lock(CVPixelBufferLock.None);

            // Prepare to decode buffer
            CGBitmapFlags flags = CGBitmapFlags.PremultipliedFirst | CGBitmapFlags.ByteOrder32Little;

            // Decode buffer - Create a new colorspace
            using (var cs = CGColorSpace.CreateDeviceRGB())
            {
                // Create new context from buffer
                using CGBitmapContext context = new(pixelBuffer.BaseAddress,
                    pixelBuffer.Width,
                    pixelBuffer.Height,
                    8,
                    pixelBuffer.BytesPerRow,
                    cs,
                    (CGImageAlphaInfo)flags);
                // Get the image from the context
                using CGImage cgImage = context.ToImage();
                // Unlock and return image
                pixelBuffer.Unlock(CVPixelBufferLock.None);

                if (orientation == null)
                {
                    return UIImage.FromImage(cgImage);
                }

                return UIImage.FromImage(cgImage, 1, orientation.Value);
            }
        }
        else
        {
            return null;
        }
    }

    private void ReleaseSampleBuffer(CMSampleBuffer sampleBuffer)
    {
        sampleBuffer?.Dispose();
    }

    public override void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
    {
        lastRunTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        if (lastRunTime - lastAnalysisTime > cameraView.ScanInterval && cameraView.IsScanning)
        {
            lastAnalysisTime = lastRunTime;
            try
            {
                var shouldReturnBarcodeImage = cameraView.ReturnBarcodeImage;
                var image = GetImageFromSampleBuffer(sampleBuffer, shouldReturnBarcodeImage ? GetUIImageOrientation() : null);
                if (image == null) return;

                var visionImage = new MLImage(image) { Orientation = orientation };
                ReleaseSampleBuffer(sampleBuffer);
                barcodeDetector.ProcessImage(visionImage, (barcodes, error) =>
                {
                    if (cameraView == null) return;
                    if (!cameraView.IsScanning) return;

                    if (error != null)
                    {
                        System.Diagnostics.Debug.WriteLine(error);
                        return;
                    }

                    if (barcodes == null || barcodes.Length == 0)
                    {
                        return;
                    }

                    cameraView.IsScanning = false;

                    if (cameraView.VibrationOnDetected)
                    {
                        SystemSound.Vibrate.PlayAlertSound();
                    }

                    List<BarcodeResult> resultList = new();
                    foreach (Barcode barcode in barcodes)
                    {
                        resultList.Add(Methods.ProcessBarcodeResult(barcode));
                    }

                    byte[] imageDataByteArray = Array.Empty<byte>();
                    if (shouldReturnBarcodeImage)
                    {
                        using (NSData imageData = image.AsJPEG())
                        {
                            imageDataByteArray = new byte[imageData.Length];
                            System.Runtime.InteropServices.Marshal.Copy(imageData.Bytes, imageDataByteArray, 0, Convert.ToInt32(imageData.Length));
                        }
                    }

                    OnDetected?.Invoke(new OnDetectedEventArg { BarcodeResults = resultList, ImageData = imageDataByteArray });
                });
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message);
            }
        }
        ReleaseSampleBuffer(sampleBuffer);
    }
}
