namespace BarcodeScanner.Mobile;

[Flags]
public enum BarcodeFormats
{
    CODE_128 = 1 << 1,
    CODE_39 = 1 << 2,
    CODE_93 = 1 << 3,
    CODA_BAR = 1 << 4,
    DATA_MATRIX = 1 << 5,
    EAN_13 = 1 << 6,
    EAN_8 = 1 << 7,
    ITF = 1 << 8,
    QR_CODE = 1 << 9,
    UPCA = 1 << 10,
    UPCE = 1 << 11,
    PDF_417 = 1 << 12,
    AZTEC = 1 << 13,
    ALL = CODE_128 | CODE_39 | CODE_93 | CODA_BAR | DATA_MATRIX | EAN_13 | EAN_8 | ITF | QR_CODE | UPCA | UPCE | PDF_417 | AZTEC
}