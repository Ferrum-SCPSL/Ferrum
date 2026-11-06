using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Ferrum.Interop;

namespace Ferrum.Interop.SmokeTests;

public static class Program
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate UIntPtr AllocationCount();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate NativeStatus Panic(out NativeBuffer buffer);

    private static int assertions;

    private static void Check(bool condition, string description)
    {
        if (!condition) throw new Exception("FAIL: " + description);
        assertions++;
    }

    private static T Throws<T>(Action action, string description) where T : Exception
    {
        try { action(); }
        catch (T error) { assertions++; return error; }
        throw new Exception("FAIL: expected " + typeof(T).Name + ": " + description);
    }

    private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 3) throw new ArgumentException("Usage: SmokeTests <test-hooks-native-library> <fixture-directory> <production-native-library>");
            string native = Path.GetFullPath(args[0]);
            string fixtures = Path.GetFullPath(args[1]);
            using var bridge = new NativeBridge(native);
            NativeModule module = NativeModule.Open(native);
            var allocations = module.GetExport<AllocationCount>("ferrum_test_outstanding_allocations");
            var process = module.GetExport<ProcessUtf8>("ferrum_process_utf8");
            var free = module.GetExport<FreeBuffer>("ferrum_buffer_free");
            var panic = module.GetExport<Panic>("ferrum_test_panic");
            NativeModule production = NativeModule.Open(args[2]);
            Throws<NativeBridgeException>(() => production.GetExport<AllocationCount>("ferrum_test_outstanding_allocations"), "production excludes allocation hook");
            Throws<NativeBridgeException>(() => production.GetExport<Panic>("ferrum_test_panic"), "production excludes panic hook");

            Check(Marshal.SizeOf<NativeBuffer>() == 16, "buffer size");
            Check(Marshal.OffsetOf<NativeBuffer>(nameof(NativeBuffer.Length)).ToInt32() == 8, "length offset");
            Check(allocations().ToUInt64() == 0, "initial allocation count");
            foreach (string value in new[] { "", "ASCII", "Кириллица", "emoji 🦀🚀", "before\0after", new string('Ж', 131072) })
            {
                int calls = 0;
                string result = bridge.RoundTrip(value, message =>
                {
                    Check(message == "Received UTF-8: " + value, "callback UTF-8 matches");
                    calls++;
                    GC.Collect();
                });
                Check(result == value && calls == 1, "round-trip and exactly one callback");
                Check(allocations().ToUInt64() == 0, "round-trip output released");
            }

            var callbackError = Throws<NativeBridgeException>(
                () => bridge.RoundTrip("callback failure", _ => throw new InvalidOperationException("host error")), "callback exception");
            Check(callbackError.Status == NativeStatus.CallbackError && callbackError.InnerException is InvalidOperationException,
                "callback exception contained and retained in managed diagnostics");
            Check(allocations().ToUInt64() == 0, "error payload released");
            Throws<EncoderFallbackException>(() => bridge.RoundTrip("\ud800", _ => { }), "unpaired surrogate rejected before FFI");
            Throws<ArgumentNullException>(() => bridge.RoundTrip(null!, _ => { }), "null managed input");
            Throws<ArgumentNullException>(() => bridge.RoundTrip("text", null!), "null managed callback");

            LogCallback logger = (_, _) => 0;
            NativeBuffer output;
            Check(process(IntPtr.Zero, new UIntPtr(1), logger, out output) == NativeStatus.InvalidArgument, "null plus nonzero length");
            ReleaseError(ref output, free, "Invalid input");
            Check(process(IntPtr.Zero, UIntPtr.Zero, null, out output) == NativeStatus.InvalidArgument, "missing callback");
            ReleaseError(ref output, free, "Missing log");

            byte[] invalidUtf8 = { 0xff };
            GCHandle invalidPinned = GCHandle.Alloc(invalidUtf8, GCHandleType.Pinned);
            try
            {
                Check(process(invalidPinned.AddrOfPinnedObject(), new UIntPtr(1), logger, out output) == NativeStatus.InvalidUtf8,
                    "invalid UTF-8 status");
                ReleaseError(ref output, free, "not valid UTF-8");
            }
            finally { invalidPinned.Free(); }

            Check(panic(out output) == NativeStatus.Panic, "unwinding panic contained");
            ReleaseError(ref output, free, "panic contained");
            Check(bridge.RoundTrip("after panic", _ => { }) == "after panic", "process still works after panic");

            for (int i = 0; i < 10000; i++)
            {
                string input = "cycle " + i + " 🦀";
                Check(bridge.RoundTrip(input, _ => { }) == input, "stress round-trip");
                if (i % 128 == 0) { GC.Collect(); GC.WaitForPendingFinalizers(); }
            }
            GC.KeepAlive(logger);
            Check(allocations().ToUInt64() == 0, "zero outstanding allocations after stress and failures");

            Throws<FileNotFoundException>(() => new NativeBridge(Path.Combine(fixtures, "absent-native.dll")), "missing native file");
            string extension = NativeModule.IsWindows ? ".dll" : ".so";
            var missing = Throws<NativeBridgeException>(() => new NativeBridge(Path.Combine(fixtures, "fixture_missing" + extension)), "missing export");
            Check(missing.Message.Contains("ferrum_process_utf8"), "missing export named");
            var wrongAbi = Throws<NativeBridgeException>(() => new NativeBridge(Path.Combine(fixtures, "fixture_abi" + extension)), "incompatible ABI");
            Check(wrongAbi.Message.Contains("999"), "incompatible version reported");
            string invalidImage = Path.Combine(fixtures, "invalid-image" + extension);
            File.WriteAllText(invalidImage, "not an executable image");
            Throws<NativeBridgeException>(() => new NativeBridge(invalidImage), "invalid native architecture/image diagnosis");
            File.Delete(invalidImage);

            bridge.Dispose();
            bridge.Dispose();
            Throws<ObjectDisposedException>(() => bridge.RoundTrip("disabled", _ => { }), "disabled bridge rejects calls");
            using (var reopened = new NativeBridge(native))
                Check(reopened.RoundTrip("reenabled", _ => { }) == "reenabled", "same process can reuse loaded module");
            Console.WriteLine($"PASS: {assertions} assertions; real C# -> Rust -> C#; {NativeBridge.PlatformName}; zero outstanding buffers.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    private static void ReleaseError(ref NativeBuffer output, FreeBuffer free, string expected)
    {
        Check(NativeBridge.ReadUtf8(output.Data, output.Length).Contains(expected), "owned error diagnostic");
        Check(free(ref output) == NativeStatus.Ok, "free error buffer");
        Check(output.Data == IntPtr.Zero && output.Length == UIntPtr.Zero, "descriptor reset after free");
        Check(free(ref output) == NativeStatus.Ok, "free empty descriptor twice");
    }
}
