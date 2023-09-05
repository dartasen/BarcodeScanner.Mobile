using System.Windows.Input;

namespace BarcodeScanner.Mobile;

public partial class CameraView : View, ICameraView
{
    public static BindableProperty OnDetectedCommandProperty = BindableProperty.Create(nameof(OnDetectedCommand)
           , typeof(ICommand), typeof(CameraView)
           , null
           , defaultBindingMode: BindingMode.TwoWay
           , propertyChanged: (bindable, value, newValue) => ((CameraView)bindable).OnDetectedCommand = (ICommand)newValue);
    public ICommand OnDetectedCommand
    {
        get => (ICommand)GetValue(OnDetectedCommandProperty);
        set => SetValue(OnDetectedCommandProperty, value);
    }

    public static readonly BindableProperty CameraEnabledProperty = BindableProperty.Create(nameof(CameraEnabled)
    , typeof(bool)
    , typeof(CameraView)
    , true
    , defaultBindingMode: BindingMode.TwoWay
    , propertyChanged: (bindable, value, newValue) => ((CameraView)bindable).CameraEnabled = (bool)newValue);
    /// <summary>
    /// Disables or enables camera.
    /// </summary>
    public bool CameraEnabled
    {
        get => (bool)GetValue(CameraEnabledProperty);
        set => SetValue(CameraEnabledProperty, value);
    }

    public static BindableProperty VibrationOnDetectedProperty = BindableProperty.Create(nameof(VibrationOnDetected)
        , typeof(bool)
        , typeof(CameraView)
        , true
        , defaultBindingMode: BindingMode.TwoWay
        , propertyChanged: (bindable, value, newValue) => ((CameraView)bindable).VibrationOnDetected = (bool)newValue);
    public bool VibrationOnDetected
    {
        get => (bool)GetValue(VibrationOnDetectedProperty);
        set => SetValue(VibrationOnDetectedProperty, value);
    }

    public static BindableProperty IsScanningProperty = BindableProperty.Create(nameof(IsScanning)
        , typeof(bool)
        , typeof(CameraView)
        , true
        , defaultBindingMode: BindingMode.TwoWay
        , propertyChanged: (bindable, value, newValue) => ((CameraView)bindable).IsScanning = (bool)newValue);
    /// <summary>
    /// Disables or enables scanning
    /// </summary>
    public bool IsScanning
    {
        get => (bool)GetValue(IsScanningProperty);
        set => SetValue(IsScanningProperty, value);
    }

    public static BindableProperty TorchOnProperty = BindableProperty.Create(nameof(TorchOn)
        , typeof(bool)
        , typeof(CameraView)
        , false
        , defaultBindingMode: BindingMode.TwoWay
        , propertyChanged: (bindable, value, newValue) => ((CameraView)bindable).TorchOn = (bool)newValue);
    /// <summary>
    /// Disables or enables torch
    /// </summary>
    public bool TorchOn
    {
        get => (bool)GetValue(TorchOnProperty);
        set => SetValue(TorchOnProperty, value);
    }

    public static BindableProperty ZoomProperty = BindableProperty.Create(nameof(Zoom)
     , typeof(float)
     , typeof(CameraView)
     , 0f
     , defaultBindingMode: BindingMode.TwoWay
     , propertyChanged: (bindable, value, newValue) => ((CameraView)bindable).Zoom = (float)newValue);
    /// <summary>
    /// Set the zoom level for the image.
    /// </summary>
    public float Zoom
    {
        get => (float)GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public static BindableProperty CameraFacingProperty = BindableProperty.Create(nameof(CameraFacing)
        , typeof(CameraFacing)
        , typeof(CameraView)
        , CameraFacing.BACK
        , defaultBindingMode: BindingMode.TwoWay
        , propertyChanged: (bindable, value, newValue) => ((CameraView)bindable).CameraFacing = (CameraFacing)newValue);
    /// <summary>
    /// Select Back or Front camera.
    /// Default value is Back Camera
    /// </summary>
    public CameraFacing CameraFacing
    {
        get => (CameraFacing)GetValue(CameraFacingProperty);
        set => SetValue(CameraFacingProperty, value);
    }

    public static BindableProperty CaptureQualityProperty = BindableProperty.Create(nameof(CaptureQuality)
        , typeof(CaptureQuality)
        , typeof(CameraView)
        , CaptureQuality.MEDIUM
        , defaultBindingMode: BindingMode.TwoWay
        , propertyChanged: (bindable, value, newValue) => ((CameraView)bindable).CaptureQuality = (CaptureQuality)newValue);

    /// <summary>
    /// Set the capture quality for the image analysis.
    /// Reccomended and default value is Medium.
    /// Use highest values for more precision or lower for fast scanning.
    /// </summary>
    public CaptureQuality CaptureQuality
    {
        get => (CaptureQuality)GetValue(CaptureQualityProperty);
        set => SetValue(CaptureQualityProperty, value);
    }

    public static readonly BindableProperty TapToFocusEnabledProperty = BindableProperty.Create(nameof(TapToFocusEnabled)
    , typeof(bool)
    , typeof(CameraView)
    , false
    , defaultBindingMode: BindingMode.TwoWay
    , propertyChanged: (bindable, value, newValue) => ((CameraView)bindable).TapToFocusEnabled = (bool)newValue);
    /// <summary>
    /// Disables or enables tap-to-focus.
    /// </summary>
    public bool TapToFocusEnabled
    {
        get => (bool)GetValue(TapToFocusEnabledProperty);
        set => SetValue(TapToFocusEnabledProperty, value);
    }

    public static readonly BindableProperty PinchToZoomEnabledProperty = BindableProperty.Create(nameof(PinchToZoomEnabled)
    , typeof(bool)
    , typeof(CameraView)
    , true
    , defaultBindingMode: BindingMode.TwoWay
    , propertyChanged: (bindable, value, newValue) => ((CameraView)bindable).PinchToZoomEnabled = (bool)newValue);
    /// <summary>
    /// Disables or enables tap-to-focus.
    /// </summary>
    public bool PinchToZoomEnabled
    {
        get => (bool)GetValue(TapToFocusEnabledProperty);
        set => SetValue(TapToFocusEnabledProperty, value);
    }

    public event EventHandler<OnDetectedEventArg> OnDetected;
    public void TriggerOnDetected(List<BarcodeResult> barCodeResults)
    {
        if (VibrationOnDetected && barCodeResults.Count > 0)
        {
            Vibration.Vibrate(200);
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            OnDetected?.Invoke(this, new OnDetectedEventArg { BarcodeResults = barCodeResults });
            OnDetectedCommand?.Execute(new OnDetectedEventArg { BarcodeResults = barCodeResults });
        });
    }

    public CameraView()
    {
        this.Unloaded += CameraView_Unloaded;
    }

    /// <summary>
    /// Due to DisconnectHandler has to be called manually...we do it when the Window unloaded
    /// https://github.com/dotnet/maui/issues/3604
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CameraView_Unloaded(object sender, EventArgs e)
    {
        Handler?.DisconnectHandler();
    }
}
