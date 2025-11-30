using System.Runtime.InteropServices;
using WhiteBinTools.Support;
using WhiteBinTools.Unpack;

namespace WhiteBinTools.Native.Export;

public static class Unpack
{
     // ---------------------------------------------------------
    // 1. Metadata Binding
    // ---------------------------------------------------------
    [UnmanagedCallersOnly(EntryPoint = "get_file_metadata")]
    public static unsafe NativeStructs.FileEntryList GetFileMetadata(int gameCodeRaw, byte* filelistPathPtr)
    {
        try
        {
            var filelistPath = Marshal.PtrToStringUTF8((IntPtr)filelistPathPtr);
            var gameCode = (LibaryEnums.GameCodes)gameCodeRaw;
            var list = new List<NativeStructs.FileEntry>();

            // Initialize the filelist reader
            var vars = UnpackProcesses.InitializeFilelist(gameCode, filelistPath);

            // Iterate through all entries and collect data
            UnpackProcesses.IterateEntries(gameCode, vars, index =>
            {
                var entry = new NativeStructs.FileEntry
                {
                    FileCode = vars.FileCode,
                    FileTypeId = (gameCode == LibaryEnums.GameCodes.ff132) ? vars.FileTypeID : 0,
                    // ff131 uses 'ChunkNumber', ff132 uses 'CurrentChunkNumber'
                    ChunkIndex = (gameCode == LibaryEnums.GameCodes.ff131) ? vars.ChunkNumber : vars.CurrentChunkNumber,
                    // Allocate unmanaged memory for the string so it survives until FreeMetadata is called
                    FilePath = Marshal.StringToCoTaskMemUTF8(vars.PathString)
                };
                list.Add(entry);
            });

            return Exports.MarshalListToArray(list);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[C# Error] GetFileMetadata: {ex.Message}");
            return new NativeStructs.FileEntryList { Items = IntPtr.Zero, Count = 0 };
        }
    }
    // ---------------------------------------------------------
    // 2. Action Bindings
    // ---------------------------------------------------------

    [UnmanagedCallersOnly(EntryPoint = "unpack_all")]
    public static unsafe int UnpackAll(int gameCodeRaw, byte* filelistPtr, byte* whiteBinPtr)
    {
        try 
        {
            var filelist = Marshal.PtrToStringUTF8((IntPtr)filelistPtr);
            var whiteBin = Marshal.PtrToStringUTF8((IntPtr)whiteBinPtr);
            var gameCode = (LibaryEnums.GameCodes)gameCodeRaw;
            
            // Extracts every file (predicate returns true)
            UnpackProcesses.ExtractFiles(gameCode, filelist, whiteBin, (v) => true);
            return 0; // Success
        }
        catch (Exception ex)
        { 
            Console.WriteLine($"[C# Error] UnpackAll: {ex.Message}");
            return -1; // Fail
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "unpack_single")]
    public static unsafe int UnpackSingle(int gameCodeRaw, byte* filelistPtr, byte* whiteBinPtr, byte* targetPathPtr)
    {
        try
        {
            var filelist = Marshal.PtrToStringUTF8((IntPtr)filelistPtr);
            var whiteBin = Marshal.PtrToStringUTF8((IntPtr)whiteBinPtr);
            var targetPath = Marshal.PtrToStringUTF8((IntPtr)targetPathPtr);
            var gameCode = (LibaryEnums.GameCodes)gameCodeRaw;

            // Extracts only if the path matches exactly
            UnpackProcesses.ExtractFiles(gameCode, filelist, whiteBin, (v) => v.MainPath == targetPath);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[C# Error] UnpackSingle: {ex.Message}");
            return -1; 
        }
    }
    
    [UnmanagedCallersOnly(EntryPoint = "unpack_multiple")]
    public static unsafe int UnpackMultiple(int gameCodeRaw, byte* filelistPtr, byte* whiteBinPtr, byte* directoryPtr)
    {
        try
        {
            var filelist = Marshal.PtrToStringUTF8((IntPtr)filelistPtr);
            var whiteBin = Marshal.PtrToStringUTF8((IntPtr)whiteBinPtr);
            var targetDir = Marshal.PtrToStringUTF8((IntPtr)directoryPtr);
            var gameCode = (LibaryEnums.GameCodes)gameCodeRaw;

            // Logic from UnpackTypeC: remove wildcard
            targetDir = targetDir.Replace("*", "");

            // Extracts if the path starts with the directory
            UnpackProcesses.ExtractFiles(gameCode, filelist, whiteBin, (v) => 
            {
                // Replicate the iterative directory matching logic from UnpackTypeC
                // using the platform-specific separator to ensure Linux compatibility
                var currentPathDataArray = v.MainPath.Split(System.IO.Path.DirectorySeparatorChar);
                var assembledDir = string.Empty;

                foreach (var dir in currentPathDataArray)
                {
                    assembledDir += dir + System.IO.Path.DirectorySeparatorChar;
                    if (assembledDir == targetDir) break;
                }

                return assembledDir == targetDir;
            });
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[C# Error] UnpackMultiple: {ex.Message}");
            return -1; 
        }
    }

    
    // ---------------------------------------------------------
    // 3. Memory Management Helpers
    // ---------------------------------------------------------

    [UnmanagedCallersOnly(EntryPoint = "free_metadata")]
    public static void FreeMetadata(NativeStructs.FileEntryList list)
    {
        if (list.Items == IntPtr.Zero) return;

        var sizeOfEntry = Marshal.SizeOf<NativeStructs.FileEntry>();
        
        // Loop through the array to free the individual string pointers
        for (var i = 0; i < list.Count; i++)
        {
            var currentPos = list.Items + (i * sizeOfEntry);
            var entry = Marshal.PtrToStructure<NativeStructs.FileEntry>(currentPos);
            
            if (entry.FilePath != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(entry.FilePath);
            }
        }

        // Free the array block itself
        Marshal.FreeCoTaskMem(list.Items);
    }
}