using System;
using System.Text;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Buffers;
using System.Diagnostics;

namespace WgpuSample
{
    internal unsafe static partial class NativeApi
    {
        private const string Library = "wgpu_sample";

        [DebuggerHidden]
        public static void start() => start(&OnError);

        [LibraryImport(Library), UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static partial void start(delegate* unmanaged[Cdecl]<byte*, nuint, void> onError);

        [DebuggerHidden]
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static void OnError(byte* message, nuint len)
        {
            var bytelen = (int)Math.Min(len, int.MaxValue);
            var messageStr = Encoding.UTF8.GetString(message, bytelen);
            throw new NativeApiException(messageStr);
        }
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
}
