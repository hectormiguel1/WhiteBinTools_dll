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
}