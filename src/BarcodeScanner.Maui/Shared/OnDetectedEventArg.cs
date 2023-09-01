namespace BarcodeScanner.Mobile;

public class OnDetectedEventArg : EventArgs
{
    public List<BarcodeResult> BarcodeResults { get; set; }

    public byte[] ImageData { get; set; }

    public OnDetectedEventArg()
    {
        ImageData = Array.Empty<byte>();
        BarcodeResults = new List<BarcodeResult>();
    }
}