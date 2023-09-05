namespace BarcodeScanner.Mobile;

public partial class Methods
{
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
}
