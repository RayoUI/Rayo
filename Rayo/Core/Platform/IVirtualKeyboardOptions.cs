namespace Rayo.Core.Platform;

public interface IVirtualKeyboardOptions
{
    VirtualKeyboardType KeyboardType { get; }
    bool IsMultiline { get; }
}
