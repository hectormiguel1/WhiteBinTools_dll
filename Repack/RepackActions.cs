using System.Text;
using WhiteBinTools.Filelist;
using WhiteBinTools.Native;
using WhiteBinTools.Support;

namespace WhiteBinTools.Repack;

public static class RepackActions
{
    // =======================================================================
    // Type A: Repack All (Full Rebuild)
    // =======================================================================
    public static void RepackAll(LibaryEnums.GameCodes gameCode, string filelistFile, string extractedDir, bool bckup)
    {
        Repacker.ExecuteBinRepackSession(gameCode, filelistFile, extractedDir, bckup, (filelistVars, repackVars, newChunksDict) =>
        {
            // Type A specific Setup: Handle old/new file rotation
            if (bckup)
            {
                repackVars.OldWhiteBinFileBackup = repackVars.NewWhiteBinFile + ".bak";
                CommonMethods.IfFileExistsDel(repackVars.OldWhiteBinFileBackup);
                if (File.Exists(repackVars.NewWhiteBinFile))
                {
                    File.Move(repackVars.NewWhiteBinFile, repackVars.OldWhiteBinFileBackup);
                }
            }
            CommonMethods.IfFileExistsDel(repackVars.NewWhiteBinFile);

            using var newWhiteBinStream = new FileStream(repackVars.NewWhiteBinFile, FileMode.Append, FileAccess.Write);
            
            Repacker.IterateFileEntries(gameCode, filelistVars, repackVars, extractedDir, newChunksDict, () =>
            {
                // Type A Logic: Create dummy if missing, then Append
                if (!File.Exists(repackVars.OgFullFilePath))
                {
                    var dir = Path.GetDirectoryName(repackVars.OgFullFilePath);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
                    using var _ = File.Create(repackVars.OgFullFilePath);
                }

                RepackProcesses.RepackTypeAppend(repackVars, newWhiteBinStream, repackVars.OgFullFilePath);
                
                Log.Fine($"{repackVars.RepackState} {Path.Combine(repackVars.NewWhiteBinFileName, repackVars.RepackLogMsg)} (Appended)");
                return true;
            });

            Log.Fine($"Finished repacking files to \"{repackVars.NewWhiteBinFileName}\"");
        });
    }

    // =======================================================================
    // Type B: Repack Single File (Modify Existing)
    // =======================================================================
    public static void RepackSingle(LibaryEnums.GameCodes gameCode, string filelistFile, string whiteBinFile, string targetFilePath, bool bckup)
    {
        // Type B Convention: Extracted dir is usually "_filename"
        var extractedDir = Path.Combine(Path.GetDirectoryName(whiteBinFile)!, "_" + Path.GetFileName(whiteBinFile));

        Repacker.ExecuteBinRepackSession(gameCode, filelistFile, extractedDir, bckup, (filelistVars, repackVars, newChunksDict) =>
        {
            if (bckup) RepackProcesses.CreateWhiteBinBackup(whiteBinFile, repackVars);
            
            // Logic differs from Type A: We don't delete NewWhiteBinFile, we modify it.
            // (Assuming NewWhiteBinFile points to the target bin we are patching)

            var hasPacked = false;

            Repacker.IterateFileEntries(gameCode, filelistVars, repackVars, extractedDir, newChunksDict, () =>
            {
                // Type B Logic: Only process if path matches target
                var currentFilePath = Path.Combine(repackVars.OgDirectoryPath, repackVars.OgFileName);
                if (currentFilePath != targetFilePath) return false;
                var packedAs = Repacker.PerformInjectOrAppend(repackVars);
                Log.Fine($"{repackVars.RepackState} {Path.Combine(repackVars.NewWhiteBinFileName, repackVars.RepackLogMsg)} {packedAs}");
                hasPacked = true;
                return true;
            });

            Log.Fine(hasPacked
                ? $"Finished repacking file(s) into \"{repackVars.NewWhiteBinFileName}\""
                : "Specified file does not exist. please specify the correct file path.");
        });
    }

    // =======================================================================
    // Type C: Repack Multiple Files (Modify Existing if found)
    // =======================================================================
    public static void RepackMultiple(LibaryEnums.GameCodes gameCode, string filelistFile, string whiteBinFile, string whiteExtractedDir, bool bckup)
    {
        Repacker.ExecuteBinRepackSession(gameCode, filelistFile, whiteExtractedDir, bckup, (filelistVars, repackVars, newChunksDict) =>
        {
            if (bckup) RepackProcesses.CreateWhiteBinBackup(whiteBinFile, repackVars);

            var hasPacked = false;

            Repacker.IterateFileEntries(gameCode, filelistVars, repackVars, whiteExtractedDir, newChunksDict, () =>
            {
                // Type C Logic: Process if file exists in the directory
                var currentFileInProcess = Path.Combine(repackVars.OgDirectoryPath, repackVars.OgFileName);
                if (!File.Exists(Path.Combine(whiteExtractedDir, currentFileInProcess))) return false;
                var packedAs = Repacker.PerformInjectOrAppend(repackVars);
                Log.Fine($"{repackVars.RepackState} {Path.Combine(repackVars.NewWhiteBinFileName, repackVars.RepackLogMsg)} {packedAs}");
                hasPacked = true;
                return true;
            });

            Log.Fine(hasPacked
                ? $"\nFinished repacking multiple files into \"{repackVars.NewWhiteBinFileName}\""
                : "Specified directory does not exist or contains no matching files.");
        });
    }

 // =======================================================================
    // Type D: Repack Filelist from Text Chunks
    // =======================================================================
    public static void RepackFilelistFromChunks(LibaryEnums.GameCodes gameCode, string extractedFilelistDir, bool bckup)
    {
        // 1. Parse Info.txt to get Filelist Variables
        var infoFile = Path.Combine(extractedFilelistDir, "#info.txt");
        if (!File.Exists(infoFile)) CommonMethods.ErrorExit("Error: #info.txt not found.");

        var infoLines = File.ReadAllLines(infoFile);
        var filelistVars = Repacker.ParseInfoTextFile(infoLines, gameCode);

        // 2. Execute Session
        Repacker.ExecuteMetadataRepackSession(gameCode, filelistVars, extractedFilelistDir, bckup, (repackVars, newChunksDict) =>
        {
            // Pre-calc odd chunks for FF13-2
            var oddChunkNumValues = Repacker.GetOddChunkValues(gameCode, filelistVars.TotalChunks);

            using var entriesStream = new MemoryStream();
            using var entriesWriter = new BinaryWriter(entriesStream);
            long entriesWriterPos = 0;
            var oddChunkCounter = 0;

            for (var c = 0; c < filelistVars.TotalChunks; c++)
            {
                filelistVars.LastChunkNumber = c;
                var currentChunkFile = Path.Combine(extractedFilelistDir, $"Chunk_{c}.txt");
                
                if (!File.Exists(currentChunkFile)) CommonMethods.ErrorExit($"Missing {currentChunkFile}");

                var currentChunkLines = File.ReadAllLines(currentChunkFile);

                for (var l = 0; l < currentChunkLines.Length; l++)
                {
                    var lineData = currentChunkLines[l].Split('|');
                    
                    // Parse Entry
                    Repacker.ProcessTextEntry(gameCode, lineData, c, l, entriesWriter, entriesWriterPos, oddChunkNumValues, oddChunkCounter, filelistVars);
                    
                    // Add Path to Dictionary
                    newChunksDict[c].AddRange(Encoding.UTF8.GetBytes(filelistVars.PathString + "\0"));
                    entriesWriterPos += 8;
                }
                oddChunkCounter++;
            }

            // Finalize Entries Data
            filelistVars.EntriesData = entriesStream.ToArray();
        });

        Log.Fine($"Finished repacking filelist data.");
    }

   // =======================================================================
    // Type E: Repack Filelist from JSON
    // =======================================================================
    public static void RepackFilelistFromJson(LibaryEnums.GameCodes gameCode, string jsonFile, bool bckup)
    {
        if (!File.Exists(jsonFile)) CommonMethods.ErrorExit("JSON file not found.");

        var filelistVars = new FilelistVariables();
        
        // We must keep the stream open to read the header, then pass it (or logic) to the session
        // However, ExecuteMetadataRepackSession is generic. 
        // Strategy: Parse header first, then reopen or pass reader logic. 
        // Given the linear nature of the original code, we will parse headers here.

        using var jsonReader = new StreamReader(jsonFile);
        _ = jsonReader.ReadLine(); // Skip opening brace

        // 1. Parse Headers
        Repacker.ParseJsonHeaders(jsonReader, gameCode, filelistVars);

        var outputDir = Path.GetDirectoryName(jsonFile)!;

        // 2. Execute Session
        Repacker.ExecuteMetadataRepackSession(gameCode, filelistVars, outputDir, bckup, (repackVars, newChunksDict) =>
        {
            // Verify Data Property start
            if (!jsonReader.ReadLine()!.TrimStart(' ').StartsWith("\"data\": {"))
                CommonMethods.ErrorExit("Error: data property specified in the json file is invalid");

            var oddChunkNumValues = Repacker.GetOddChunkValues(gameCode, filelistVars.TotalChunks);

            using var entriesStream = new MemoryStream();
            using var entriesWriter = new BinaryWriter(entriesStream);
            long entriesWriterPos = 0;
            var oddChunkCounter = 0;

            for (var c = 0; c < filelistVars.TotalChunks; c++)
            {
                filelistVars.LastChunkNumber = c;
                var currentChunkName = $"Chunk_{c}";

                // Validate Chunk Start
                if (!jsonReader.ReadLine()!.TrimStart(' ').StartsWith($"\"{currentChunkName}\": ["))
                    CommonMethods.ErrorExit($"Error: {currentChunkName} property missing or invalid");

                while (true)
                {
                    var line = jsonReader.ReadLine()?.Trim();
                    if (line == "}" || line == null) { _ = jsonReader.ReadLine(); break; } // End of chunk
                    if (line == "},") continue; 
                    if (line != "{") continue;

                    // Parse Entry
                    Repacker.ProcessJsonEntry(jsonReader, gameCode, c, entriesWriter, entriesWriterPos, oddChunkNumValues, oddChunkCounter, filelistVars, newChunksDict);
                    
                    entriesWriterPos += 8;
                }
                oddChunkCounter++;
            }

            filelistVars.EntriesData = entriesStream.ToArray();
        });

        Log.Fine("Finished repacking JSON data.");
    }

    
}