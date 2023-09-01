using Android.Gms.Extensions;
using Android.Graphics;
using Android.Runtime;
using Java.Util;
using Xamarin.Google.MLKit.Vision.Barcode.Common;
using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Common;

namespace BarcodeScanner.Mobile;

// All the code in this file is only included on Android.
public class Methods
{
    internal static BarcodeTypes ConvertBarcodeResultTypes(int barcodeValueType)
    {
        return barcodeValueType switch
        {
            Barcode.TypeCalendarEvent => BarcodeTypes.CalendarEvent,
            Barcode.TypeContactInfo => BarcodeTypes.ContactInfo,
            Barcode.TypeDriverLicense => BarcodeTypes.DriversLicense,
            Barcode.TypeEmail => BarcodeTypes.Email,
            Barcode.TypeGeo => BarcodeTypes.GeographicCoordinates,
            Barcode.TypeIsbn => BarcodeTypes.Isbn,
            Barcode.TypePhone => BarcodeTypes.Phone,
            Barcode.TypeProduct => BarcodeTypes.Product,
            Barcode.TypeSms => BarcodeTypes.Sms,
            Barcode.TypeText => BarcodeTypes.Text,
            Barcode.TypeUrl => BarcodeTypes.Url,
            Barcode.TypeWifi => BarcodeTypes.WiFi,
            _ => BarcodeTypes.Unknown,
        };
    }

    internal static int ConvertBarcodeFormats(BarcodeFormats barcodeFormats)
    {
        int formats = Barcode.FormatAllFormats;

        if (barcodeFormats.HasFlag(BarcodeFormats.CODA_BAR))
            formats |= Barcode.FormatCodabar;
        if (barcodeFormats.HasFlag(BarcodeFormats.CODE_128))
            formats |= Barcode.FormatCode128;
        if (barcodeFormats.HasFlag(BarcodeFormats.CODE_93))
            formats |= Barcode.FormatCode93;
        if (barcodeFormats.HasFlag(BarcodeFormats.CODE_39))
            formats |= Barcode.FormatCode39;
        if (barcodeFormats.HasFlag(BarcodeFormats.CODA_BAR))
            formats |= Barcode.FormatCodabar;
        if (barcodeFormats.HasFlag(BarcodeFormats.DATA_MATRIX))
            formats |= Barcode.FormatDataMatrix;
        if (barcodeFormats.HasFlag(BarcodeFormats.EAN_13))
            formats |= Barcode.FormatEan13;
        if (barcodeFormats.HasFlag(BarcodeFormats.EAN_8))
            formats |= Barcode.FormatEan8;
        if (barcodeFormats.HasFlag(BarcodeFormats.ITF))
            formats |= Barcode.FormatItf;
        if (barcodeFormats.HasFlag(BarcodeFormats.PDF_417))
            formats |= Barcode.FormatPdf417;
        if (barcodeFormats.HasFlag(BarcodeFormats.QR_CODE))
            formats |= Barcode.FormatQrCode;
        if (barcodeFormats.HasFlag(BarcodeFormats.UPCA))
            formats |= Barcode.FormatUpcA;
        if (barcodeFormats.HasFlag(BarcodeFormats.UPCE))
            formats |= Barcode.FormatUpcE;
        if (barcodeFormats.HasFlag(BarcodeFormats.AZTEC))
            formats |= Barcode.FormatAztec;
        if (barcodeFormats.HasFlag(BarcodeFormats.ALL))
            formats |= Barcode.FormatAllFormats;
        return formats;
    }

    public static void SetSupportBarcodeFormat(BarcodeFormats barcodeFormats)
    {
        int supportFormats = Methods.ConvertBarcodeFormats(barcodeFormats);
        Configuration.BarcodeFormats = supportFormats;
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
            //Something went wrong
        }

        return false;
    }

    public static async Task<List<BarcodeResult>> ScanFromImage(byte[] imageArray)
    {
        using Bitmap bitmap = await BitmapFactory.DecodeByteArrayAsync(imageArray, 0, imageArray.Length);
        if (bitmap == null)
            return null;
        using var image = InputImage.FromBitmap(bitmap, 0);
        var scanner = BarcodeScanning.GetClient(new BarcodeScannerOptions.Builder().SetBarcodeFormats(Configuration.BarcodeFormats)
            .Build());
        return ProcessBarcodeResult(await scanner.Process(image));
    }

    public static List<BarcodeResult> ProcessBarcodeResult(Java.Lang.Object result)
    {
        if (result == null) return null;

        ArrayList javaList = result.JavaCast<ArrayList>();
        if (javaList?.IsEmpty ?? false) return null;

        List<BarcodeResult> resultList = new();
        foreach (var barcode in javaList.ToArray())
        {
            Barcode mapped = barcode.JavaCast<Barcode>();

            List<Microsoft.Maui.Graphics.Point> cornerPoints = new();

            foreach (Android.Graphics.Point cornerPoint in mapped.GetCornerPoints())
            {
                cornerPoints.Add(new Microsoft.Maui.Graphics.Point(cornerPoint.X, cornerPoint.Y));
            }

            resultList.Add(new BarcodeResult()
            {
                BarcodeType = ConvertBarcodeResultTypes(mapped.ValueType),
                BarcodeFormat = (BarcodeFormats)mapped.Format,
                DisplayValue = mapped.DisplayValue,
                RawValue = mapped.RawValue,
                CornerPoints = cornerPoints.ToArray(),
                RawData = mapped.GetRawBytes()
            });
        }

        return resultList;
    }
}