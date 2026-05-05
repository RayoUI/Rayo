namespace Rayo.Core;

using Rayo.Reactivity;
using Rayo.Rendering;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Base class for layouts that contain multiple children.
/// Similar to MAUI Layout - examples: StackLayout, Grid, Flex, Absolute.
/// NOTE: During migration, inherits from UIElementBase for compatibility.
/// Eventually will inherit directly from UIElementBase when UIElementBase is removed.
/// </summary>
public abstract class Layout<T> : CompositeView<T> where T : Layout<T>
{
    /// <summary>
    /// Collection of child elements in this layout.
    /// NOTE: This hides the base Children property to expose it as read-only.
    /// </summary>
    //public new IReadOnlyList<VisualElement> Children => base.Children.AsReadOnly();


    /// <summary>
    /// Adds a child element to this layout.
    /// </summary>
    public virtual void Add(VisualElement child)
    {
        if (child == null)
            throw new ArgumentNullException(nameof(child));

        if (child.Parent != null)
            throw new InvalidOperationException("Element already has a parent. Remove it from the current parent first.");

        child.Parent = this;
        base.Children.Add(child);
        MarkNeedsLayout();
        RaiseTreeStructureChanged(this);
    }

    /// <summary>
    /// Inserts a child element at the specified index.
    /// </summary>
    public virtual void Insert(int index, VisualElement child)
    {
        if (child == null)
            throw new ArgumentNullException(nameof(child));

        if (child.Parent != null)
            throw new InvalidOperationException("Element already has a parent. Remove it from the current parent first.");

        child.Parent = this;
        base.Children.Insert(index, child);
        MarkNeedsLayout();
        RaiseTreeStructureChanged(this);
    }

    /// <summary>
    /// Removes a child element from this layout.
    /// </summary>
    public virtual bool Remove(VisualElement child)
    {
        bool removed = base.Children.Remove(child);
        if (removed && child.Parent == this)
        {
            child.Parent = null;
            MarkNeedsLayout();
            RaiseTreeStructureChanged(this);
        }

        return removed;
    }

    /// <summary>
    /// Removes the child at the specified index.
    /// </summary>
    public virtual void RemoveAt(int index)
    {
        var child = base.Children[index];
        base.Children.RemoveAt(index);
        child.Parent = null;
        MarkNeedsLayout();
        RaiseTreeStructureChanged(this);
    }

    /// <summary>
    /// Removes all children from this layout.
    /// </summary>
    public virtual void Clear()
    {
        if (base.Children is null || !base.Children.Any())
        {
            return;
        }


        // Use ToArray to avoid collection modification during iteration
        foreach (var child in base.Children.ToArray())
        {
            child.Parent = null;
        }
        base.Children.Clear();
        MarkNeedsLayout();
        RaiseTreeStructureChanged(this);
    }

    /// <summary>
    /// Adds a child element to this layout. Alias for Add() method.
    /// </summary>
    public new void AddChild(VisualElement child)
    {
        Add(child);
    }

    /// <summary>
    /// Removes a child element from this layout. Alias for Remove() method.
    /// </summary>
    public new bool RemoveChild(VisualElement child)
    {
        return Remove(child);
    }

    /// <summary>
    /// Removes all children from this layout. Alias for Clear() method.
    /// </summary>
    public new void ClearChildren()
    {
        Clear();
    }

    /// <summary>
    /// Gets the index of the specified child.
    /// </summary>
    public int IndexOf(VisualElement child)
    {
        return base.Children.IndexOf(child);
    }

    /// <summary>
    /// Gets whether this layout contains the specified child.
    /// </summary>
    public bool Contains(VisualElement child)
    {
        return base.Children.Contains(child);
    }

    /// <summary>
    /// Gets the number of children in this layout.
    /// </summary>
    public int Count => base.Children.Count;

    /// <summary>
    /// Gets a direct reference to the internal children list (for performance).
    /// </summary>
    protected List<VisualElement> ChildrenInternal => base.Children;


    // =========================================================================
    // LAYOUT PROPERTIES
    // =========================================================================

    /// <summary>
    /// Indicates if this layout should expand to fill available space.
    /// By default is true for layouts, unlike regular controls.
    /// </summary>
    [LayoutProperty]
    public bool ShouldExpand
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = true;


    // =========================================================================
    // LAYOUT EXPANSION
    // =========================================================================

    /// <summary>
    /// Disables automatic expansion of the layout.
    /// Useful when you want the layout to size to its content.
    /// </summary>
    public T DisableExpansion()
    {
        ShouldExpand = false;
        return (T)this;
    }

    /// <summary>
    /// Enables automatic expansion of the layout (default behavior).
    /// </summary>
    public T EnableExpansion()
    {
        ShouldExpand = true;
        return (T)this;
    }

    
}

/// <summary>
/// Generic base class for LayoutBase with fluent API support using CRTP pattern.
/// </summary>
//public abstract class Layout<T> : Layout where T : Layout<T>
//{
    
//}
