using Rayo.Reactivity;
using Rayo.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace Rayo.Core;

/// <summary>
/// Base class for UI elements that manage internal children for composition.
/// Examples: Button (icon+text), TabControl (tabs+content), complex controls.
/// Not for public containers - use Layout for that.
/// </summary>
public abstract class CompositeView<T> : VisualElement<T> where T : CompositeView<T>
{
    #region Internal Children Management

    /// <summary>
    /// Protected property for complex controls to manage internal children.
    /// This is NOT public API - only for internal control composition.
    /// </summary>
    public List<VisualElement> Children
    {
        get => field;
        set
        {
            var incoming = value ?? [];
            var current  = field;

            // Detach old children that are not in the incoming list.
            if (current != null)
            {
                foreach (var old in current)
                {
                    if (old != null && old.Parent == this && !incoming.Contains(old))
                        old.Parent = null;
                }
            }

            // Attach new children that don't already have this as parent.
            foreach (var child in incoming)
            {
                if (child != null && child.Parent != this)
                    child.Parent = this;
            }

            this.SetProperty(ref field, incoming, () => RaiseTreeStructureChanged(this));
        }
    } = [];

    /// <summary>
    /// Gets all children of this element (for rendering and layout).
    /// </summary>
    internal override IEnumerable<VisualElement> GetChildren()
    {
        if (Children != null && Children.Count > 0)
        {
            return Children.Where(c => c != null)!;
        }

        return Enumerable.Empty<VisualElement>();
    }

    /// <summary>
    /// Protected method for complex controls to add internal children.
    /// This is NOT public API - only for internal control composition.
    /// </summary>
    public void AddChild(VisualElement? child)
    {
        if (child == null) return;
        child.Parent = this;
        Children.Add(child);
        MarkNeedsLayout();
        RaiseTreeStructureChanged(this);
    }

    /// <summary>
    /// Protected method for complex controls to remove internal children.
    /// This is NOT public API - only for internal control composition.
    /// </summary>
    public void RemoveChild(VisualElement? child)
    {
        if (child == null) return;
        child.Parent = null;
        Children.Remove(child);
        MarkNeedsLayout();
        RaiseTreeStructureChanged(this);
    }

    /// <summary>
    /// Protected method for complex controls to clear internal children.
    /// This is NOT public API - only for internal control composition.
    /// </summary>
    public void ClearChildren()
    {
        foreach (var child in Children.ToArray())
        {
            child.Parent = null;
        }
        Children.Clear();
        MarkNeedsLayout();
        RaiseTreeStructureChanged(this);
    }

    #endregion

    }
