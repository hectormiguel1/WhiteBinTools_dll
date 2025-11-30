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
    public static unsafe NativeStructs.FileEntryList GetFileMetadata(int gameCodeRaw, byte* filesPtr)
    {
        try
        {
            var filesPath = Marshal.PtrToStringUTF8((IntPtr)filesPtr);
            if (filesPath == null) 
            {
               NativeLogger.Error($"filesPath is null");
                return new NativeStructs.FileEntryList {Items = IntPtr.Zero, Count = 0};
            }

            var gameCode = (LibaryEnums.GameCodes)gameCodeRaw;
            var list = new List<NativeStructs.FileEntry>();
            NativeLogger.Debug($"Getting file metadata for file: {filesPath}, game: {gameCode}");

            // Initialize the filelist reader
            var vars = UnpackProcesses.InitializeFilelist(gameCode, filesPath);

            // Iterate through all entries and collect data
            UnpackProcesses.IterateEntries(gameCode, vars, _ =>
            {
                var entry = new NativeStructs.FileEntry
                {
                    FileCode = vars.FileCode,
                    FileTypeId = (gameCode == LibaryEnums.GameCodes.Ff132) ? vars.FileTypeId : 0,
                    // ff131 uses 'ChunkNumber', ff132 uses 'CurrentChunkNumber'
                    ChunkIndex = (gameCode == LibaryEnums.GameCodes.Ff131) ? vars.ChunkNumber : vars.CurrentChunkNumber,
                    // Allocate unmanaged memory for the string so it survives until FreeMetadata is called
                    FilePath = Marshal.StringToCoTaskMemUTF8(vars.PathString)
                };
                list.Add(entry);
            });

            return Exports.MarshalListToArray(list);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WBT Native Error] GetFileMetadata: {ex.Message}. Stacktrace: {ex.StackTrace} {ex.Data} {ex.ToString()}");
            return new NativeStructs.FileEntryList { Items = IntPtr.Zero, Count = 0 };
        }
    }
    // ---------------------------------------------------------
    // 2. Action Bindings
    // ---------------------------------------------------------

    [UnmanagedCallersOnly(EntryPoint = "unpack_all")]
    public static unsafe int UnpackAll(int gameCodeRaw, byte* filesPtr, byte* whiteBinPtr)
    {
        try 
        {
            var files = Marshal.PtrToStringUTF8((IntPtr)filesPtr);
            var whiteBin = Marshal.PtrToStringUTF8((IntPtr)whiteBinPtr);
            if (files == null || whiteBin == null)
            {
                NativeLogger.Error($"Either fileList or whiteBin are null\nfileList: {files}, whiteBin: {whiteBin}");
                return -1;
            }
            var gameCode = (LibaryEnums.GameCodes)gameCodeRaw;
            NativeLogger.Debug($"Unpacking All files from whiteBin: {whiteBin} game: {gameCode}");
            // Extracts every file (predicate returns true)
            UnpackProcesses.ExtractFiles(gameCode, files, whiteBin, _ => true);
            NativeLogger.Info($"Unpack complete for bin: {whiteBin}, game: {gameCode}");
            return 0; // Success
        }
        catch (Exception ex)
        { 
            NativeLogger.Error($"Unpack failed with error: {ex.Message}");
            return 1; // Fail
        }
    }
    [UnmanagedCallersOnly(EntryPoint = "unpack_all_to_path")]
    public static unsafe int UnpackAllToPath(int gameCodeRaw, byte* filesPtr, byte* whiteBinPtr, byte* outDirPtr)
    {
        try 
        {
            var files = Marshal.PtrToStringUTF8((IntPtr)filesPtr);
            var whiteBin = Marshal.PtrToStringUTF8((IntPtr)whiteBinPtr);
            var outDir  = Marshal.PtrToStringUTF8((IntPtr)outDirPtr);
            if (files == null || whiteBin == null || outDir == null)
            {
                NativeLogger.Error($"Either fileList, whiteBin or outDir are null\nfileList: {files}, whiteBin: {whiteBin}, outDir:  {outDir}");
                return -1;
            }
            var gameCode = (LibaryEnums.GameCodes)gameCodeRaw;
            NativeLogger.Debug($"Unpacking All files from whiteBin: {whiteBin} game: {gameCode} to path: {outDir}");
            // Extracts every file (predicate returns true)
            UnpackProcesses.ExtractFiles(gameCode, files, whiteBin, _ => true, outDir);
            NativeLogger.Info($"Unpack complete for bin: {whiteBin}, game: {gameCode} to path: {outDir}");
            return 0; // Success
        }
        catch (Exception ex)
        { 
            NativeLogger.Error($"Unpack failed with error: {ex.Message}");
            return 1; // Fail
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "unpack_single")]
    public static unsafe int UnpackSingle(int gameCodeRaw, byte* filesPtr, byte* whiteBinPtr, byte* targetPathPtr)
    {
        try
        {
            var files = Marshal.PtrToStringUTF8((IntPtr)filesPtr);
            var whiteBin = Marshal.PtrToStringUTF8((IntPtr)whiteBinPtr);
            var targetPath = Marshal.PtrToStringUTF8((IntPtr)targetPathPtr);
            if (files == null || whiteBin == null || targetPath == null)
            {
                NativeLogger.Error($"Either fileList, whiteBin, directory or targetPath is null." +
                                   $"\nfileList: {files}, whiteBin: {whiteBin}, targetPath: {targetPath}");
                return -1;
            }
            var gameCode = (LibaryEnums.GameCodes)gameCodeRaw;
            NativeLogger.Debug($"Attempting to unpack {targetPath} from {whiteBin} game: {gameCode}...");

            // Extracts only if the path matches exactly
            UnpackProcesses.ExtractFiles(gameCode, files, whiteBin, (v) => v.MainPath == targetPath);
            NativeLogger.Debug($"Unpack successful for {targetPath} from {whiteBin} game: {gameCode}...");
            return 0;
        }
        catch (Exception ex)
        {
            NativeLogger.Error($"Unpack Single failed with error: {ex.Message}");
            return 1; 
        }
    }
    
    [UnmanagedCallersOnly(EntryPoint = "unpack_multiple")]
    public static unsafe int UnpackMultiple(int gameCodeRaw, byte* filesPtr, byte* whiteBinPtr, byte* directoryPtr)
    {
        try
        {
            var files = Marshal.PtrToStringUTF8((IntPtr)filesPtr);
            var whiteBin = Marshal.PtrToStringUTF8((IntPtr)whiteBinPtr);
            var targetDir = Marshal.PtrToStringUTF8((IntPtr)directoryPtr);
            if (files == null || whiteBin == null || targetDir == null)
            {
                NativeLogger.Error($"Either fileList, whiteBin, directory or targetDir is null." +
                                   $"\nfileList: {files}, whiteBin: {whiteBin}, targetDir: {targetDir}");
                return -1;
            }

            var gameCode = (LibaryEnums.GameCodes)gameCodeRaw;

            // Logic from UnpackTypeC: remove wildcard
            targetDir = targetDir.Replace("*", "");

            // Extracts if the path starts with the directory
            UnpackProcesses.ExtractFiles(gameCode, files, whiteBin, (v) => 
            {
                // Replicate the iterative directory matching logic from UnpackTypeC
                // using the platform-specific separator to ensure Linux compatibility
                var currentPathDataArray = v.MainPath.Split(Path.DirectorySeparatorChar);
                var assembledDir = string.Empty;

                foreach (var dir in currentPathDataArray)
                {
                    assembledDir += dir + Path.DirectorySeparatorChar;
                    if (assembledDir == targetDir) break;
                }

                return assembledDir == targetDir;
            });
            return 0;
        }
        catch (Exception ex)
        {
            NativeLogger.Error("Unpack failed with error: " + ex.Message);
            return -1; 
        }
    }

    
    // ---------------------------------------------------------
    // 3. Memory Management Helpers
    // ---------------------------------------------------------

    [UnmanagedCallersOnly(EntryPoint = "free_metadata")]
    public static void FreeMetadata(NativeStructs.FileEntryList list)
    {
        NativeLogger.Debug($"Cleaning up memory for {list.Items} item(s)...");
        if (list.Items == IntPtr.Zero)
        {
            NativeLogger.Warn("Total items to clean are 0, skipping freeing memory...");
            return;
        }
        
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
        
        NativeLogger.Debug($"Cleaning up memory for {list.Items} item(s) done");
        // Free the array block itself
        Marshal.FreeCoTaskMem(list.Items);
    }
}