using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WhiteBinTools.Support;
using WhiteBinTools.Unpack;
using Native.Common;

namespace WhiteBinTools.Native.Export;

public static class Unpack
{
     // ---------------------------------------------------------
    // 1. Metadata Binding
    // ---------------------------------------------------------
    [UnmanagedCallersOnly(EntryPoint = "get_file_metadata", CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe NativeResult.Result<NativeStructs.FileEntryList> GetFileMetadata(int gameCodeRaw, byte* filesPtr)
    {
        try
        {
            var filesPath = NativeResult.StringFromPtr(filesPtr);
            if (string.IsNullOrEmpty(filesPath))
            {
                var msg = $"filesPath is null or empty. Got value: {filesPath}";
                Log.Fatal(msg);
                return NativeResult.CreateError<NativeStructs.FileEntryList>(msg, Exports.InvalidArgsError);
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
                    FilePath = (byte*)Marshal.StringToCoTaskMemUTF8(vars.PathString)
                };
                list.Add(entry);
            });
            Log.Info($"Successfully extracted file {filesPath} metadata with size: {list.Count}");
            // Use Export helper to wrap list in Result
            return NativeResult.CreateSuccess(Exports.MarshalListToArray(list));
        }
        catch (Exception ex)
        {
            var msg = $"[WBT Native Error] GetFileMetadata: {ex.Message}";
            Log.Fatal($"{msg}. Stacktrace: {ex.StackTrace}");
            return NativeResult.CreateError<NativeStructs.FileEntryList>(msg, Exports.ExceptionError);
        }
    }
    // ---------------------------------------------------------
    // 2. Action Bindings
    // ---------------------------------------------------------

    [UnmanagedCallersOnly(EntryPoint = "unpack_all", CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe NativeResult.Result<int> UnpackAll(int gameCodeRaw, byte* filesPtr, byte* whiteBinPtr)
    {
        try 
        {
            var files = NativeResult.StringFromPtr(filesPtr);
            var whiteBin = NativeResult.StringFromPtr(whiteBinPtr);
            if (string.IsNullOrEmpty(files) || string.IsNullOrEmpty(whiteBin))
            {
                var msg = $"Either fileList or whiteBin are null\nfileList: {files}, whiteBin: {whiteBin}";
                Log.Fatal(msg);
                return NativeResult.CreateError<int>(msg, Exports.InvalidArgsError);
            }
            var gameCode = (LibaryEnums.GameCodes)gameCodeRaw;
            Log.Info($"Unpacking All files from whiteBin: {whiteBin} game: {gameCode}");
            // Extracts every file (predicate returns true)
            UnpackProcesses.ExtractFiles(gameCode, files, whiteBin, _ => true);
            Log.Info($"Unpack complete for bin: {whiteBin}, game: {gameCode}");
            return NativeResult.CreateInlineSuccess(Exports.SuccessReturn);
        }
        catch (Exception ex)
        {
            Log.Fatal($"Unpack failed with error: {ex.Message}");
            return NativeResult.CreateError<int>(ex.Message, Exports.ExceptionError);
        }
    }
    [UnmanagedCallersOnly(EntryPoint = "unpack_all_to_path" , CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe NativeResult.Result<int> UnpackAllToPath(int gameCodeRaw, byte* filesPtr, byte* whiteBinPtr, byte* outDirPtr)
    {
        try 
        {
            var files = NativeResult.StringFromPtr(filesPtr);
            var whiteBin = NativeResult.StringFromPtr(whiteBinPtr);
            var outDir  = NativeResult.StringFromPtr(outDirPtr);
            var gameCode = (LibaryEnums.GameCodes)gameCodeRaw;

            if ( string.IsNullOrEmpty(files) || string.IsNullOrEmpty(whiteBin) || string.IsNullOrEmpty(outDir))
            {
                var msg = $"Either fileList, whiteBin or outDir are null\nfileList: {files}, whiteBin: {whiteBin}, outDir:  {outDir}";
                Log.Fatal(msg);
                return NativeResult.CreateError<int>(msg, Exports.InvalidArgsError);
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
            return NativeResult.CreateInlineSuccess(Exports.SuccessReturn);
        }
        catch (Exception ex)
        {
            Log.Fatal($"Unpack failed with error: {ex.Message}");
            return NativeResult.CreateError<int>(ex.Message, Exports.ExceptionError);
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "unpack_single" , CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe NativeResult.Result<int> UnpackSingle(int gameCodeRaw, byte* filesPtr, byte* whiteBinPtr, byte* targetPathPtr)
    {
        try
        {
            var files = NativeResult.StringFromPtr(filesPtr);
            var whiteBin = NativeResult.StringFromPtr(whiteBinPtr);
            var targetPath = NativeResult.StringFromPtr(targetPathPtr);
            if (string.IsNullOrEmpty(files) || string.IsNullOrEmpty(whiteBin) || string.IsNullOrEmpty(targetPath))
            {
                var msg = $"Either fileList, whiteBin, directory or targetPath is null."
                          +$"\nfileList: {files}, whiteBin: {whiteBin}, targetPath: {targetPath}";
                Log.Fatal(msg);
                return NativeResult.CreateError<int>(msg, Exports.InvalidArgsError);
            }
            var gameCode = (LibaryEnums.GameCodes)gameCodeRaw;
            Log.Info($"Attempting to unpack {targetPath} from {whiteBin} game: {gameCode}...");

            // Extracts only if the path matches exactly
            UnpackProcesses.ExtractFiles(gameCode, files, whiteBin, (v) => v.MainPath == targetPath);
            Log.Info($"Unpack successful for {targetPath} from {whiteBin} game: {gameCode}...");
            return NativeResult.CreateInlineSuccess(Exports.SuccessReturn);
        }
        catch (Exception ex)
        {
            Log.Fatal($"Unpack Single failed with error: {ex.Message}");
            return NativeResult.CreateError<int>(ex.Message, Exports.ExceptionError);
        }
    }
    [UnmanagedCallersOnly(EntryPoint = "unpack_single_to_path" , CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe NativeResult.Result<int> UnpackSingleToPath(int gameCodeRaw, byte* filesPtr, byte* whiteBinPtr, byte* targetPathPtr, byte* outDirPtr)
    {
        try
        {
            var files = NativeResult.StringFromPtr(filesPtr);
            var whiteBin = NativeResult.StringFromPtr(whiteBinPtr);
            var targetPath = NativeResult.StringFromPtr(targetPathPtr);
            var outDir  = NativeResult.StringFromPtr(outDirPtr);
            var gameCode = (LibaryEnums.GameCodes)gameCodeRaw;
            
            if (string.IsNullOrEmpty(files) || string.IsNullOrEmpty(whiteBin) || string.IsNullOrEmpty(targetPath) || string.IsNullOrEmpty(outDir))
            {
                var msg = $"Either fileList, whiteBin, directory, targetPath or outDir is null."
                          +"\nfileList: {files}, whiteBin: {whiteBin}, targetPath: {targetPath}, outDir: {outDir}";
                Log.Fatal(msg);
                return NativeResult.CreateError<int>(msg, Exports.InvalidArgsError);
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
            return NativeResult.CreateInlineSuccess(Exports.SuccessReturn);
        }
        catch (Exception ex)
        {
            Log.Fatal($"Unpack Single failed with error: {ex.Message}");
            return NativeResult.CreateError<int>(ex.Message, Exports.ExceptionError);
        }
    }
    
    
    [UnmanagedCallersOnly(EntryPoint = "unpack_multiple", CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe NativeResult.Result<int> UnpackMultiple(int gameCodeRaw, byte* filesPtr, byte* whiteBinPtr, byte* directoryPtr)
    {
        try
        {
            var files = NativeResult.StringFromPtr(filesPtr);
            var whiteBin = NativeResult.StringFromPtr(whiteBinPtr);
            var targetDir = NativeResult.StringFromPtr(directoryPtr);
            if (string.IsNullOrEmpty(files) || string.IsNullOrEmpty(whiteBin) || string.IsNullOrEmpty(targetDir))
            {
                var msg = $"Either fileList, whiteBin, directory or targetDir is null."
                          +"\nfileList: {files}, whiteBin: {whiteBin}, targetDir: {targetDir}";
                Log.Fatal(msg);
                return NativeResult.CreateError<int>(msg, Exports.InvalidArgsError);
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
            return NativeResult.CreateInlineSuccess(Exports.SuccessReturn);
        }
        catch (Exception ex)
        {
            Log.Fatal("Unpack failed with error: " + ex.Message);
            return NativeResult.CreateError<int>(ex.Message, Exports.ExceptionError);
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "unpack_multiple_to_path" , CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe NativeResult.Result<int> UnpackMultipleToPath(int gameCodeRaw, byte* filesPtr, byte* whiteBinPtr, byte* directoryPtr, byte* outDirPtr)
    {
        try
        {
            var files = NativeResult.StringFromPtr(filesPtr);
            var whiteBin = NativeResult.StringFromPtr(whiteBinPtr);
            var targetDir = NativeResult.StringFromPtr(directoryPtr);
            var outDir  = NativeResult.StringFromPtr(outDirPtr);
            var gameCode = (LibaryEnums.GameCodes)gameCodeRaw;

            if (string.IsNullOrEmpty(files) || string.IsNullOrEmpty(whiteBin) || string.IsNullOrEmpty(targetDir) || string.IsNullOrEmpty(outDir))
            {
                var msg = $"Either fileList, whiteBin, directory, targetDir or outDir is null."
                          +"\nfileList: {files}, whiteBin: {whiteBin}, targetDir: {targetDir}, out outDir: {outDir}";
                Log.Fatal(msg);
                return NativeResult.CreateError<int>(msg, Exports.InvalidArgsError);
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
            return NativeResult.CreateInlineSuccess(Exports.SuccessReturn);
        }
        catch (Exception ex)
        {
            Log.Fatal("Unpack failed with error: " + ex.Message);
            return NativeResult.CreateError<int>(ex.Message, Exports.ExceptionError);
        }
    }
}
