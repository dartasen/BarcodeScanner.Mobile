using Microsoft.Maui.Handlers;

#if IOS
using NativeCameraView = UIKit.UIView;
#elif ANDROID
using NativeCameraView = AndroidX.Camera.View.PreviewView;
#endif

namespace BarcodeScanner.Mobile;

public partial class CameraViewHandler : ViewHandler<ICameraView, NativeCameraView>
{
    public static readonly PropertyMapper<ICameraView, CameraViewHandler> CameraViewMapper = new()
    {
        [nameof(ICameraView.CameraEnabled)] = (handler, virtualView) => handler.HandleCameraEnabled(),
        [nameof(ICameraView.CameraFacing)] = (handler, virtualView) => handler.UpdateCamera(),
        [nameof(ICameraView.CaptureQuality)] = (handler, virtualView) => handler.UpdateResolution(),
        [nameof(ICameraView.TorchOn)] = (handler, virtualView) => handler.UpdateTorch(),
        [nameof(ICameraView.Zoom)] = (handler, virtualView) => handler.UpdateZoom(),
    };

    public static readonly CommandMapper<ICameraView, CameraViewHandler> CameraCommandMapper = new()
    {
    };

    public CameraViewHandler() : base(CameraViewMapper)
    {

    }

    public CameraViewHandler(PropertyMapper mapper = null) : base(mapper ?? CameraViewMapper)
    {

    }

    protected override void ConnectHandler(NativeCameraView nativeView)
    {
        base.ConnectHandler(nativeView);
        this.HandleCameraEnabled();
    }

    protected override void DisconnectHandler(NativeCameraView nativeView)
    {
        base.DisconnectHandler(nativeView);
        this.Stop();
        nativeView.Dispose();
    }
}