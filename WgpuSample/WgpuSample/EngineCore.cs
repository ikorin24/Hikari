#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using WgpuSample.Bind;
using System.Text;
using System.Collections.Concurrent;

[assembly: DisableRuntimeMarshalling]

namespace WgpuSample
{
    internal unsafe static partial class EngineCore
    {
        private static readonly ConcurrentDictionary<uint, string> _errorMessageStore = new();
        private static EngineConfig _config;
        private static int _isStarted = 0;

        [DoesNotReturn]
        public static Never EngineStart(in EngineConfig config)
        {
            if(Interlocked.CompareExchange(ref _isStarted, 1, 0) == 1) {
                throw new InvalidOperationException("The engine is already running.");
            }
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                if(Thread.CurrentThread.GetApartmentState() != ApartmentState.STA) {
                    throw new InvalidOperationException("The thread should be STA. (for C#, mark main method as [STAThread] attribute.)");
                }
            }

            _config = config;
            var engineConfig = new EngineCoreConfig
            {
                on_screen_init = new(&OnInit),
                err_dispatcher = &DispatchError,
            };
            var screenConfig = new HostScreenConfig
            {
                backend = wgpu_Backends.ALL,
                width = 1280,
                height = 720,
                style = WindowStyle.Default,
                title = Slice.FromFixedSpanUnsafe("Elffy"u8),
            };
            elffy_engine_start(&engineConfig, &screenConfig);
            throw new UnreachableException();

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
            static HostScreenCallbacks OnInit(HostScreenHandle screen, HostScreenInfo* info)
            {
                _config.OnStart(screen, in *info);

                return new HostScreenCallbacks
                {
                    on_render = &OnRender,
                };
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
            static void DispatchError(uint messageId, byte* messagePtr, nuint messageByteLen)
            {
                const int MaxByteLen = 1024 * 1024;
                var len = (int)nuint.Min(messageByteLen, MaxByteLen);
                var message = Encoding.UTF8.GetString(messagePtr, len);
                _errorMessageStore[messageId] = message;
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
            static void OnRender(HostScreenHandle screen, RenderPassHandle render_pass) => _config.OnRender(screen, render_pass);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckResult(uint result)
        {
            if(result != 0) {
                ThrowNativeError(result);

                [DoesNotReturn]
                static void ThrowNativeError(uint messageId)
                {
                    Debug.Assert(messageId != 0);
                    if(_errorMessageStore.TryRemove(messageId, out var message) == false) {
                        message = "Some error occurred in the native code, but the error message could not be retrieved.";
                    }

                    throw new Exception(message);
                }
            }


        }
    }

    internal readonly struct EngineConfig
    {
        public required EngineCoreStartAction OnStart { get; init; }
        public required Action<HostScreenHandle, RenderPassHandle> OnRender { get; init; }
    }

    internal delegate void EngineCoreStartAction(HostScreenHandle screen, in HostScreenInfo info);



    public sealed class NativeApiException : Exception
    {
        public NativeApiException()
        {
        }

        public NativeApiException(string message) : base(message)
        {
        }
    }

    public sealed class Never
    {
        private Never() => throw new UnreachableException();
    }
}
