﻿#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Hikari;

/// <summary>GC callback class</summary>
public static class GCCallback
{
    /// <summary>Register callback of GC in specified generation. Callback is alive while it returns true.</summary>
    /// <remarks>
    /// <paramref name="generation"/> is min gen where callback fires.
    /// For example, if regester to gen2, callback fires on gen0, gen1 and gen2.
    /// </remarks>
    /// <param name="callback">callback method which fires after GC</param>
    /// <param name="generation">target generation of GC</param>
    public static void Register(Func<bool> callback, int generation)
    {
        ArgumentNullException.ThrowIfNull(callback);
        if((uint)generation > (uint)GC.MaxGeneration) {
            ThrowOutOfRange();
            [DoesNotReturn] static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(generation));
        }

        // This is zombie object. No one can access it but it survives.
#pragma warning disable CA1806
        new CallbackHolder(callback, generation);
#pragma warning restore CA1806
    }

    /// <summary>Register callback of GC in specified generation. Callback is alive while it returns true, or argument of it is alive.</summary>
    /// <remarks>
    /// <paramref name="generation"/> is min gen where callback fires.
    /// For example, if regester to gen2, callback fires on gen0, gen1 and gen2.
    /// </remarks>
    /// <param name="callback">callback method which fires after GC</param>
    /// <param name="callbackArg">argument of callback method</param>
    /// <param name="generation">target generation of GC</param>
    public static void Register(Func<object, bool> callback, object callbackArg, int generation)
    {
        ArgumentNullException.ThrowIfNull(callback);
        ArgumentNullException.ThrowIfNull(callbackArg);
        if((uint)generation > (uint)GC.MaxGeneration) {
            ThrowOutOfRange();
            [DoesNotReturn] static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(generation));
        }

        // This is zombie object. No one can access it but it survives.
#pragma warning disable CA1806
        new CallbackWithArgHolder(callback, callbackArg, generation);
#pragma warning restore CA1806
    }

    private class CallbackHolder
    {
        private int _generation;
        private readonly Func<bool> _callback;
        private readonly int _targetGen;
        private int _targetGenCount;

        public CallbackHolder(Func<bool> callback, int targetGen)
        {
            _callback = callback;
            _targetGen = targetGen;
            _targetGenCount = GC.CollectionCount(targetGen);
        }

        private CallbackHolder(Func<bool> callback, int targetGen, int targetGenCount)
        {
            // Create clone as generation 0.
            _callback = callback;
            _targetGen = targetGen;
            _targetGenCount = targetGenCount;
        }

        ~CallbackHolder()
        {
            // Keep this object alive until the callback returns false.

            var targetGenCount = GC.CollectionCount(_targetGen);
            if(targetGenCount != _targetGenCount) {
                _targetGenCount = targetGenCount;
                try {
                    if(!_callback()) { return; }
                }
                catch {
#if DEBUG
                    throw;  // throw if DEBUG
#endif
                }
            }

            // Revive !!
            if(_targetGen < GC.MaxGeneration) {
                if(_generation == _targetGen) {
                    // `this` object becomes dead. Create clone as generation 0.
#pragma warning disable CA1806
                    new CallbackHolder(_callback, _targetGen, _targetGenCount);
#pragma warning restore CA1806
                }
                else {
                    _generation++;
                    GC.ReRegisterForFinalize(this);
                }
            }
            else {
                GC.ReRegisterForFinalize(this);
            }
        }
    }

    private class CallbackWithArgHolder
    {
        private int _generation;
        private readonly Func<object, bool> _callback;
        private readonly GCHandle _callbackArg;
        private readonly int _targetGen;
        private int _targetGenCount;

        public CallbackWithArgHolder(Func<object, bool> callback, object callbackArg, int targetGen)
        {
            _callback = callback;
            _callbackArg = GCHandle.Alloc(callbackArg, GCHandleType.Weak);
            _targetGen = targetGen;
            _targetGenCount = GC.CollectionCount(targetGen);
        }

        private CallbackWithArgHolder(Func<object, bool> callback, GCHandle callbackArg, int targetGen, int targetGenCount)
        {
            // Create clone as generation 0
            _callback = callback;
            _callbackArg = callbackArg;
            _targetGen = targetGen;
            _targetGenCount = targetGenCount;
        }

        ~CallbackWithArgHolder()
        {
            // Keep this object alive until the callback returns false or arg is alive.

            var targetGenCount = GC.CollectionCount(_targetGen);
            if(targetGenCount != _targetGenCount) {
                try {
                    _targetGenCount = targetGenCount;
                    var arg = _callbackArg.Target;
                    if(arg is null) {
                        _callbackArg.Free();
                        return;
                    }
                    if(!_callback(arg)) { return; }
                }
                catch {
#if DEBUG
                    throw;  // throw if DEBUG
#endif

                }
            }

            // Revive !!
            if(_targetGen < GC.MaxGeneration) {
                if(_generation == _targetGen) {
                    // `this` object becomes dead. Create clone as generation 0.
#pragma warning disable CA1806
                    new CallbackWithArgHolder(_callback, _callbackArg, _targetGen, _targetGenCount);
#pragma warning restore CA1806
                }
                else {
                    _generation++;
                    GC.ReRegisterForFinalize(this);
                }
            }
            else {
                GC.ReRegisterForFinalize(this);
            }
        }
    }
}
