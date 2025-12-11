using System.Runtime.InteropServices;

namespace WhiteBinTools.Native;

public static class NativeStructs
{

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct FileEntry
    {
        public int ChunkIndex;
        public ulong FileCode;
        public int FileTypeId; // Used for ff132
        public byte* FilePath; // char* (UTF-8)
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct FileEntryList
    {
        public FileEntry* Items; // FileEntry*
        public int Count;
    }

    
}