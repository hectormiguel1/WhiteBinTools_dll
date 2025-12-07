using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WhiteBinTools.Repack;
using WhiteBinTools.Support;

namespace WhiteBinTools.Native.Export;

public static class Repack
{
    // =======================================================================
    // Helpers
    // =======================================================================

    /// <summary>
    /// safe conversion from C-String (char*) to .NET String.
    /// Returns null if pointer is null.
    /// </summary>
    private static string? PtrToString(IntPtr ptr)
    {
        return Marshal.PtrToStringUTF8(ptr);
    }

    // =======================================================================
    // Native API Exports
    // Return Codes: 0 = Success, 1 = Exception/Fail, -1 = Invalid Arguments
    // =======================================================================

    [UnmanagedCallersOnly(EntryPoint = "repack_all", CallConvs = [typeof(CallConvCdecl)])]
    public static int RepackAll(int gameCode, IntPtr filesPtr, IntPtr extractedDir, byte backup)
    {
        try
        {
            var fFile = PtrToString(filesPtr);
            var eDir = PtrToString(extractedDir);
            var shouldBackup = backup != 0;
            if (string.IsNullOrEmpty(fFile) || string.IsNullOrEmpty(eDir))
            {
                Log.Error(
                    $"Either files or extractDir are null.\nfiles: {fFile}, extractedDir: {eDir}, backup: {shouldBackup}");
                return Exports.InvalidArgsError;
            }

            Log.Info(
                $"Repacking all files fileList: {fFile}, extractedDir: {eDir}, Game: {(LibaryEnums.GameCodes)gameCode}, backup: {shouldBackup}");
            RepackActions.RepackAll(
                (LibaryEnums.GameCodes)gameCode,
                fFile,
                eDir,
                shouldBackup
            );
            Log.Info($"Repack for {eDir} completed successfully!");
            return Exports.SuccessReturn;
        }
        catch (Exception ex)
        {
            Log.Error($"Repack failed with error: {ex.Message}");
            return Exports.ExceptionError;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "repack_single", CallConvs = [typeof(CallConvCdecl)])]
    public static int RepackSingle(int gameCode, IntPtr fileListPtr, IntPtr whiteBinPtr, IntPtr targetFilePtr,
        byte backup)
    {
        try
        {
            var fFile = PtrToString(fileListPtr);
            var wFile = PtrToString(whiteBinPtr);
            var tPath = PtrToString(targetFilePtr);
            var shouldBackup = backup != 0;
            var game = (LibaryEnums.GameCodes)gameCode;
            if (string.IsNullOrEmpty(fFile) || string.IsNullOrEmpty(wFile) || string.IsNullOrEmpty(tPath))
            {
                Log.Error($"Either fileListPtr, whiteBinPtr or targetFilePtr are null.\n" +
                                   $"fileListPtr: {fFile}, whiteBinPtr: {wFile}, targetFilePtr: {tPath}");
                return Exports.InvalidArgsError;
            }

            Log.Info($"Repacking single file: {tPath} for game: {game}, backup: {shouldBackup}");
            RepackActions.RepackSingle(
                game,
                fFile,
                wFile,
                tPath,
                shouldBackup
            );
            Log.Info($"Repack for {tPath} completed successfully!");
            return Exports.SuccessReturn;
        }
        catch (Exception ex)
        {
            Log.Error($"Repack Failed with error: {ex.Message}");
            return Exports.ExceptionError;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "repack_multiple", CallConvs = [typeof(CallConvCdecl)])]
    public static int RepackMultiple(int gameCode, IntPtr fileListPtr, IntPtr whiteBinPtr, IntPtr extractedDirPtr,
        byte backup)
    {
        try
        {
            var fFile = PtrToString(fileListPtr);
            var wFile = PtrToString(whiteBinPtr);
            var eDir = PtrToString(extractedDirPtr);
            var game = (LibaryEnums.GameCodes)gameCode;
            var shouldBackup = backup != 0;

            if (string.IsNullOrEmpty(fFile) || string.IsNullOrEmpty(wFile) || string.IsNullOrEmpty(eDir))
            {
                Log.Error($"Invalid arguments! Either fileListPtr, whiteBinPtr or extractedDirPtr are null." +
                                   $"\nfileListPtr: {fFile},  whiteBinPtr: {wFile}, extractedDirPtr: {eDir}");
                return Exports.InvalidArgsError;
            }

            Log.Info($"Repacking multiple files : {eDir} for game: {game}, backup: {shouldBackup}");
            RepackActions.RepackMultiple(
                game,
                fFile,
                wFile,
                eDir,
                shouldBackup
            );
            Log.Info($"Repack for {eDir} completed successfully!");
            return Exports.SuccessReturn;
        }
        catch (Exception ex)
        {
            Log.Error($"Repack Failed with error: {ex.Message}");
            return Exports.ExceptionError;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "repack_filelist_from_chunks", CallConvs = [typeof(CallConvCdecl)])]
    public static int RepackFileListFromChunks(int gameCode, IntPtr extractedFilelistDir, byte backup)
    {
        try
        {
            var eDir = PtrToString(extractedFilelistDir);
            var shouldBackup = backup != 0;
            var game = (LibaryEnums.GameCodes)gameCode;
            if (string.IsNullOrEmpty(eDir))
            {
                Log.Error("Invalid Arguments! extractedFilelistDir is null or empty." +
                                   $"\neDir: {eDir}");
                return Exports.InvalidArgsError;
            }
            
            Log.Info($"Repacking single file: {eDir} for game: {game}, backup: {shouldBackup}");
            RepackActions.RepackFilelistFromChunks(
                game,
                eDir,
                shouldBackup
            );
            Log.Info($"Repacked filelist: {eDir} successfully!");
            return Exports.SuccessReturn;
        }
        catch (Exception ex)
        {
            Log.Error($"Repack Failed with error: {ex.Message}");
            return Exports.ExceptionError;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "repack_filelist_from_json", CallConvs = [typeof(CallConvCdecl)])]
    public static int RepackFileListFromJson(int gameCode, IntPtr jsonFile, byte backup)
    {
        try
        {
            var jFile = PtrToString(jsonFile);
            var game =  (LibaryEnums.GameCodes)gameCode;
            var shouldBackup = backup != 0;
            if (string.IsNullOrEmpty(jFile))
            {
                Log.Error("Invalid Arguments! jsonFile is null or empty." +
                                   $"\njsonFile: {jFile}");
                return Exports.InvalidArgsError;
            }
            Log.Info($"Repacking single file: {jFile} for game: {game}, backup: {shouldBackup}");
            RepackActions.RepackFilelistFromJson(
                game,
                jFile,
                shouldBackup
            );
            Log.Info($"Repacked filelist: {jFile} successfully!");
            return Exports.SuccessReturn;
        }
        catch (Exception ex)
        {
            Log.Error($"Repack Failed with error: {ex.Message}");
            return Exports.ExceptionError;
        }
    }
}