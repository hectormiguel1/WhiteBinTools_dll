using System.Text;
using WhiteBinTools.Filelist;
using WhiteBinTools.Native;
using WhiteBinTools.Support;
using WhiteBinTools.Support.Extensions;
using static WhiteBinTools.Support.LibaryEnums;

namespace WhiteBinTools.Repack;

/// <summary>
/// Handles the core orchestration lifecycles for repacking sessions.
/// Contains shared logic for initializing variables, decryption, stream management, and finalization.
/// </summary>
public static class Repacker
{
    /// <summary>
    /// Execute a standard Binary Repack Session (Type A, B, C).
    /// Handles Filelist setup, Decryption, Chunk Prep, and Finalization.
    /// </summary>
    public static void ExecuteBinRepackSession(
        GameCodes gameCode,
        string filelistFile,
        string extractedDir,
        bool bckup,
        Action<FilelistVariables, RepackVariables, Dictionary<int, List<byte>>> coreLogic)
    {
        // 1. Setup Phase
        var filelistVariables = new FilelistVariables();
        var repackVariables = new RepackVariables();

        FilelistProcesses.PrepareFilelistVars(filelistVariables, filelistFile);
        RepackProcesses.PrepareRepackVars(repackVariables, filelistFile, filelistVariables, extractedDir);

        if (bckup)
        {
            RepackProcesses.CreateFilelistBackup(filelistFile, repackVariables);
            // Backup creation logic specific to WhiteBin is handled inside the specific Actions if needed, 
            // or we can generalize it here if it applies to all. 
            // For now, Type A moves, Type B/C copy. We leave specific bin backup to the caller or pre-hook.
        }

        // 2. Decrypt & Parse Original Filelist
        DecryptAndPrepareChunks(gameCode, filelistVariables, filelistFile);

        // 3. Prepare Chunk Container
        if (gameCode == GameCodes.Ff132) filelistVariables.CurrentChunkNumber = -1;
        
        var newChunksDict = new Dictionary<int, List<byte>>();
        RepackProcesses.CreateEmptyNewChunksDict(filelistVariables, newChunksDict);

        // 4. Run the Core Logic (injected by RepackActions)
        coreLogic(filelistVariables, repackVariables, newChunksDict);

        // 5. Finalize: Build new Filelist and Encrypt
        Log.Debug("Building filelist....");
        RepackFilelistData.BuildFilelist(filelistVariables, newChunksDict, repackVariables, gameCode);

        if (filelistVariables.IsEncrypted)
        {
            FilelistCrypto.EncryptProcess(repackVariables);
        }
    }

    /// <summary>
    /// Execute a Metadata Repack Session (Type D, E).
    /// Handles headers and generic filelist building.
    /// </summary>
    public static void ExecuteMetadataRepackSession(
        GameCodes gameCode,
        FilelistVariables filelistVariables,
        string outputDir,
        bool bckup,
        Action<RepackVariables, Dictionary<int, List<byte>>> coreLogic)
    {
        Log.Debug("TotalChunks: " + filelistVariables.TotalChunks);
        Log.Debug("No of files: " + filelistVariables.TotalFiles + "\n");
        Log.Debug("Building filelist....");

        var repackVariables = new RepackVariables
        {
            NewFilelistFile = Path.Combine(outputDir, "filelist.win32.bin") // Adjusted to generic naming or derived from input
        };

        // Handle Backup
        if (bckup && File.Exists(repackVariables.NewFilelistFile))
        {
            CommonMethods.IfFileExistsDel(repackVariables.NewFilelistFile + ".bak");
            File.Copy(repackVariables.NewFilelistFile, repackVariables.NewFilelistFile + ".bak");
        }
        CommonMethods.IfFileExistsDel(repackVariables.NewFilelistFile);

        // Prepare Chunks
        var newChunksDict = new Dictionary<int, List<byte>>();
        RepackProcesses.CreateEmptyNewChunksDict(filelistVariables, newChunksDict);

        // Run Core Logic
        coreLogic(repackVariables, newChunksDict);

        // Finalize
        RepackFilelistData.BuildFilelist(filelistVariables, newChunksDict, repackVariables, gameCode);

        if (filelistVariables.IsEncrypted)
        {
            FilelistCrypto.EncryptProcess(repackVariables);
        }
    }

    /// <summary>
    /// Shared helper to iterate through the FileEntries stream.
    /// Replaces the big 'for' loop found in RepackTypeA/B/C.
    /// </summary>
    public static void IterateFileEntries(
        GameCodes gameCode,
        FilelistVariables fVars,
        RepackVariables rVars,
        string extractedDir,
        Dictionary<int, List<byte>> chunksDict,
        Func<bool> onEntryProcess) // Returns true if a specific action (like packing) happened
    {
        using var entriesStream = new MemoryStream();
        entriesStream.Write(fVars.EntriesData, 0, fVars.EntriesData.Length);
        entriesStream.Seek(0, SeekOrigin.Begin);

        using var entriesReader = new BinaryReader(entriesStream, GlobalConfig.DefaultEncoding);
        
        long entriesReadPos = 0;
        for (var f = 0; f < fVars.TotalFiles; f++)
        {
            FilelistProcesses.GetCurrentFileEntry(gameCode, entriesReader, entriesReadPos, fVars);
            entriesReadPos += 8;

            // Populate RepackVariables with current file info
            RepackProcesses.GetPackedState(fVars.PathString, rVars, extractedDir);
            
            // Standardize hex string values for chunk writing
            rVars.AsciiFilePos = rVars.ConvertedOgStringData[0];
            rVars.AsciiUnCmpSize = rVars.ConvertedOgStringData[1];
            rVars.AsciiCmpSize = rVars.ConvertedOgStringData[2];
            
            // Always build the path for the chunk at the end
            RepackProcesses.BuildPathForChunk(rVars, gameCode, fVars, chunksDict);
        }
    }

    public static void DecryptAndPrepareChunks(GameCodes gameCode, FilelistVariables vars, string filePath)
    {
        FilelistCrypto.DecryptProcess(gameCode, vars);

        using (var filelistStream = new FileStream(vars.MainFilelistFile, FileMode.Open, FileAccess.Read))
        {
            using (var filelistReader = new BinaryReader(filelistStream, GlobalConfig.DefaultEncoding))
            {
                FilelistChunksPrep.GetFilelistOffsets(filelistReader, vars);
                FilelistChunksPrep.BuildChunks(filelistStream, vars);

                if (vars.IsEncrypted)
                {
                    filelistStream.Seek(0, SeekOrigin.Begin);
                    vars.EncryptedHeaderData = new byte[32];
                    filelistStream.ReadExactly(vars.EncryptedHeaderData, 0, 32);

                    filelistStream.Dispose();
                    File.Delete(vars.MainFilelistFile);
                }
            }
        }
        CommonMethods.IfFileExistsDel(filePath); // Delete the temp decrypted file
    }
    // =======================================================================
    // Shared Utilities
    // =======================================================================

    public static List<int> GetOddChunkValues(GameCodes code, uint totalChunks)
    {
        var list = new List<int>();
        if (code != GameCodes.Ff132 || totalChunks <= 1) return list;
        var next = 1;
        for (var i = 0; i < totalChunks; i++)
        {
            if (i != next) continue;
            list.Add(i);
            next += 2;
        }
        return list;
    }

    private static byte[] GenerateEncHeader(ulong seedA, ulong seedB, uint tag)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.WriteBytesUInt64(seedA, false);
        bw.WriteBytesUInt64(seedB, false);
        bw.WriteBytesUInt32(0, false);
        bw.WriteBytesUInt32(tag, false);
        bw.WriteBytesUInt64(0, false);
        return ms.ToArray();
    }

    // Validation Helpers
    private enum ValueTypes { Boolean, Byte, Uint, Ulong }

    private static T ParseInfoLine<T>(string line, string expectedProp, ValueTypes type, Func<string, T> parser)
    {
        if (!line.Trim().StartsWith(expectedProp)) CommonMethods.ErrorExit($"Invalid property: {expectedProp}");
        try { return parser(line.Split(' ')[1].Trim()); }
        catch { CommonMethods.ErrorExit($"Invalid value for {expectedProp}"); return default!; }
    }

    private static string CheckGetMainProperty(StreamReader reader, string expected, ValueTypes type)
    {
        // Re-implementation of CheckGetMainProperty logic from RepackTypeE
        var line = reader.ReadLine()!;
        var parts = line.Split(':');
        if (!parts[0].Trim().StartsWith(expected)) CommonMethods.ErrorExit($"Missing {expected}");
        return parts[1].Trim().TrimEnd(',');
    }

    private static string CheckGetChunkEntryProperty(string line, string expected, int chunkId, ValueTypes type)
    {
        // Re-implementation of CheckGetChunkEntryProperty logic from RepackTypeE
        var parts = line.Split(':');
        if (!parts[0].Trim().StartsWith(expected)) CommonMethods.ErrorExit($"Missing {expected} in Chunk_{chunkId}");
        return parts[1].Trim().TrimEnd(',');
    }
    
    private static void CheckChunkEntryData(string val, ValueTypes type, int chunkId, int lineNo)
    {
        var valid = type switch {
            ValueTypes.Byte => byte.TryParse(val, out _),
            ValueTypes.Uint => uint.TryParse(val, out _),
            _ => true
        };
        if (!valid) CommonMethods.ErrorExit($"Invalid data in Chunk_{chunkId} line {lineNo}");
    }
    // =======================================================================
    // Helpers: Text Parsing (Type D Logic)
    // =======================================================================

    internal static FilelistVariables ParseInfoTextFile(string[] lines, GameCodes code)
    {
        var vars = new FilelistVariables();
        // Logic adapted directly from RepackTypeD.cs switch statement
        if (code == GameCodes.Ff131)
        {
            if (lines.Length < 2) CommonMethods.ErrorExit("#info.txt invalid.");
            vars.TotalFiles = ParseInfoLine(lines[0], "fileCount: ", ValueTypes.Uint, (s) => uint.Parse(s));
            vars.TotalChunks = ParseInfoLine(lines[1], "chunkCount: ", ValueTypes.Uint, (s) => uint.Parse(s));
        }
        else // ff132
        {
            if (lines.Length < 3) CommonMethods.ErrorExit("#info.txt invalid.");
            vars.IsEncrypted = ParseInfoLine(lines[0], "encrypted: ", ValueTypes.Boolean, (s) => bool.Parse(s));

            if (vars.IsEncrypted)
            {
                vars.SeedA = ParseInfoLine(lines[1], "seedA: ", ValueTypes.Ulong, (s) => ulong.Parse(s));
                vars.SeedB = ParseInfoLine(lines[2], "seedB: ", ValueTypes.Ulong, (s) => ulong.Parse(s));
                vars.EncTag = ParseInfoLine(lines[3], "encryptionTag(DO_NOT_CHANGE): ", ValueTypes.Uint, (s) => uint.Parse(s));
                vars.TotalFiles = ParseInfoLine(lines[4], "fileCount: ", ValueTypes.Uint, (s) => uint.Parse(s));
                vars.TotalChunks = ParseInfoLine(lines[5], "chunkCount: ", ValueTypes.Uint, (s) => uint.Parse(s));
                
                vars.EncryptedHeaderData = GenerateEncHeader(vars.SeedA, vars.SeedB, vars.EncTag);
            }
            else
            {
                vars.TotalFiles = ParseInfoLine(lines[1], "fileCount: ", ValueTypes.Uint, (s) => uint.Parse(s));
                vars.TotalChunks = ParseInfoLine(lines[2], "chunkCount: ", ValueTypes.Uint, (s) => uint.Parse(s));
            }
        }
        return vars;
    }

    internal static void ProcessTextEntry(GameCodes code, string[] data, int chunkId, int lineIdx, BinaryWriter writer, long pos, List<int> oddChunks, int oddCtr, FilelistVariables vars)
    {
        //
        CheckChunkEntryData(data[0], ValueTypes.Uint, chunkId, lineIdx);
        vars.FileCode = uint.Parse(data[0]);
        writer.BaseStream.Position = pos;
        writer.WriteBytesUInt32(vars.FileCode, false);

        if (code == GameCodes.Ff131)
        {
            if (data.Length < 2) CommonMethods.ErrorExit($"Line {lineIdx} in Chunk_{chunkId} invalid.");
            writer.BaseStream.Position = pos + 4;
            writer.WriteBytesUInt16((ushort)chunkId, false);
            writer.BaseStream.Position = pos + 6;
            writer.WriteBytesUInt16(0, false);
            vars.PathString = data[1];
        }
        else // ff132
        {
            if (data.Length < 3) CommonMethods.ErrorExit($"Line {lineIdx} in Chunk_{chunkId} invalid.");
            CheckChunkEntryData(data[1], ValueTypes.Byte, chunkId, lineIdx);
            vars.FileTypeId = byte.Parse(data[1]);

            writer.BaseStream.Position = pos + 4;
            // Write 32768 position value if odd chunk
            if (oddChunks.Contains(chunkId))
            {
                oddCtr = oddChunks.IndexOf(chunkId);
                writer.WriteBytesUInt16(32768, false);
            }
            else
            {
                writer.WriteBytesUInt16(0, false);
            }

            writer.BaseStream.Position = pos + 6;
            writer.Write((byte)oddCtr);
            writer.BaseStream.Position = pos + 7;
            writer.Write(vars.FileTypeId);
            vars.PathString = data[2];
        }
    }

    // =======================================================================
    // Helpers: JSON Parsing (Type E Logic)
    // =======================================================================

    internal static void ParseJsonHeaders(StreamReader reader, GameCodes code, FilelistVariables vars)
    {
        //
        if (code == GameCodes.Ff132)
        {
            vars.IsEncrypted = bool.Parse(CheckGetMainProperty(reader, "\"encrypted\"", ValueTypes.Boolean));
            if (vars.IsEncrypted)
            {
                vars.SeedA = ulong.Parse(CheckGetMainProperty(reader, "\"seedA\"", ValueTypes.Ulong));
                vars.SeedB = ulong.Parse(CheckGetMainProperty(reader, "\"seedB\"", ValueTypes.Ulong));
                vars.EncTag = uint.Parse(CheckGetMainProperty(reader, "\"encryptionTag(DO_NOT_CHANGE)\"", ValueTypes.Uint));
                vars.EncryptedHeaderData = GenerateEncHeader(vars.SeedA, vars.SeedB, vars.EncTag);
            }
        }
        vars.TotalFiles = uint.Parse(CheckGetMainProperty(reader, "\"fileCount\"", ValueTypes.Uint));
        vars.TotalChunks = uint.Parse(CheckGetMainProperty(reader, "\"chunkCount\"", ValueTypes.Uint));
    }

    internal static void ProcessJsonEntry(StreamReader reader, GameCodes code, int chunkId, BinaryWriter writer, long pos, List<int> oddChunks, int oddCtr, FilelistVariables vars, Dictionary<int, List<byte>> chunksDict)
    {
        // 1. FileCode
        var line = reader.ReadLine()?.Trim();
        var val = CheckGetChunkEntryProperty(line!, "\"fileCode\"", chunkId, ValueTypes.Uint);
        vars.FileCode = uint.Parse(val);

        writer.BaseStream.Position = pos;
        writer.WriteBytesUInt32(vars.FileCode, false);

        // 2. Metadata (Type/ChunkID)
        if (code == GameCodes.Ff131)
        {
            writer.BaseStream.Position = pos + 4;
            writer.WriteBytesUInt16((ushort)chunkId, false);
            writer.BaseStream.Position = pos + 6;
            writer.WriteBytesUInt16(0, false);
        }
        else // ff132
        {
            line = reader.ReadLine()?.Trim();
            val = CheckGetChunkEntryProperty(line!, "\"fileTypeID\"", chunkId, ValueTypes.Byte);
            vars.FileTypeId = byte.Parse(val);

            writer.BaseStream.Position = pos + 4;
            if (oddChunks.Contains(chunkId))
            {
                oddCtr = oddChunks.IndexOf(chunkId);
                writer.WriteBytesUInt16(32768, false);
            }
            else
            {
                writer.WriteBytesUInt16(0, false);
            }
            writer.BaseStream.Position = pos + 6;
            writer.Write((byte)oddCtr);
            writer.BaseStream.Position = pos + 7;
            writer.Write(vars.FileTypeId);
        }

        // 3. FilePath
        line = reader.ReadLine()?.Trim();
        var parts = line!.Split(["\": "], StringSplitOptions.None);
        if (!parts[0].StartsWith("\"filePath")) CommonMethods.ErrorExit($"Error: Missing filePath in Chunk_{chunkId}");
        
        vars.PathString = parts[1].Replace("\"", "").TrimEnd(',');
        chunksDict[chunkId].AddRange(Encoding.UTF8.GetBytes(vars.PathString + "\0"));
    }
    
    // =======================================================================
    // Shared Logic Helper (Inject vs Append decision)
    // =======================================================================
    internal static string PerformInjectOrAppend(RepackVariables rVars)
    {
        var packedAs = "";
        
        // Logic to decide if we can overwrite in place (Inject) or must add to end (Append)
        if (rVars.WasCompressed)
        {
            RepackProcesses.CleanOldFile(rVars.NewWhiteBinFile, rVars.OgFilePos, rVars.OgCmpSize);
            
            // Re-compress to check size
            var zlibTmpCmpData = ZlibMethods.ZlibCompress(rVars.OgFullFilePath);
            var zlibCmpFileSize = (uint)zlibTmpCmpData.Length;

            if (zlibCmpFileSize <= rVars.OgCmpSize)
                RepackProcesses.InjectProcess(rVars, ref packedAs);
            else
                RepackProcesses.AppendProcess(rVars, ref packedAs);
        }
        else
        {
            RepackProcesses.CleanOldFile(rVars.NewWhiteBinFile, rVars.OgFilePos, rVars.OgUnCmpSize);
            
            var dummyFileSize = (uint)new FileInfo(rVars.OgFullFilePath).Length;

            if (dummyFileSize <= rVars.OgUnCmpSize)
                RepackProcesses.InjectProcess(rVars, ref packedAs);
            else
                RepackProcesses.AppendProcess(rVars, ref packedAs);
        }
        return packedAs;
    }
}