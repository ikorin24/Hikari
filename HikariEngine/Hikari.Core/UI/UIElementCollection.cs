#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Hikari.UI;

[DebuggerDisplay("{DebugView,nq}")]
[DebuggerTypeProxy(typeof(UIElementCollectionDebugProxy))]
public sealed partial class UIElementCollection : IEnumerable<UIElement>, IReadOnlyCollection<UIElement>, IReadOnlyList<UIElement>
{
    private UIElement? _parent;
    private readonly List<UIElement> _children = new();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebugView => $"{nameof(UIElement)}[{Count}]";

    internal UIElement? Parent
    {
        get => _parent;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if(Interlocked.CompareExchange(ref _parent, value, null) != null) {
                ThrowInvalidInstance();
            }
            var tree = value.Screen?.UITree;
            foreach(var child in _children) {
                child.SetParent(value);
                if(tree != null) {
                    child.CreateModel(tree);
                }
            }
        }
    }

    public UIElement this[int index]
    {
        get => _children[index];
    }

    public int Count => _children.Count;


    static UIElementCollection() => RegistorSerdeConstructor();

    static partial void RegistorSerdeConstructor();

    public UIElementCollection()
    {
    }

    public void Add(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        _children.Add(element);
        _parent?.RequestRelayout();
        element.ModelDead.Subscribe(element =>
        {
            if(_children.Remove(element)) {
                element.ClearParent();
                _parent?.RequestRelayout();
            }
        }).AddTo(element.ModelSubscriptions);
        var parent = _parent;
        if(parent != null) {
            element.SetParent(parent);
            var tree = parent.Screen?.UITree;
            if(tree != null) {
                element.CreateModel(tree);
            }
        }
    }

    [DoesNotReturn]
    private static void ThrowInvalidInstance() => throw new InvalidOperationException("invalid instance");

    public Enumerator GetEnumerator() => new Enumerator(_children.GetEnumerator());

    IEnumerator<UIElement> IEnumerable<UIElement>.GetEnumerator() => _children.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _children.GetEnumerator();

    public struct Enumerator : IEnumerator<UIElement>
    {
        private List<UIElement>.Enumerator _enumerator;

        public UIElement Current => _enumerator.Current;

        object IEnumerator.Current => ((IEnumerator)_enumerator).Current;

        internal Enumerator(List<UIElement>.Enumerator enumerator)
        {
            _enumerator = enumerator;
        }

        public void Dispose() => _enumerator.Dispose();

        public bool MoveNext() => _enumerator.MoveNext();

        void IEnumerator.Reset() => ((IEnumerator)_enumerator).Reset();
    }

    private sealed class UIElementCollectionDebugProxy
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly UIElement[] _array;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public UIElement[] Items => _array;

        public UIElementCollectionDebugProxy(UIElementCollection collection)
        {
            _array = collection._children.ToArray();
        }
    }
}
