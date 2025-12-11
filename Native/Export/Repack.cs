using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Native.Common;
using WhiteBinTools.Repack;
using WhiteBinTools.Support;

namespace WhiteBinTools.Native.Export;

public static class Repack
{

    
    // =======================================================================
    // Native API Exports
    // Return Codes: 0 = Success, 1 = Exception/Fail, -1 = Invalid Arguments
    // =======================================================================

    [UnmanagedCallersOnly(EntryPoint = "repack_all", CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe NativeResult.Result<int> RepackAll(int gameCode, byte* filesPtr, byte* extractedDir, byte backup)
    {
        try
        {
            if (filesPtr == null || extractedDir == null) 
            {
                const string msg = "Either files or extractDir are null.";
                Log.Fatal(msg);
                return NativeResult.CreateError<int>(msg, Exports.InvalidArgsError);
            }

            var fFile = NativeResult.StringFromPtr(filesPtr);
            var eDir = NativeResult.StringFromPtr(extractedDir);
            var shouldBackup = backup != 0;
            

            Log.Info(
                $"Repacking all files fileList: {fFile}, extractedDir: {eDir}, Game: {(LibaryEnums.GameCodes)gameCode}, backup: {shouldBackup}");
            RepackActions.RepackAll(
                (LibaryEnums.GameCodes)gameCode,
                fFile,
                eDir,
                shouldBackup
            );
            Log.Info($"Repack for {eDir} completed successfully!");
            return NativeResult.CreateInlineSuccess(Exports.SuccessReturn);
        }
        catch (Exception ex)
        {
            Log.Fatal($"Repack failed with error: {ex.Message}");
            return NativeResult.CreateError<int>(ex.Message, Exports.ExceptionError);
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "repack_single", CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe NativeResult.Result<int> RepackSingle(int gameCode, byte* fileListPtr, byte* whiteBinPtr, byte* targetFilePtr,
        byte backup)
    {
        try
        {
            var fFile = NativeResult.StringFromPtr(fileListPtr);
            var wFile = NativeResult.StringFromPtr(whiteBinPtr);
            var tPath = NativeResult.StringFromPtr(targetFilePtr);
            var shouldBackup = backup != 0;
            var game = (LibaryEnums.GameCodes)gameCode;
            
            if (string.IsNullOrEmpty(fFile) || string.IsNullOrEmpty(wFile) || string.IsNullOrEmpty(tPath))
            {
                var msg = $"Either fileListPtr, whiteBinPtr or targetFilePtr are null.\n" +
                          $"fileListPtr: {fFile}, whiteBinPtr: {wFile}, targetFilePtr: {tPath}";
                Log.Fatal(msg);
                return NativeResult.CreateError<int>(msg, Exports.InvalidArgsError);
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
            return NativeResult.CreateInlineSuccess(Exports.SuccessReturn);
        }
        catch (Exception ex)
        {
            Log.Fatal($"Repack Failed with error: {ex.Message}");
            return NativeResult.CreateError<int>(ex.Message, Exports.ExceptionError);
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "repack_multiple", CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe NativeResult.Result<int> RepackMultiple(int gameCode, byte* fileListPtr, byte* whiteBinPtr, byte* extractedDirPtr,
        byte backup)
    {
        try
        {
            var fFile = NativeResult.StringFromPtr(fileListPtr);
            var wFile = NativeResult.StringFromPtr(whiteBinPtr);
            var eDir = NativeResult.StringFromPtr(extractedDirPtr);
            var game = (LibaryEnums.GameCodes)gameCode;
            var shouldBackup = backup != 0;

            if (string.IsNullOrEmpty(fFile) || string.IsNullOrEmpty(wFile) || string.IsNullOrEmpty(eDir))
            {
                var msg = $"Invalid arguments! Either fileListPtr, whiteBinPtr or extractedDirPtr are null."
                          + $"\nfileListPtr: {fFile},  whiteBinPtr: {wFile}, extractedDirPtr: {eDir}";
                Log.Fatal(msg);
                return NativeResult.CreateError<int>(msg, Exports.InvalidArgsError);
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
            return NativeResult.CreateInlineSuccess(Exports.SuccessReturn);
        }
        catch (Exception ex)
        {
            Log.Fatal($"Repack Failed with error: {ex.Message}");
            return NativeResult.CreateError<int>(ex.Message, Exports.ExceptionError);
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "repack_filelist_from_chunks", CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe NativeResult.Result<int> RepackFileListFromChunks(int gameCode, byte* extractedFilelistDir, byte backup)
    {
        try
        {
            var eDir = NativeResult.StringFromPtr(extractedFilelistDir);
            var shouldBackup = backup != 0;
            var game = (LibaryEnums.GameCodes)gameCode;
            if (string.IsNullOrEmpty(eDir))
            {
                var msg = "Invalid Arguments! extractedFilelistDir is null or empty."
                          +$"\neDir: {eDir}";
                Log.Fatal(msg);
                return NativeResult.CreateError<int>(msg, Exports.InvalidArgsError);
            }
            
            Log.Info($"Repacking single file: {eDir} for game: {game}, backup: {shouldBackup}");
            RepackActions.RepackFilelistFromChunks(
                game,
                eDir,
                shouldBackup
            );
            Log.Info($"Repacked filelist: {eDir} successfully!");
            return NativeResult.CreateInlineSuccess(Exports.SuccessReturn);
        }
        catch (Exception ex)
        {
            Log.Fatal($"Repack Failed with error: {ex.Message}");
            return NativeResult.CreateError<int>(ex.Message, Exports.ExceptionError);
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "repack_filelist_from_json", CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe NativeResult.Result<int> RepackFileListFromJson(int gameCode, byte* jsonFile, byte backup)
    {
        try
        {
            var jFile = NativeResult.StringFromPtr(jsonFile);
            var game =  (LibaryEnums.GameCodes)gameCode;
            var shouldBackup = backup != 0;
            if (string.IsNullOrEmpty(jFile))
            {
                var msg = "Invalid Arguments! jsonFile is null or empty."
                          +$"\njsonFile: {jFile}";
                Log.Fatal(msg);
                return NativeResult.CreateError<int>(msg, Exports.InvalidArgsError);
            }
            Log.Info($"Repacking single file: {jFile} for game: {game}, backup: {shouldBackup}");
            RepackActions.RepackFilelistFromJson(
                game,
                jFile,
                shouldBackup
            );
            Log.Info($"Repacked filelist: {jFile} successfully!");
            return NativeResult.CreateInlineSuccess(Exports.SuccessReturn);
        }
        catch (Exception ex)
        {
            Log.Fatal($"Repack Failed with error: {ex.Message}");
            return NativeResult.CreateError<int>(ex.Message, Exports.ExceptionError);
        }
    }
}
