using Foundation;
using MLKit.BarcodeScanning;
using MLKit.Core;
using UIKit;

namespace BarcodeScanner.Mobile;

// All the code in this file is only included on iOS.
public class Methods
{
    public static void SetSupportBarcodeFormat(BarcodeFormats barcodeFormats)
    {
        BarcodeFormat supportFormats = Methods.ConvertBarcodeFormats(barcodeFormats);
        Configuration.BarcodeDetectorSupportFormat = supportFormats;
    }

    public static async Task<bool> AskForRequiredPermission()
    {
        try
        {
            PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                await Permissions.RequestAsync<Permissions.Camera>();
            }

            status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status == PermissionStatus.Granted)
            {
                return true;
            }
        }
        catch (Exception)
        {
            // Something went wrong
        }

        return false;
    }

    public static async Task<List<BarcodeResult>> ScanFromImage(byte[] imageArray)
    {
        UIImage image = new(NSData.FromArray(imageArray));
        var visionImage = new MLImage(image);
        //VisionImageMetadata metadata = new VisionImageMetadata();
        //VisionApi vision = VisionApi.Create();
        //VisionBarcodeDetector barcodeDetector = vision.GetBarcodeDetector(Configuration.BarcodeDetectorSupportFormat);
        //VisionBarcode[] barcodes = await barcodeDetector.DetectAsync(visionImage);
        BarcodeScannerOptions options = new(Configuration.BarcodeDetectorSupportFormat);
        MLKit.BarcodeScanning.BarcodeScanner barcodeScanner = MLKit.BarcodeScanning.BarcodeScanner.BarcodeScannerWithOptions(options);

        TaskCompletionSource<List<BarcodeResult>> tcs = new();

        barcodeScanner.ProcessImage(visionImage, (barcodes, error) =>
        {
            if (error != null)
            {
                Console.WriteLine($"Error occurred : {error}");
                tcs.TrySetResult(null);
                return;
            }
            if (barcodes == null || barcodes.Length == 0)
            {
                tcs.TrySetResult(new List<BarcodeResult>());
                return;
            }

            List<BarcodeResult> resultList = new();
            foreach (Barcode barcode in barcodes)
            {
                resultList.Add(ProcessBarcodeResult(barcode));
            }

            tcs.TrySetResult(resultList);
            return;
        });

        barcodeScanner.Dispose();

        return await tcs.Task;
    }

    public static BarcodeResult ProcessBarcodeResult(Barcode barcode)
    {
        List<Microsoft.Maui.Graphics.Point> cornerPoints = new();

        foreach (NSValue cornerPoint in barcode.CornerPoints)
        {
            cornerPoints.Add(new Point(cornerPoint.CGPointValue.X, cornerPoint.CGPointValue.Y));
        }

        return new BarcodeResult
        {
            BarcodeType = ConvertBarcodeResultTypes(barcode.ValueType),
            BarcodeFormat = (BarcodeFormats)barcode.Format,
            DisplayValue = barcode.DisplayValue,
            RawValue = barcode.RawValue
        };
    }

    internal static BarcodeTypes ConvertBarcodeResultTypes(BarcodeValueType visionBarcodeValueType)
    {
        return visionBarcodeValueType switch
        {
            BarcodeValueType.CalendarEvent => BarcodeTypes.CalendarEvent,
            BarcodeValueType.ContactInfo => BarcodeTypes.ContactInfo,
            BarcodeValueType.DriversLicense => BarcodeTypes.DriversLicense,
            BarcodeValueType.Email => BarcodeTypes.Email,
            BarcodeValueType.GeographicCoordinates => BarcodeTypes.GeographicCoordinates,
            BarcodeValueType.Isbn => BarcodeTypes.Isbn,
            BarcodeValueType.Phone => BarcodeTypes.Phone,
            BarcodeValueType.Product => BarcodeTypes.Product,
            BarcodeValueType.Sms => BarcodeTypes.Sms,
            BarcodeValueType.Text => BarcodeTypes.Text,
            BarcodeValueType.Unknown => BarcodeTypes.Unknown,
            BarcodeValueType.Url => BarcodeTypes.Url,
            BarcodeValueType.WiFi => BarcodeTypes.WiFi,
            _ => BarcodeTypes.Unknown,
        };
    }

    internal static BarcodeFormat ConvertBarcodeFormats(BarcodeFormats barcodeFormats)
    {
        BarcodeFormat visionBarcodeFormat = BarcodeFormat.Unknown;

        if (barcodeFormats.HasFlag(BarcodeFormats.CODA_BAR))
            visionBarcodeFormat |= BarcodeFormat.CodaBar;
        if (barcodeFormats.HasFlag(BarcodeFormats.CODE_128))
            visionBarcodeFormat |= BarcodeFormat.Code128;
        if (barcodeFormats.HasFlag(BarcodeFormats.CODE_39))
            visionBarcodeFormat |= BarcodeFormat.Code39;
        if (barcodeFormats.HasFlag(BarcodeFormats.CODE_93))
            visionBarcodeFormat |= BarcodeFormat.Code93;
        if (barcodeFormats.HasFlag(BarcodeFormats.DATA_MATRIX))
            visionBarcodeFormat |= BarcodeFormat.DataMatrix;
        if (barcodeFormats.HasFlag(BarcodeFormats.EAN_13))
            visionBarcodeFormat |= BarcodeFormat.Ean13;
        if (barcodeFormats.HasFlag(BarcodeFormats.EAN_8))
            visionBarcodeFormat |= BarcodeFormat.Ean8;
        if (barcodeFormats.HasFlag(BarcodeFormats.ITF))
            visionBarcodeFormat |= BarcodeFormat.Itf;
        if (barcodeFormats.HasFlag(BarcodeFormats.PDF_417))
            visionBarcodeFormat |= BarcodeFormat.Pdf417;
        if (barcodeFormats.HasFlag(BarcodeFormats.QR_CODE))
            visionBarcodeFormat |= BarcodeFormat.QrCode;
        if (barcodeFormats.HasFlag(BarcodeFormats.UPCA))
            visionBarcodeFormat |= BarcodeFormat.Upca;
        if (barcodeFormats.HasFlag(BarcodeFormats.UPCE))
            visionBarcodeFormat |= BarcodeFormat.Upce;
        if (barcodeFormats.HasFlag(BarcodeFormats.AZTEC))
            visionBarcodeFormat |= BarcodeFormat.Aztec;
        if (barcodeFormats.HasFlag(BarcodeFormats.ALL))
            visionBarcodeFormat |= BarcodeFormat.All;

        if (visionBarcodeFormat == BarcodeFormat.Unknown)
            visionBarcodeFormat = BarcodeFormat.All;

        return visionBarcodeFormat;
    }
}