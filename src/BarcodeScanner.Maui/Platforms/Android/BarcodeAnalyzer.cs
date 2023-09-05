using Android.App;
using Android.Graphics;
using Android.OS;
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

    public BarcodeAnalyzer(ICameraView cameraView)
    {
        this.cameraView = cameraView;
        barcodeScanner = BarcodeScanning.GetClient(
            new BarcodeScannerOptions.Builder().SetBarcodeFormats(Configuration.BarcodeFormats).Build()
        );
    }

    public async void Analyze(IImageProxy proxy)
    {
        if (!(cameraView?.IsScanning ?? false)) return;
        if (proxy == null) return;

        try
        {
            global::Android.Media.Image mediaImage = proxy.Image;
            if (mediaImage == null) return;

            InputImage image = InputImage.FromMediaImage(mediaImage, proxy.ImageInfo.RotationDegrees);
            Java.Lang.Object results = await ToAwaitableTask(barcodeScanner.Process(image));
            List<BarcodeResult> barcodeResults = Methods.ProcessBarcodeResult(results);

            if (!cameraView.IsScanning)
            {
                return;
            }

            if (barcodeResults?.Count > 0)
            {
                cameraView?.TriggerOnDetected(barcodeResults);
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

    private static void SafeCloseImageProxy(IImageProxy proxy)
    {
        try
        {
            proxy?.Close();
        }
        catch (Exception)
        {

        }
    }

    private static Task<Java.Lang.Object> ToAwaitableTask(global::Android.Gms.Tasks.Task task)
    {
        var taskCompletionSource = new TaskCompletionSource<Java.Lang.Object>();
        var taskCompleteListener = new TaskCompleteListener(taskCompletionSource);
        task.AddOnCompleteListener(taskCompleteListener);

        return taskCompletionSource.Task;
    }
}
