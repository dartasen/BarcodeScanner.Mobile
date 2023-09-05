using System.Windows.Input;

namespace BarcodeScanner.Mobile;

public interface ICameraView : IView
{
    public static BindableProperty OnDetectedCommandProperty { get; set; }
    public ICommand OnDetectedCommand { get; set; }

    public static BindableProperty CameraEnabledProperty { get; set; }
    /// <summary>
    /// Disables or enables camera.
    /// </summary>
    public bool CameraEnabled { get; set; }

    public static BindableProperty VibrationOnDetectedProperty { get; set; }
    public bool VibrationOnDetected { get; set; }

    /// <summary>
    /// Disables or enables scanning
    /// </summary>
    public bool IsScanning { get; set; }

    public static BindableProperty TapToFocusEnabledProperty { get; set; }
    /// <summary>
    /// Disables or enables tap-to-focus.
    /// </summary>
    public bool TapToFocusEnabled { get; set; }

    public static BindableProperty PinchToZoomEnabledProperty { get; set; }
    /// <summary>
    /// Disables or enables tap-to-focus.
    /// </summary>
    public bool PinchToZoomEnabled { get; set; }

    public static BindableProperty TorchOnProperty { get; set; }
    /// <summary>
    /// Disables or enables torch
    /// </summary>
    public bool TorchOn { get; set; }

    public static BindableProperty ZoomProperty { get; set; }
    /// <summary>
    /// Set the zoom level for the image.
    /// </summary>
    public float Zoom { get; set; }

    public static BindableProperty CameraFacingProperty { get; set; }
    /// <summary>
    /// Select Back or Front camera.
    /// Default value is Back Camera
    /// </summary>
    public CameraFacing CameraFacing { get; set; }
    public static BindableProperty CaptureQualityProperty { get; set; }
    /// <summary>
    /// Set the capture quality for the image analysys.
    /// Reccomended and default value is Medium.
    /// Use highest values for more precision or lower for fast scanning.
    /// </summary>
    public CaptureQuality CaptureQuality { get; set; }

    public event EventHandler<OnDetectedEventArg> OnDetected;
    public void TriggerOnDetected(List<BarcodeResult> barCodeResults);
}