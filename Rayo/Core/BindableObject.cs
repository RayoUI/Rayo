namespace Rayo.Core;

using System.ComponentModel;
using System.Runtime.CompilerServices;

/// <summary>
/// Base class for all bindable objects. Provides property change notification
/// and reactive binding support similar to MAUI BindableObject.
/// </summary>
public abstract class BindableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the PropertyChanged event for the specified property.
    /// </summary>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Sets a field value and raises PropertyChanged if the value changed.
    /// Returns true if the value was changed.
    /// </summary>
    public bool SetProperty<T>(
        ref T field, 
        T value,
        Action? onBeforeChanged = null, 
        [CallerMemberName] string? propertyName = null) 
    { 
        if (EqualityComparer<T>.Default.Equals(field, value)) 
            return false; field = value; 
        
        onBeforeChanged?.Invoke(); 
        OnPropertyChanged(propertyName); 
        return true; 
    } 


    public bool SetPropertyCondition<T>(
        ref T field,
        T value,
        Func<T, T, bool>? shouldUpdate = null,
        Action? onBeforeChanged = null,
        [CallerMemberName] string? propertyName = null)
    {
        if (shouldUpdate is null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
        }
        else
        {
            if (!shouldUpdate(field, value))
                return false;
        }

        field = value;
        onBeforeChanged?.Invoke();
        OnPropertyChanged(propertyName);
        return true;
    }
}
