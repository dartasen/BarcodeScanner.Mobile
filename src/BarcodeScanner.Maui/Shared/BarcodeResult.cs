namespace BarcodeScanner.Mobile;

[Serializable]
public class BarcodeResult
{
    public BarcodeTypes BarcodeType { get; set; }

    public BarcodeFormats BarcodeFormat { get; set; }

    public string DisplayValue { get; set; }

    public string RawValue { get; set; }

    public override string ToString()
    {
        return $"[{nameof(BarcodeResult)}: BarcodeType: {BarcodeType}, BarcodeFormat: {BarcodeFormat}, DisplayValue: {DisplayValue}, RawValue: {RawValue}]";
    }
}