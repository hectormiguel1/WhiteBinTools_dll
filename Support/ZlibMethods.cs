using System.IO.Compression;

namespace WhiteBinTools.Support;

internal static class ZlibMethods
{
    public static void ZlibDecompress(Stream cmpStreamName, Stream outStreamName)
    {
        using var decompressor = new ZLibStream(cmpStreamName, CompressionMode.Decompress);
        decompressor.CopyTo(outStreamName);
    }

    public static byte[] ZlibDecompressBuffer(MemoryStream cmpStreamName)
    {
        return Decompress(cmpStreamName.ToArray());
    }


    public static byte[] ZlibCompress(string fileToCmp)
    {
        if (!File.Exists(fileToCmp)) throw new FileNotFoundException("File not found", fileToCmp);
            
        var dataToCompressBuffer = File.ReadAllBytes(fileToCmp);
        return Compress(dataToCompressBuffer);
    }

    public static byte[] ZlibCompressBuffer(byte[] dataToCmp)
    {
        return Compress(dataToCmp);
    }
        
    // ---------------------------------------------------------
    // Internal Helpers (Native Implementation)
    // ---------------------------------------------------------

    /// <summary>
    /// Core compression logic using System.IO.Compression.ZLibStream
    /// </summary>
    private static byte[] Compress(byte[] data)
    {
        using var outputStream = new MemoryStream();
            
        // CompressionLevel.SmallestSize is roughly equivalent to "BestCompression"
        using (var zlib = new ZLibStream(outputStream, CompressionLevel.SmallestSize))
        {
            zlib.Write(data, 0, data.Length);
        }
            
        return outputStream.ToArray();
    }

    /// <summary>
    /// Core decompression logic using System.IO.Compression.ZLibStream
    /// </summary>
    private static byte[] Decompress(byte[] data)
    {
        if (data.Length == 0) return [];

        using var inputStream = new MemoryStream(data);
        using var outputStream = new MemoryStream();
            
        using (var zlib = new ZLibStream(inputStream, CompressionMode.Decompress))
        {
            zlib.CopyTo(outputStream);
        }
            
        return outputStream.ToArray();
    }

    /// <summary>
    /// Optimized decompression if you know the expected size (avoid resizing).
    /// </summary>
    public static byte[] Decompress(byte[] data, int expectedSize)
    {
        if (data.Length == 0) return [];

        using var inputStream = new MemoryStream(data);
        using var zlib = new ZLibStream(inputStream, CompressionMode.Decompress);
            
        var buffer = new byte[expectedSize];
        var totalBytesRead = 0;
            
        // Loop ensures we read everything even if the stream returns chunks
        while (totalBytesRead < expectedSize)
        {
            var bytesRead = zlib.Read(buffer, totalBytesRead, expectedSize - totalBytesRead);
            if (bytesRead == 0) break;
            totalBytesRead += bytesRead;
        }

        return buffer;
    }
}