using System.Runtime.InteropServices;

namespace WhiteBinTools.Native;

public static class NativeStructs
{

    [StructLayout(LayoutKind.Sequential)]
    public struct FileEntry
    {
        public int ChunkIndex;
        public ulong FileCode;
        public int FileTypeId; // Used for ff132
        public IntPtr FilePath; // char* (UTF-8)
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FileEntryList
    {
        public IntPtr Items; // FileEntry*
        public int Count;
    }
}