using System.Runtime.Serialization;
using WhiteBinTools.Crypto;
using WhiteBinTools.Native;
using WhiteBinTools.Repack;
using WhiteBinTools.Support;
using WhiteBinTools.Support.Extensions;
using static WhiteBinTools.Support.LibaryEnums;

namespace WhiteBinTools.Filelist;

internal static class FilelistCrypto
{
    public static void DecryptProcess(GameCodes gameCode, FilelistVariables filelistVariables)
    {
        switch (gameCode)
        {
            // Check for encryption header in the filelist file,
            // if the game code is set to ff13-1
            case GameCodes.Ff131:
            {
                filelistVariables.IsEncrypted = CheckIfEncrypted(filelistVariables.MainFilelistFile);

                if (filelistVariables.IsEncrypted)
                {
                    NativeLogger.Error("Error: Detected encrypted filelist file. set the game code to 'ff132' for handling this type of filelist");
                    throw new ArgumentException("Encrypted filelist with FF13-1 Game mode set!");
                }

                break;
            }
            // Check for encryption header in the filelist file,
            // if the game code is set to ff13-2
            case GameCodes.Ff132:
                filelistVariables.IsEncrypted = CheckIfEncrypted(filelistVariables.MainFilelistFile);
                break;
        }

        // Check if the filelist is in decrypted
        // state and the length of the cryptBody
        // for divisibility.
        // If the file was decrypted then skip
        // decrypting it.
        // If the filelist is encrypted then
        // decrypt the filelist file by first
        // creating a temp copy of the filelist.            
        if (!filelistVariables.IsEncrypted) return;
        var wasDecrypted = false;

        using (var encCheckReader = new BinaryReader(File.Open(filelistVariables.MainFilelistFile, FileMode.Open, FileAccess.Read), GlobalConfig.DefaultEncoding))
        {
            encCheckReader.BaseStream.Position = 16;
            var cryptBodySize = encCheckReader.ReadBytesUInt32(true);
            cryptBodySize += 8;

            if (cryptBodySize % 8 != 0)
            {
                NativeLogger.Error("Length of the body to decrypt/encrypt is not valid");
                throw new InvalidDataContractException("Length of the body to decrypt/encrypt is not valid");
            }

            encCheckReader.BaseStream.Position = 32 + cryptBodySize - 8;
            cryptBodySize -= 8;

            if (encCheckReader.ReadUInt32() == cryptBodySize)
            {
                wasDecrypted = true;
            }
        }

        switch (wasDecrypted)
        {
            case true:
                CommonMethods.IfFileExistsDel(filelistVariables.TmpDcryptFilelistFile);
                File.Copy(filelistVariables.MainFilelistFile, filelistVariables.TmpDcryptFilelistFile);

                filelistVariables.MainFilelistFile = filelistVariables.TmpDcryptFilelistFile;
                break;

            case false:
                CommonMethods.IfFileExistsDel(filelistVariables.TmpDcryptFilelistFile);
                File.Copy(filelistVariables.MainFilelistFile, filelistVariables.TmpDcryptFilelistFile);

                NativeLogger.Debug("Decrypting the filelist...");
                CryptFilelist.ProcessFilelist(CryptActions.D, filelistVariables.TmpDcryptFilelistFile);

                using (var decFilelistReader = new BinaryReader(File.Open(filelistVariables.TmpDcryptFilelistFile, FileMode.Open, FileAccess.Read), GlobalConfig.DefaultEncoding))
                {
                    decFilelistReader.BaseStream.Position = 16;
                    var filelistDataSize = decFilelistReader.ReadBytesUInt32(true);
                    var hashOffset = 32 + filelistDataSize + 4;

                    decFilelistReader.BaseStream.Position = hashOffset;
                    var filelistHash = decFilelistReader.ReadUInt32();

                    if (filelistHash != decFilelistReader.ComputeCheckSum(filelistDataSize / 4, 32))
                    {
                        NativeLogger.Error("Filelist was not decrypted correctly");
                        throw new InvalidDataContractException("Failed to decrypt the filelist");
                    }
                }

                NativeLogger.Info("Finished decrypting filelist file");

                filelistVariables.MainFilelistFile = filelistVariables.TmpDcryptFilelistFile;
                break;
        }
    }


    private static bool CheckIfEncrypted(string filelistFile)
    {
        var isEncrypted = false;
        using var encStream = new FileStream(filelistFile, FileMode.Open, FileAccess.Read);
        using var encStreamReader = new BinaryReader(encStream, GlobalConfig.DefaultEncoding);
        encStreamReader.BaseStream.Position = 20;
        var encHeaderNumber = encStreamReader.ReadUInt32();

        if (encHeaderNumber == 501232760)
        {
            isEncrypted = true;
        }

        return isEncrypted;
    }


    public static void EncryptProcess(RepackVariables repackVariables)
    {
        var filelistDataSize = (uint)0;

        // Check filelist size if divisibile by 8
        // and pad in null bytes if not divisible.
        // Then write some null bytes for the size 
        // and hash offsets
        using (var preEncryptedfilelist = new FileStream(repackVariables.NewFilelistFile, FileMode.Append, FileAccess.Write))
        {
            filelistDataSize = (uint)preEncryptedfilelist.Length - 32;

            if (filelistDataSize % 8 != 0)
            {
                // Get remainder from the division and
                // reduce the remainder with 8. set that
                // reduced value to a variable
                var remainder = filelistDataSize % 8;
                var increaseByteAmount = 8 - remainder;

                // Increase the filelist size with the
                // increase byte variable from the previous step and
                // set this as a variable
                // Then get the amount of null bytes to pad by subtracting 
                // the new size with the filelist size
                var newSize = filelistDataSize + increaseByteAmount;
                var padNulls = newSize - filelistDataSize;

                preEncryptedfilelist.Seek((uint)preEncryptedfilelist.Length, SeekOrigin.Begin);
                preEncryptedfilelist.PadNull(padNulls);

                filelistDataSize = newSize;
            }

            // Add 8 bytes for the size and hash
            // offsets and 8 null bytes
            preEncryptedfilelist.Seek((uint)preEncryptedfilelist.Length, SeekOrigin.Begin);
            preEncryptedfilelist.PadNull(16);
        }

        using (var filelistToEncrypt = new FileStream(repackVariables.NewFilelistFile, FileMode.Open, FileAccess.Write))
        {
            using (var filelistToEncryptWriter = new BinaryWriter(filelistToEncrypt))
            {
                filelistToEncrypt.Seek(0, SeekOrigin.Begin);

                filelistToEncryptWriter.BaseStream.Position = 16;
                filelistToEncryptWriter.WriteBytesUInt32(filelistDataSize, true);

                filelistToEncryptWriter.BaseStream.Position = (uint)filelistToEncrypt.Length - 16;
                filelistToEncryptWriter.WriteBytesUInt32(filelistDataSize, false);
            }
        }

        // Encrypt the filelist file
        CryptFilelist.ProcessFilelist(CryptActions.E, repackVariables.NewFilelistFile);
        NativeLogger.Info("Finished encrypting new filelist");
    }
}