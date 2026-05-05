namespace Rayo.Hosting.Android;

public sealed class AndroidVirtualKeyboardService : Rayo.Core.Platform.IVirtualKeyboardService
{
    private readonly global::Android.Views.View _view;
    private readonly global::Android.Content.Context _context;

    public AndroidVirtualKeyboardService(global::Android.Views.View view, global::Android.Content.Context context)
    {
        _view = view;
        _context = context;
    }

    public void Show()
    {
        var imm = _context.GetSystemService(global::Android.Content.Context.InputMethodService)
            as global::Android.Views.InputMethods.InputMethodManager;
        if (imm == null)
        {
            return;
        }

        _view.Post(() =>
        {
            _view.RequestFocus();
            imm.ShowSoftInput(_view, global::Android.Views.InputMethods.ShowFlags.Implicit);
        });
    }

    public void Hide()
    {
        var imm = _context.GetSystemService(global::Android.Content.Context.InputMethodService)
            as global::Android.Views.InputMethods.InputMethodManager;
        if (imm == null)
        {
            return;
        }

        _view.Post(() =>
        {
            imm.HideSoftInputFromWindow(_view.WindowToken, global::Android.Views.InputMethods.HideSoftInputFlags.None);
        });
    }
}
