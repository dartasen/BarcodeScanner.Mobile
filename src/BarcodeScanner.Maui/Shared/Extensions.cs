namespace BarcodeScanner.Mobile;

public static class Extensions
{
    public static void AddBarcodeScannerHandler(this IMauiHandlersCollection handlers)
    {
        handlers.AddHandler(typeof(ICameraView), typeof(CameraViewHandler));
    }
}