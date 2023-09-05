namespace BarcodeScanner.Mobile;

[Serializable]
public record BarcodeResult
{
    public BarcodeTypes BarcodeType { get; set; }

    public BarcodeFormats BarcodeFormat { get; set; }

    public string DisplayValue { get; set; }

    public string RawValue { get; set; }
}