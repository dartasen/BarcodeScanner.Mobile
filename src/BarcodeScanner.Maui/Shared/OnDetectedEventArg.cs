namespace BarcodeScanner.Mobile;

[Serializable]
public class OnDetectedEventArg : EventArgs
{
    public List<BarcodeResult> BarcodeResults { get; set; }

    public OnDetectedEventArg()
    {
        BarcodeResults = new List<BarcodeResult>();
    }
}