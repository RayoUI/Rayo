using Examples;
using ExampleTabControl;
using Rayo.Core;

namespace Rayo.Test;

public class Program
{
    public static void Main(string[] args)
    {
        using var app = new UIApplication("Rayo - Layout Debug Demo", 800, 600);

        app.EnableVSync = true;
        app.SetUI<Soft>();
        app.Run();
    }
}