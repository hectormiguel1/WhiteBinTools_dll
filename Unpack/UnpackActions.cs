using System.Text;
using WhiteBinTools.Support;

namespace WhiteBinTools.Unpack;

public class UnpackActions
{
    public static void UnpackFull(LibaryEnums.GameCodes gameCode, string filelistFile, string whiteBinFile)
    {
        // TypeA specific: Deletes the directory if it exists before starting
        var tempVars = new UnpackVariables();
        UnpackProcesses.PrepareBinVars(whiteBinFile, tempVars);
        
        if (Directory.Exists(tempVars.ExtractDir))
        {
            Console.WriteLine("Detected previous unpack. deleting....");
            CommonMethods.IfDirExistsDel(tempVars.ExtractDir);
        }

        // Pass 'true' to extract everything
        UnpackProcesses.ExtractFiles(gameCode, filelistFile, whiteBinFile, _ => true);
    }
    
    public static void UnpackSingle(LibaryEnums.GameCodes gameCode, string filelistFile, string whiteBinFile, string whiteFilePath)
    {
        UnpackProcesses.ExtractFiles(gameCode, filelistFile, whiteBinFile, vars => vars.MainPath == whiteFilePath);
    }
    
    public static void UnpackMultiple(LibaryEnums.GameCodes gameCode, string filelistFile, string whiteBinFile, string whiteVirtualDirPath)
    {
        // Pre-process the input string
        var targetDir = whiteVirtualDirPath.Replace("*", "");

        UnpackProcesses.ExtractFiles(gameCode, filelistFile, whiteBinFile, vars =>
        {
            // Replicating the logic from your original file to match directories
            var currentPathDataArray = vars.MainPath.Split('\\');
            var assembledDir = string.Empty;

            foreach (var dir in currentPathDataArray)
            {
                assembledDir += dir + "\\";
                if (assembledDir == targetDir) break;
            }

            return assembledDir == targetDir;
        });
    }
    
    public static void UnpackFilelist(LibaryEnums.GameCodes gameCode, string filelistFile)
    {
        // 1. Initialize
        var vars = UnpackProcesses.InitializeFilelist(gameCode, filelistFile);

        var filelistOutName = Path.GetFileName(filelistFile);
        var extractedFilelistDir = Path.Combine(vars.MainFilelistDirectory, "_" + filelistOutName);
        var chunkTxtFilePathPrefix = Path.Combine(extractedFilelistDir, $"Chunk_");

        CommonMethods.IfDirExistsDel(extractedFilelistDir);
        Directory.CreateDirectory(extractedFilelistDir);

        // Write Info Header
        using (var infoStreamWriter = new StreamWriter(Path.Combine(extractedFilelistDir, "#info.txt"), true))
        {
            if (gameCode == LibaryEnums.GameCodes.ff132)
            {
                infoStreamWriter.WriteLine($"encrypted: {vars.IsEncrypted.ToString().ToLower()}");
                if (vars.IsEncrypted)
                {
                    infoStreamWriter.WriteLine($"seedA: {vars.SeedA}");
                    infoStreamWriter.WriteLine($"seedB: {vars.SeedB}");
                    infoStreamWriter.WriteLine($"encryptionTag(DO_NOT_CHANGE): {vars.EncTag}");
                }
            }
            infoStreamWriter.WriteLine($"fileCount: {vars.TotalFiles}");
            infoStreamWriter.WriteLine($"chunkCount: {vars.TotalChunks}");
        }

        // Initialize Dictionary
        var outChunksDict = new Dictionary<int, List<string>>();
        for (var c = 0; c < vars.TotalChunks; c++)
        {
            outChunksDict.Add(c, []);
        }

        // 2. Iterate and Collect
        UnpackProcesses.IterateEntries(gameCode, vars, _ =>
        {
            var stringData = $"{vars.FileCode}|";
            
            if (gameCode == LibaryEnums.GameCodes.ff132)
            {
                stringData += $"{vars.FileTypeID}|";
            }
            stringData += vars.PathString;

            var chunkKey = (gameCode == LibaryEnums.GameCodes.ff131) ? vars.ChunkNumber : vars.CurrentChunkNumber;
            outChunksDict[chunkKey].Add(stringData);
        });

        // 3. Write Output
        for (var d = 0; d < vars.TotalChunks; d++)
        {
            using var chunkWriter = new StreamWriter(chunkTxtFilePathPrefix + d + ".txt", true, new UTF8Encoding(false));
            foreach (var stringData in outChunksDict[d])
            {
                chunkWriter.WriteLine(stringData);
            }
        }

        Console.WriteLine($"\nFinished unpacking \"{filelistOutName}\"");
    }
    
    public static void UnpackFilelistPaths(LibaryEnums.GameCodes gameCode, string filelistFile)
    {
        var vars = UnpackProcesses.InitializeFilelist(gameCode, filelistFile);
        var outTxtFile = Path.Combine(vars.MainFilelistDirectory, Path.GetFileName(filelistFile) + ".txt");

        CommonMethods.IfFileExistsDel(outTxtFile);

        using (var outchunkWriter = new StreamWriter(outTxtFile, true))
        {
            UnpackProcesses.IterateEntries(gameCode, vars, (index) =>
            {
                outchunkWriter.WriteLine(vars.PathString);
            });
            outchunkWriter.WriteLine("end");
        }

        Console.WriteLine($"\nFinished writing filepaths to \"{Path.GetFileName(outTxtFile)}\"");
    }
}