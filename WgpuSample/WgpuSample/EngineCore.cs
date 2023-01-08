#nullable enable
using u8 = System.Byte;
using u16 = System.UInt16;
using u32 = System.UInt32;
using u64 = System.UInt64;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using WgpuSample.Bind;

[assembly: DisableRuntimeMarshalling]

namespace WgpuSample
{
    internal unsafe static partial class EngineCore
    {
        private static EngineCoreConfig _config;
        private static int _isStarted = 0;

        [DoesNotReturn]
        public static Never EngineStart(in EngineCoreConfig config)
        {
            if(Interlocked.CompareExchange(ref _isStarted, 1, 0) == 1) {
                throw new InvalidOperationException("The engine is already running.");
            }
            if(Thread.CurrentThread.GetApartmentState() != ApartmentState.STA) {
                throw new InvalidOperationException("The thread should be STA. (for C#, mark main method as [STAThread] attribute.)");
            }

            _config = config;
            elffy_engine_start(&OnInit);
            throw new UnreachableException();

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
            static HostScreenCallbacks OnInit(HostScreenHandle screen)
            {
                _config.OnStart(screen);

                return new HostScreenCallbacks
                {
                    on_render = &OnRender,
                };
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
            static void OnRender(HostScreenHandle screen, RenderPassHandle render_pass) => _config.OnRender(screen, render_pass);
        }
    }

    internal readonly struct EngineCoreConfig
    {
        public required Action<HostScreenHandle> OnStart { get; init; }
        public required Action<HostScreenHandle, RenderPassHandle> OnRender { get; init; }
    }



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
