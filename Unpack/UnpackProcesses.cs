using WhiteBinTools.Filelist;
using WhiteBinTools.Native;
using WhiteBinTools.Support;
using WhiteBinTools.Support.Extensions;
using static WhiteBinTools.Support.LibaryEnums;

namespace WhiteBinTools.Unpack;

internal static class UnpackProcesses
{
    private static readonly string PathSeparatorChar = Convert.ToString(Path.DirectorySeparatorChar);

    // ---------------------------------------------------------
    // NEW: Shared Initialization Logic
    // ---------------------------------------------------------
    public static FilelistVariables InitializeFilelist(GameCodes gameCode, string filelistFile)
    {
        var vars = new FilelistVariables();
        FilelistProcesses.PrepareFilelistVars(vars, filelistFile);
        FilelistCrypto.DecryptProcess(gameCode, vars);

        using (var filelistStream = new FileStream(vars.MainFilelistFile, FileMode.Open, FileAccess.Read))
        {
            using (var filelistReader = new BinaryReader(filelistStream, GlobalConfig.DefaultEncoding))
            {
                FilelistChunksPrep.GetFilelistOffsets(filelistReader, vars);
                FilelistChunksPrep.BuildChunks(filelistStream, vars);
            }
        }

        if (gameCode == GameCodes.Ff132)
        {
            vars.CurrentChunkNumber = -1;
        }

        if (!vars.IsEncrypted) return vars;
        CommonMethods.IfFileExistsDel(vars.TmpDcryptFilelistFile);
        vars.MainFilelistFile = filelistFile;

        using var encDataReader = new BinaryReader(File.Open(filelistFile, FileMode.Open, FileAccess.Read), GlobalConfig.DefaultEncoding);
        encDataReader.BaseStream.Position = 0;
        vars.SeedA = encDataReader.ReadUInt64();
        vars.SeedB = encDataReader.ReadUInt64();
        encDataReader.BaseStream.Position += 4;
        vars.EncTag = encDataReader.ReadUInt32();

        return vars;
    }

    // ---------------------------------------------------------
    // NEW: Shared Loop Logic
    // ---------------------------------------------------------
    public static void IterateEntries(GameCodes gameCode, FilelistVariables vars, Action<int> onEntry)
    {
        using var entriesStream = new MemoryStream();
        entriesStream.Write(vars.EntriesData, 0, vars.EntriesData.Length);
        entriesStream.Seek(0, SeekOrigin.Begin);

        using var entriesReader = new BinaryReader(entriesStream, GlobalConfig.DefaultEncoding);
        long entriesReadPos = 0;
        for (var f = 0; f < vars.TotalFiles; f++)
        {
            FilelistProcesses.GetCurrentFileEntry(gameCode, entriesReader, entriesReadPos, vars);
            entriesReadPos += 8;

            onEntry(f);
        }
    }

    // ---------------------------------------------------------
    // NEW: Shared Extraction Logic (Replaces Logic in A, B, C)
    // ---------------------------------------------------------
    public static void ExtractFiles(GameCodes gameCode, string filelistFile, string whiteBinFile, Func<FilelistVariables, bool>? shouldExtractPredicate, string? whiteExtractedDir = null)
    {
        var vars = InitializeFilelist(gameCode, filelistFile);
        var unpackVars = new UnpackVariables();
        PrepareBinVars(whiteBinFile, unpackVars, whiteExtractedDir);

        // Clean/Create Directory
        // Type A deletes existing, B/C just creates. 
        // We will default to create, but let the caller handle deletion if needed before calling this.
        if (!Directory.Exists(unpackVars.ExtractDir))
        {
            Directory.CreateDirectory(unpackVars.ExtractDir);
        }

        var hasExtracted = false;

        using (var whiteBinStream = new FileStream(whiteBinFile, FileMode.Open, FileAccess.Read))
        {
            IterateEntries(gameCode, vars, (index) =>
            {
                PrepareExtraction(vars.PathString, vars, unpackVars.ExtractDir);

                // Run the specific filter (e.g. "Is this the file I want?")
                if (shouldExtractPredicate != null && !shouldExtractPredicate(vars)) return;

                // Ensure Subdirectory exists
                var targetDir = Path.Combine(unpackVars.ExtractDir, vars.DirectoryPath);
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                // Check Duplicates
                if (File.Exists(vars.FullFilePath))
                {
                    File.Delete(vars.FullFilePath);
                    unpackVars.CountDuplicates++;
                }

                // Unpack
                UnpackFile(vars, whiteBinStream, unpackVars);
                hasExtracted = true;

                Log.Debug($"{unpackVars.UnpackedState} _{Path.Combine(unpackVars.ExtractDirName, vars.MainPath)}");
            });
        }

        if (hasExtracted)
        {
            Log.Debug($"Finished unpacking \"{unpackVars.WhiteBinName}\"");
            if (unpackVars.CountDuplicates > 0)
            {
                Log.Warn($"{unpackVars.CountDuplicates} duplicate file(s)");
            }
        }
        else
        {
            Log.Warn("Specified file/directory does not exist or nothing was extracted.");
        }
    }

    // ---------------------------------------------------------
    // Existing Helpers
    // ---------------------------------------------------------
    public static void PrepareBinVars(string whiteBinFile, UnpackVariables unpackVariables, string? extractDir = null)
    {
        unpackVariables.WhiteBinName = Path.GetFileName(whiteBinFile);
        var inBinFilePath = Path.GetFullPath(whiteBinFile);
        unpackVariables.InBinFileDir = Path.GetDirectoryName(inBinFilePath) ?? string.Empty;
        unpackVariables.ExtractDirName = Path.GetFileName(whiteBinFile);
        unpackVariables.ExtractDir = Path.Combine( extractDir ?? unpackVariables.InBinFileDir, "_" + unpackVariables.ExtractDirName);
    }

    public static void PrepareExtraction(string convertedString, FilelistVariables filelistVariables, string extractDir)
    {
        filelistVariables.ConvertedStringData = convertedString.Split(':');
        filelistVariables.Position = Convert.ToUInt32(filelistVariables.ConvertedStringData[0], 16) * 2048;
        filelistVariables.UnCmpSize = Convert.ToUInt32(filelistVariables.ConvertedStringData[1], 16);
        filelistVariables.CmpSize = Convert.ToUInt32(filelistVariables.ConvertedStringData[2], 16);
        filelistVariables.MainPath = filelistVariables.ConvertedStringData[3].Replace("/", PathSeparatorChar);
        filelistVariables.IsCompressed = false;

        if (filelistVariables.MainPath == " ")
        {
            filelistVariables.NoPathFileCount++;
            filelistVariables.DirectoryPath = "noPath";
            filelistVariables.FileName = "FILE_" + filelistVariables.NoPathFileCount;
            filelistVariables.FullFilePath = Path.Combine(extractDir, filelistVariables.DirectoryPath, filelistVariables.FileName);
            filelistVariables.MainPath = Path.Combine(filelistVariables.DirectoryPath, filelistVariables.FileName);
        }
        else
        {
            filelistVariables.DirectoryPath = Path.GetDirectoryName(filelistVariables.MainPath) ?? string.Empty;
            filelistVariables.FileName = Path.GetFileName(filelistVariables.MainPath);
            filelistVariables.FullFilePath = Path.Combine(extractDir, filelistVariables.DirectoryPath, filelistVariables.FileName);
        }

        if (filelistVariables.UnCmpSize != filelistVariables.CmpSize)
        {
            filelistVariables.IsCompressed = true;
        }
    }

    public static void UnpackFile(FilelistVariables filelistVariables, FileStream whiteBinStream, UnpackVariables unpackVariables)
    {
        // Using the existing logic you provided, just keeping it cleaner
        if (filelistVariables.IsCompressed)
        {
            using var cmpData = new MemoryStream();
            whiteBinStream.Seek(filelistVariables.Position, SeekOrigin.Begin);
            whiteBinStream.CopyStreamTo(cmpData, filelistVariables.CmpSize, false);

            using var outFile = new FileStream(filelistVariables.FullFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            cmpData.Seek(0, SeekOrigin.Begin);
            ZlibMethods.ZlibDecompress(cmpData, outFile);
            unpackVariables.UnpackedState = "Decompressed";
        }
        else
        {
            using var outFile = new FileStream(filelistVariables.FullFilePath, FileMode.OpenOrCreate, FileAccess.Write);
            outFile.Seek(0, SeekOrigin.Begin);
            whiteBinStream.Seek(filelistVariables.Position, SeekOrigin.Begin);
            whiteBinStream.CopyStreamTo(outFile, filelistVariables.UnCmpSize, false);
            unpackVariables.UnpackedState = "Copied";
        }
    }
}