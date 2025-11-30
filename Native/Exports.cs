using System.Runtime.InteropServices;

namespace WhiteBinTools.Native;

public static class Exports
{
    static Exports()
    {   
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    }
    
    
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