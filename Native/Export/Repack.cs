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
    public static int RepackAll(int gameCode, IntPtr filelistFile, IntPtr extractedDir, byte bckup)
    {
        try
        {
            var fFile = PtrToString(filelistFile);
            var eDir = PtrToString(extractedDir);

            if (string.IsNullOrEmpty(fFile) || string.IsNullOrEmpty(eDir)) 
                return -1;

            RepackActions.RepackAll(
                (LibaryEnums.GameCodes)gameCode, 
                fFile, 
                eDir, 
                bckup != 0
            );
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WBT Native Error]: {ex.Message}");
            return 1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "repack_single", CallConvs = [typeof(CallConvCdecl)])]
    public static int RepackSingle(int gameCode, IntPtr filelistFile, IntPtr whiteBinFile, IntPtr targetFilePath, byte bckup)
    {
        try
        {
            var fFile = PtrToString(filelistFile);
            var wFile = PtrToString(whiteBinFile);
            var tPath = PtrToString(targetFilePath);

            if (string.IsNullOrEmpty(fFile) || string.IsNullOrEmpty(wFile) || string.IsNullOrEmpty(tPath)) 
                return -1;

            RepackActions.RepackSingle(
                (LibaryEnums.GameCodes)gameCode, 
                fFile, 
                wFile, 
                tPath, 
                bckup != 0
            );
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WBT Native Error]: {ex.Message}");
            return 1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "repack_multiple", CallConvs = [typeof(CallConvCdecl)])]
    public static int RepackMultiple(int gameCode, IntPtr filelistFile, IntPtr whiteBinFile, IntPtr whiteExtractedDir, byte bckup)
    {
        try
        {
            var fFile = PtrToString(filelistFile);
            var wFile = PtrToString(whiteBinFile);
            var eDir = PtrToString(whiteExtractedDir);

            if (string.IsNullOrEmpty(fFile) || string.IsNullOrEmpty(wFile) || string.IsNullOrEmpty(eDir)) 
                return -1;

            RepackActions.RepackMultiple(
                (LibaryEnums.GameCodes)gameCode, 
                fFile, 
                wFile, 
                eDir, 
                bckup != 0
            );
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WBT Native Error]: {ex.Message}");
            return 1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "repack_filelist_from_chunks", CallConvs = [typeof(CallConvCdecl)])]
    public static int RepackFilelistFromChunks(int gameCode, IntPtr extractedFilelistDir, byte bckup)
    {
        try
        {
            var eDir = PtrToString(extractedFilelistDir);

            if (string.IsNullOrEmpty(eDir)) 
                return -1;

            RepackActions.RepackFilelistFromChunks(
                (LibaryEnums.GameCodes)gameCode, 
                eDir, 
                bckup != 0
            );
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WBT Native Error]: {ex.Message}");
            return 1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "repack_filelist_from_json", CallConvs = [typeof(CallConvCdecl)])]
    public static int RepackFilelistFromJson(int gameCode, IntPtr jsonFile, byte bckup)
    {
        try
        {
            var jFile = PtrToString(jsonFile);

            if (string.IsNullOrEmpty(jFile)) 
                return -1;

            RepackActions.RepackFilelistFromJson(
                (LibaryEnums.GameCodes)gameCode, 
                jFile, 
                bckup != 0
            );
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WBT Native Error]: {ex.Message}");
            return 1;
        }
    }
}