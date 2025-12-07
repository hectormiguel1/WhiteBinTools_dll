using WhiteBinTools.Native;

namespace WhiteBinTools.Support;

internal static class CommonMethods
{
    public static void ErrorExit(string errorMsg)
    {
        Log.Error(errorMsg);
        throw new Exception(errorMsg);
    }


    public static void IfFileExistsDel(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }


    public static void IfDirExistsDel(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, true);
        }
    }        
}