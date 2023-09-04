using Android.App;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using AndroidX.Camera.Core;
using Java.Nio;
using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Common;

namespace BarcodeScanner.Mobile.Platforms.Android;

public class BarcodeAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer
{
    private readonly IBarcodeScanner barcodeScanner;
    private readonly ICameraView cameraView;
    private long lastRunTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    private long lastAnalysisTime = DateTimeOffset.MinValue.ToUnixTimeMilliseconds();

    public BarcodeAnalyzer(ICameraView cameraView)
    {
        this.cameraView = cameraView;
        if (this.cameraView != null && this.cameraView.ScanInterval < 100)
        {
            this.cameraView.ScanInterval = 500;
        }

        barcodeScanner = BarcodeScanning.GetClient(
            new BarcodeScannerOptions.Builder().SetBarcodeFormats(Configuration.BarcodeFormats).Build()
        );
    }

    public async void Analyze(IImageProxy proxy)
    {
        try
        {
            global::Android.Media.Image mediaImage = proxy.Image;
            if (mediaImage == null) return;

            lastRunTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            if (lastRunTime - lastAnalysisTime > cameraView.ScanInterval && cameraView.IsScanning)
            {
                lastAnalysisTime = lastRunTime;
                InputImage image = InputImage.FromMediaImage(mediaImage, proxy.ImageInfo.RotationDegrees);
                // Pass image to the scanner and have it do its thing
                Java.Lang.Object result = await ToAwaitableTask(barcodeScanner.Process(image));

                List<BarcodeResult> final = Methods.ProcessBarcodeResult(result);

                if (final == null || cameraView == null) return;
                if (!cameraView.IsScanning)
                {
                    return;
                }

                byte[] imageData = Array.Empty<byte>();
                if (cameraView.ReturnBarcodeImage)
                {
                    imageData = NV21toJPEG(YUV_420_888toNV21(mediaImage), mediaImage.Width, mediaImage.Height);
                    imageData = RotateJpeg(imageData, GetImageRotationCorrectionDegrees());
                }

                cameraView.IsScanning = false;
                cameraView.TriggerOnDetected(final, imageData);
                if (cameraView.VibrationOnDetected)
                {
                    Vibration.Vibrate(200);
                }
            }
        }
        catch (Java.Lang.Exception ex)
        {
            Log.Debug(nameof(BarcodeAnalyzer), ex.ToString());
        }
        catch (Exception ex)
        {
            Log.Debug(nameof(BarcodeAnalyzer), ex.ToString());
        }
        finally
        {
            SafeCloseImageProxy(proxy);
        }
    }

    /// <summary>
    /// https://stackoverflow.com/a/45926852
    /// </summary>
    private static byte[] YUV_420_888toNV21(global::Android.Media.Image image)
    {
        byte[] nv21;
        ByteBuffer yBuffer = image.GetPlanes()[0].Buffer;
        ByteBuffer uBuffer = image.GetPlanes()[1].Buffer;
        ByteBuffer vBuffer = image.GetPlanes()[2].Buffer;

        int ySize = yBuffer.Remaining();
        int uSize = uBuffer.Remaining();
        int vSize = vBuffer.Remaining();

        nv21 = new byte[ySize + uSize + vSize];

        //U and V are swapped
        yBuffer.Get(nv21, 0, ySize);
        vBuffer.Get(nv21, ySize, vSize);
        uBuffer.Get(nv21, ySize + vSize, uSize);

        return nv21;
    }

    /// <summary>
    /// https://stackoverflow.com/a/45926852
    /// </summary>
    private static byte[] NV21toJPEG(byte[] nv21, int width, int height)
    {
        MemoryStream outstran = new();
        YuvImage yuv = new(nv21, ImageFormatType.Nv21, width, height, null);
        yuv.CompressToJpeg(new global::Android.Graphics.Rect(0, 0, width, height), 100, outstran);
        return outstran.ToArray();
    }

    /// <summary>
    /// https://stackoverflow.com/a/44323834
    /// </summary>
    private static byte[] RotateJpeg(byte[] jpegData, int rotationDegrees)
    {
        var bmp = BitmapFactory.DecodeByteArray(jpegData, 0, jpegData.Length);
        var matrix = new Matrix();
        matrix.PostRotate(rotationDegrees);
        bmp = Bitmap.CreateBitmap(bmp, 0, 0, bmp.Width, bmp.Height, matrix, true);

        var ms = new MemoryStream();
        bmp.Compress(Bitmap.CompressFormat.Jpeg, 100, ms);
        return ms.ToArray();
    }

    private static int GetImageRotationCorrectionDegrees()
    {
        bool isAutoRotateEnabled = global::Android.Provider.Settings.System.GetInt(global::Android.App.Application.Context.ContentResolver, global::Android.Provider.Settings.System.AccelerometerRotation, 0) == 1;

        if (!isAutoRotateEnabled)
        {
            return 90;
        }

        global::Android.Views.IWindowManager windowManager = global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.WindowService).JavaCast<global::Android.Views.IWindowManager>();

        return windowManager.DefaultDisplay.Rotation switch
        {
            global::Android.Views.SurfaceOrientation.Rotation0 => 90,
            global::Android.Views.SurfaceOrientation.Rotation90 => 0,
            global::Android.Views.SurfaceOrientation.Rotation180 => -90,
            global::Android.Views.SurfaceOrientation.Rotation270 => 180,
            _ => 0,
        };
    }

    private void SafeCloseImageProxy(IImageProxy proxy)
    {
        try
        {
            proxy?.Close();
        }
        catch (ObjectDisposedException) { }
        catch (ArgumentException)
        {
            //Ignore argument exception, it will be thrown if BarcodeAnalyzer get disposed during processing
        }
    }

    private Task<Java.Lang.Object> ToAwaitableTask(global::Android.Gms.Tasks.Task task)
    {
        var taskCompletionSource = new TaskCompletionSource<Java.Lang.Object>();
        var taskCompleteListener = new TaskCompleteListener(taskCompletionSource);
        task.AddOnCompleteListener(taskCompleteListener);

        return taskCompletionSource.Task;
    }
}
