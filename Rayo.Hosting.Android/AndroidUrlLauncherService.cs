using Android.Content;
using Rayo.Core.Platform;

namespace Rayo.Hosting.Android;

public sealed class AndroidUrlLauncherService : IUrlLauncherService
{
    private readonly Context _context;

    public AndroidUrlLauncherService(Context context)
    {
        _context = context;
    }

    public bool Open(string url)
    {
        try
        {
            var intent = new Intent(Intent.ActionView);
            intent.SetData(global::Android.Net.Uri.Parse(url));
            intent.AddFlags(ActivityFlags.NewTask);
            _context.StartActivity(intent);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
