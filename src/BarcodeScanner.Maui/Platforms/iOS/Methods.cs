using Foundation;
using UIKit;
using Vision;

namespace BarcodeScanner.Mobile;

// All the code in this file is only included on iOS.
public partial class Methods
{
    public static void SetSupportBarcodeFormat(BarcodeFormats barcodeFormats)
    {
        VNBarcodeSymbology[] supportFormats = Methods.SelectedSymbologies(barcodeFormats);
        Configuration.BarcodeDetectorSupportFormat = supportFormats;
    }

    public static async Task<List<BarcodeResult>> ScanFromImage(byte[] imageArray)
    {
        VNBarcodeObservation[] observations = null;

        UIImage image = UIImage.LoadFromData(NSData.FromArray(imageArray));
        VNDetectBarcodesRequest barcodeRequest = new((request, error) =>
        {
            if (error is null)
            {
                observations = request.GetResults<VNBarcodeObservation>();
            }
        });

        VNImageRequestHandler handler = new(image.CGImage, new NSDictionary());
        await Task.Run(() => handler.Perform(new VNRequest[] { barcodeRequest }, out _));

        return ProcessBarcodeResult(observations);
    }

    internal static List<BarcodeResult> ProcessBarcodeResult(VNBarcodeObservation[] result)
    {
        List<BarcodeResult> resultList = new();
        if (result?.Length == 0) return resultList;

        foreach (VNBarcodeObservation barcode in result)
        {
            resultList.Add(new BarcodeResult()
            {
                BarcodeType = BarcodeTypes.Unknown,
                BarcodeFormat = ConvertFromIOSFormats(barcode.Symbology),
                DisplayValue = barcode.PayloadStringValue,
                RawValue = barcode.PayloadStringValue
            });
        };

        return resultList;
    }

    internal static VNBarcodeSymbology[] SelectedSymbologies(BarcodeFormats barcodeFormats)
    {
        List<VNBarcodeSymbology> symbologiesList = new();

        if (barcodeFormats.HasFlag(BarcodeFormats.AZTEC))
            symbologiesList.Add(VNBarcodeSymbology.Aztec);
        if (barcodeFormats.HasFlag(BarcodeFormats.CODE_39))
        {
            symbologiesList.Add(VNBarcodeSymbology.Code39);
            symbologiesList.Add(VNBarcodeSymbology.Code39Checksum);
            symbologiesList.Add(VNBarcodeSymbology.Code39FullAscii);
            symbologiesList.Add(VNBarcodeSymbology.Code39FullAsciiChecksum);
        }
        if (barcodeFormats.HasFlag(BarcodeFormats.CODE_93))
        {
            symbologiesList.Add(VNBarcodeSymbology.Code93);
            symbologiesList.Add(VNBarcodeSymbology.Code93i);
        }
        if (barcodeFormats.HasFlag(BarcodeFormats.CODE_128))
            symbologiesList.Add(VNBarcodeSymbology.Code128);
        if (barcodeFormats.HasFlag(BarcodeFormats.DATA_MATRIX))
            symbologiesList.Add(VNBarcodeSymbology.DataMatrix);
        if (barcodeFormats.HasFlag(BarcodeFormats.EAN_8))
            symbologiesList.Add(VNBarcodeSymbology.Ean8);
        if (barcodeFormats.HasFlag(BarcodeFormats.EAN_13))
            symbologiesList.Add(VNBarcodeSymbology.Ean13);
        if (barcodeFormats.HasFlag(BarcodeFormats.I2OF5))
        {
            symbologiesList.Add(VNBarcodeSymbology.I2OF5);
            symbologiesList.Add(VNBarcodeSymbology.I2OF5Checksum);
        }
        if (barcodeFormats.HasFlag(BarcodeFormats.ITF))
            symbologiesList.Add(VNBarcodeSymbology.Itf14);
        if (barcodeFormats.HasFlag(BarcodeFormats.QR_CODE))
            symbologiesList.Add(VNBarcodeSymbology.QR);
        if (barcodeFormats.HasFlag(BarcodeFormats.UPCE))
            symbologiesList.Add(VNBarcodeSymbology.Upce);

        /* ONLY FOR IOS >= 15
        if (barcodeFormats.HasFlag(BarcodeFormats.CODA_BAR) && isAtLeast15)
            symbologiesList.Add(VNBarcodeSymbology.Codabar);
        if (barcodeFormats.HasFlag(BarcodeFormats.GS1_DATABAR) && isAtLeast15)
        {
            symbologiesList.Add(VNBarcodeSymbology.GS1DataBar);
            symbologiesList.Add(VNBarcodeSymbology.GS1DataBarLimited);
            symbologiesList.Add(VNBarcodeSymbology.GS1DataBarExpanded);
        }
        if (barcodeFormats.HasFlag(BarcodeFormats.MICRO_QR) && isAtLeast15)
            symbologiesList.Add(VNBarcodeSymbology.MicroQR);
        if (barcodeFormats.HasFlag(BarcodeFormats.MICRO_PDF_417) && isAtLeast15)
            symbologiesList.Add(VNBarcodeSymbology.MicroPdf417);
        */

        return symbologiesList.ToArray();
    }

    private static BarcodeFormats ConvertFromIOSFormats(VNBarcodeSymbology symbology)
    {
        return symbology switch
        {
            VNBarcodeSymbology.Aztec => BarcodeFormats.AZTEC,
            //VNBarcodeSymbology.Codabar => BarcodeFormats.CODA_BAR,
            VNBarcodeSymbology.Code39 => BarcodeFormats.CODE_39,
            VNBarcodeSymbology.Code39Checksum => BarcodeFormats.CODE_39,
            VNBarcodeSymbology.Code39FullAscii => BarcodeFormats.CODE_39,
            VNBarcodeSymbology.Code39FullAsciiChecksum => BarcodeFormats.CODE_39,
            VNBarcodeSymbology.Code93 => BarcodeFormats.CODE_93,
            VNBarcodeSymbology.Code93i => BarcodeFormats.CODE_93,
            VNBarcodeSymbology.Code128 => BarcodeFormats.CODE_128,
            VNBarcodeSymbology.DataMatrix => BarcodeFormats.DATA_MATRIX,
            VNBarcodeSymbology.Ean8 => BarcodeFormats.EAN_8,
            VNBarcodeSymbology.Ean13 => BarcodeFormats.EAN_13,
            //VNBarcodeSymbology.GS1DataBar => BarcodeFormats.GS1_DATABAR,
            //VNBarcodeSymbology.GS1DataBarExpanded => BarcodeFormats.GS1_DATABAR,
            //VNBarcodeSymbology.GS1DataBarLimited => BarcodeFormats.GS1_DATABAR,
            VNBarcodeSymbology.I2OF5 => BarcodeFormats.I2OF5,
            VNBarcodeSymbology.I2OF5Checksum => BarcodeFormats.I2OF5,
            VNBarcodeSymbology.Itf14 => BarcodeFormats.ITF,
            //VNBarcodeSymbology.MicroPdf417 => BarcodeFormats.MICRO_PDF_417,
            //VNBarcodeSymbology.MicroQR => BarcodeFormats.MICRO_QR,
            VNBarcodeSymbology.Pdf417 => BarcodeFormats.PDF_417,
            VNBarcodeSymbology.QR => BarcodeFormats.QR_CODE,
            VNBarcodeSymbology.Upce => BarcodeFormats.UPCE,
            _ => BarcodeFormats.NONE
        };
    }
}