using AVFoundation;
using BarcodeScanner.Mobile;
using CoreMedia;
using Vision;

namespace BarcodeScanner.Mobile.Platforms.iOS;

internal class BarcodeAnalyzer : AVCaptureVideoDataOutputSampleBufferDelegate
{
    public event Action<OnDetectedEventArg> OnDetected;

    private readonly VNDetectBarcodesRequest barcodeRequest;
    private readonly VNSequenceRequestHandler sequenceRequestHandler;
    private readonly ICameraView cameraView;

    private HashSet<BarcodeResult> barcodeResults;

    internal BarcodeAnalyzer(ICameraView cameraView)
    {
        this.cameraView = cameraView;

        sequenceRequestHandler = new VNSequenceRequestHandler();
        barcodeRequest = new VNDetectBarcodesRequest((request, error) => {
            if (error is null)
            {
                barcodeResults = Methods.ProcessBarcodeResult(request.GetResults<VNBarcodeObservation>()).ToHashSet();
            }
        });

        VNBarcodeSymbology[] selectedSymbologies = Configuration.BarcodeDetectorSupportFormat;
        if (selectedSymbologies is not null)
        {
            barcodeRequest.Symbologies = selectedSymbologies;
        }
    }

    public override void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
    {
        try
        {
            if (sampleBuffer is null || !cameraView.IsScanning)
            {
                return;
            }

            sequenceRequestHandler.Perform(new VNRequest[] { barcodeRequest }, sampleBuffer, out _);

            if (barcodeResults is not null && cameraView is not null)
            {
                OnDetected?.Invoke(new OnDetectedEventArg { BarcodeResults = barcodeResults.ToList() });
            }
        }
        catch (Exception)
        {

        }
        finally
        {
            SafeCloseSampleBuffer(sampleBuffer);
        }
    }

    private static void SafeCloseSampleBuffer(CMSampleBuffer buffer)
    {
        try
        {
            buffer?.Dispose();
        }
        catch (Exception)
        {
            // Ignore
        }
    }
}
