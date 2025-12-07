using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WhiteBinTools.Support;
using WhiteBinTools.Unpack;

namespace WhiteBinTools.Native.Export;

public static class Unpack
{
     // ---------------------------------------------------------
    // 1. Metadata Binding
    // ---------------------------------------------------------
    [UnmanagedCallersOnly(EntryPoint = "get_file_metadata", CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe NativeStructs.FileEntryList GetFileMetadata(int gameCodeRaw, byte* filesPtr)
    {
        try
        {
            var filesPath = Marshal.PtrToStringUTF8((IntPtr)filesPtr);
            if (filesPath == null) 
            {
               Log.Error($"filesPath is null");
                return new NativeStructs.FileEntryList {Items = IntPtr.Zero, Count = 0};
            }

            var gameCode = (LibaryEnums.GameCodes)gameCodeRaw;
            var list = new List<NativeStructs.FileEntry>();
            Log.Info($"Getting file metadata for file: {filesPath}, game: {gameCode}");

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
            Log.Info($"Successfully extracted file {filesPath} metadata with size: {list.Count}");
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

    [UnmanagedCallersOnly(EntryPoint = "unpack_all", CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe int UnpackAll(int gameCodeRaw, byte* filesPtr, byte* whiteBinPtr)
    {
        try 
        {
            var files = Marshal.PtrToStringUTF8((IntPtr)filesPtr);
            var whiteBin = Marshal.PtrToStringUTF8((IntPtr)whiteBinPtr);
            if (files == null || whiteBin == null)
            {
                Log.Error($"Either fileList or whiteBin are null\nfileList: {files}, whiteBin: {whiteBin}");
                return Exports.InvalidArgsError;
            }
            var gameCode = (LibaryEnums.GameCodes)gameCodeRaw;
            Log.Info($"Unpacking All files from whiteBin: {whiteBin} game: {gameCode}");
            // Extracts every file (predicate returns true)
            UnpackProcesses.ExtractFiles(gameCode, files, whiteBin, _ => true);
            Log.Info($"Unpack complete for bin: {whiteBin}, game: {gameCode}");
            return Exports.SuccessReturn; // Success
        }
        catch (Exception ex)
        { 
            Log.Error($"Unpack failed with error: {ex.Message}");
            return Exports.ExceptionError; // Fail
        }
    }
    [UnmanagedCallersOnly(EntryPoint = "unpack_all_to_path" , CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe int UnpackAllToPath(int gameCodeRaw, byte* filesPtr, byte* whiteBinPtr, byte* outDirPtr)
    {
        try 
        {
            var files = Marshal.PtrToStringUTF8((IntPtr)filesPtr);
            var whiteBin = Marshal.PtrToStringUTF8((IntPtr)whiteBinPtr);
            var outDir  = Marshal.PtrToStringUTF8((IntPtr)outDirPtr);
            var gameCode = (LibaryEnums.GameCodes)gameCodeRaw;

            if (files == null || whiteBin == null || outDir == null)
            {
                Log.Error($"Either fileList, whiteBin or outDir are null\nfileList: {files}, whiteBin: {whiteBin}, outDir:  {outDir}");
                return Exports.InvalidArgsError;
            }
            
            //add the path
            if (!outDir.EndsWith('/'))
            {
                outDir += "/";
            }
            
            Log.Info($"Unpacking All files from whiteBin: {whiteBin} game: {gameCode} to path: {outDir}");
            // Extracts every file (predicate returns true)
            UnpackProcesses.ExtractFiles(gameCode, files, whiteBin, _ => true, outDir);
            Log.Info($"Unpack complete for bin: {whiteBin}, game: {gameCode} to path: {outDir}");
            return Exports.SuccessReturn; // Success
        }
        catch (Exception ex)
        { 
            Log.Error($"Unpack failed with error: {ex.Message}");
            return Exports.InvalidArgsError; // Fail
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "unpack_single" , CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe int UnpackSingle(int gameCodeRaw, byte* filesPtr, byte* whiteBinPtr, byte* targetPathPtr)
    {
        try
        {
            var files = Marshal.PtrToStringUTF8((IntPtr)filesPtr);
            var whiteBin = Marshal.PtrToStringUTF8((IntPtr)whiteBinPtr);
            var targetPath = Marshal.PtrToStringUTF8((IntPtr)targetPathPtr);
            if (files == null || whiteBin == null || targetPath == null)
            {
                Log.Error($"Either fileList, whiteBin, directory or targetPath is null." +
                                   $"\nfileList: {files}, whiteBin: {whiteBin}, targetPath: {targetPath}");
                return Exports.InvalidArgsError;
            }
            var gameCode = (LibaryEnums.GameCodes)gameCodeRaw;
            Log.Info($"Attempting to unpack {targetPath} from {whiteBin} game: {gameCode}...");

            // Extracts only if the path matches exactly
            UnpackProcesses.ExtractFiles(gameCode, files, whiteBin, (v) => v.MainPath == targetPath);
            Log.Info($"Unpack successful for {targetPath} from {whiteBin} game: {gameCode}...");
            return Exports.SuccessReturn;
        }
        catch (Exception ex)
        {
            Log.Error($"Unpack Single failed with error: {ex.Message}");
            return Exports.ExceptionError; 
        }
    }
    [UnmanagedCallersOnly(EntryPoint = "unpack_single_to_path" , CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe int UnpackSingleToPath(int gameCodeRaw, byte* filesPtr, byte* whiteBinPtr, byte* targetPathPtr, byte* outDirPtr)
    {
        try
        {
            var files = Marshal.PtrToStringUTF8((IntPtr)filesPtr);
            var whiteBin = Marshal.PtrToStringUTF8((IntPtr)whiteBinPtr);
            var targetPath = Marshal.PtrToStringUTF8((IntPtr)targetPathPtr);
            var outDir  = Marshal.PtrToStringUTF8((IntPtr)outDirPtr);
            var gameCode = (LibaryEnums.GameCodes)gameCodeRaw;
            
            if (files == null || whiteBin == null || targetPath == null || outDir == null)
            {
                Log.Error($"Either fileList, whiteBin, directory, targetPath or outDir is null." +
                                   $"\nfileList: {files}, whiteBin: {whiteBin}, targetPath: {targetPath}, outDir: {outDir}");
                return Exports.InvalidArgsError;
            }

            //add the path
            if (!outDir.EndsWith('/'))
            {
                outDir += "/";
            }

            
            Log.Info($"Attempting to unpack {targetPath} from {whiteBin} game: {gameCode}.to path: {outDir}...");

            // Extracts only if the path matches exactly
            UnpackProcesses.ExtractFiles(gameCode, files, whiteBin, (v) => v.MainPath == targetPath, outDir);
            Log.Info($"Unpack successful for {targetPath} from {whiteBin} game: {gameCode}...");
            return Exports.SuccessReturn;
        }
        catch (Exception ex)
        {
            Log.Error($"Unpack Single failed with error: {ex.Message}");
            return Exports.ExceptionError; 
        }
    }
    
    
    [UnmanagedCallersOnly(EntryPoint = "unpack_multiple", CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe int UnpackMultiple(int gameCodeRaw, byte* filesPtr, byte* whiteBinPtr, byte* directoryPtr)
    {
        try
        {
            var files = Marshal.PtrToStringUTF8((IntPtr)filesPtr);
            var whiteBin = Marshal.PtrToStringUTF8((IntPtr)whiteBinPtr);
            var targetDir = Marshal.PtrToStringUTF8((IntPtr)directoryPtr);
            if (files == null || whiteBin == null || targetDir == null)
            {
                Log.Error($"Either fileList, whiteBin, directory or targetDir is null." +
                                   $"\nfileList: {files}, whiteBin: {whiteBin}, targetDir: {targetDir}");
                return Exports.InvalidArgsError;
            }

            var gameCode = (LibaryEnums.GameCodes)gameCodeRaw;

            
            // Logic from UnpackTypeC: remove wildcard
            targetDir = targetDir.Replace("*", "");
            Log.Info($"Attempting to unpack {targetDir} game: {gameCode}...");
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
            Log.Info($"Unpack successful for path {targetDir} game: {gameCode}...");
            return Exports.SuccessReturn;
        }
        catch (Exception ex)
        {
            Log.Error("Unpack failed with error: " + ex.Message);
            return Exports.ExceptionError; 
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "unpack_multiple_to_path" , CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe int UnpackMultipleToPath(int gameCodeRaw, byte* filesPtr, byte* whiteBinPtr, byte* directoryPtr, byte* outDirPtr)
    {
        try
        {
            var files = Marshal.PtrToStringUTF8((IntPtr)filesPtr);
            var whiteBin = Marshal.PtrToStringUTF8((IntPtr)whiteBinPtr);
            var targetDir = Marshal.PtrToStringUTF8((IntPtr)directoryPtr);
            var outDir  = Marshal.PtrToStringUTF8((IntPtr)outDirPtr);
            var gameCode = (LibaryEnums.GameCodes)gameCodeRaw;

            if (files == null || whiteBin == null || targetDir == null || outDir == null)
            {
                Log.Error($"Either fileList, whiteBin, directory, targetDir or outDir is null." +
                                   $"\nfileList: {files}, whiteBin: {whiteBin}, targetDir: {targetDir}, out outDir: {outDir}");
                return Exports.InvalidArgsError;
            }
            //add the path
            if (!outDir.EndsWith('/'))
            {
                outDir += "/";
            }

            // Logic from UnpackTypeC: remove wildcard
            targetDir = targetDir.Replace("*", "");
            Log.Info($"Attempting to unpack pattern {targetDir} game: {gameCode} to path: {outDir}...");

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
            }, outDir);
            Log.Info($"Unpack successful for path {targetDir} game: {gameCode}...");
            return Exports.SuccessReturn;
        }
        catch (Exception ex)
        {
            Log.Error("Unpack failed with error: " + ex.Message);
            return Exports.ExceptionError; 
        }
    }
    
    // ---------------------------------------------------------
    // 3. Memory Management Helpers
    // ---------------------------------------------------------

    [UnmanagedCallersOnly(EntryPoint = "free_metadata" , CallConvs = [typeof(CallConvCdecl)])]
    public static void FreeMetadata(NativeStructs.FileEntryList list)
    {
        
        if (list.Items == IntPtr.Zero)
        {
            Log.Warn("Total items to clean are 0, skipping freeing memory...");
            return;
        }
        Log.Info($"Cleaning up memory for {list.Count} item(s)...");
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
        
        Log.Info($"Cleaning up memory for {list.Count} item(s) done");
        // Free the array block itself
        Marshal.FreeCoTaskMem(list.Items);
    }
}