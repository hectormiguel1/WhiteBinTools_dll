using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WhiteBinTools.Native;

public static class Exports
{
    [ModuleInitializer]
    public static void Init()
    {   
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    }
    public const int SuccessReturn = 0;
    public const int ExceptionError = 1;
    public const int InvalidArgsError = -1;
    public const int FileNotFoundError = -2;
    
    
    public static NativeStructs.FileEntryList MarshalListToArray(List<NativeStructs.FileEntry> list)
    {
        if (list.Count == 0) return new NativeStructs.FileEntryList { Items = IntPtr.Zero, Count = 0 };

        var sizeOfEntry = Marshal.SizeOf<NativeStructs.FileEntry>();
        var totalBytes = sizeOfEntry * list.Count;
        
        var ptr = Marshal.AllocCoTaskMem(totalBytes);

        for (var i = 0; i < list.Count; i++)
        {
            var currentPos = ptr + (i * sizeOfEntry);
            Marshal.StructureToPtr(list[i], currentPos, false);
        }

        return new NativeStructs.FileEntryList { Items = ptr, Count = list.Count };
    }
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void LoggerCallback(IntPtr msgPtr);


    /// <summary>
    /// Registers a callback function for logging.
    /// C Signature: void set_logging_callback(void (*callback)(const char*));
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "set_logging_callback", CallConvs = [typeof(CallConvCdecl)])]
    public static void SetLoggingCallback(IntPtr callbackPtr)
    {
        if (callbackPtr == IntPtr.Zero)
        {
            NativeLogger.LoggingCallback = Console.WriteLine;
            return;
        }

        var nativeCallback = Marshal.GetDelegateForFunctionPointer<LoggerCallback>(callbackPtr);

        NativeLogger.LoggingCallback = (msg) =>
        {
            // ALLOCATE: Create a UTF-8 copy on the Heap (Unmanaged Memory).
            // This memory persists until explicitly freed.
            var ptr = Marshal.StringToCoTaskMemUTF8(msg);

            // CALL: Pass the pointer to Dart. 
            // Since it's heap memory, it's safe even if Dart processes it asynchronously.
            nativeCallback(ptr);
        };
    }
    
    // 3. The Cleanup Function (CRITICAL NEW EXPORT)
    // Dart must call this after it reads the string.
    [UnmanagedCallersOnly(EntryPoint = "free_log_memory", CallConvs = [typeof(CallConvCdecl)])]
    public static void FreeLogMemory(IntPtr ptr)
    {
        if (ptr != IntPtr.Zero)
        {
            Marshal.FreeCoTaskMem(ptr);
        }
    }
}