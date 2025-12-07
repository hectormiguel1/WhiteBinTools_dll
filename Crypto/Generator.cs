namespace WhiteBinTools.Crypto;

internal static class Generator
{
    public static byte[] GenerateXoRTable(byte[] seedArray, bool logDisplay)
    {
        var xorTable = new byte[264];

        Array.Reverse(seedArray);

        var seedHalfA = BitConverter.ToUInt32(seedArray, 0);
        var seedHalfB = BitConverter.ToUInt32(seedArray, 4);

        seedHalfA = (seedHalfA << 0x08) | (seedHalfA >> 0x18);
        seedHalfB = (seedHalfB >> 0x10) | (seedHalfB << 0x10);

        var xorBlock = BitConverter.GetBytes(seedHalfB).Concat(BitConverter.GetBytes(seedHalfA)).ToArray();
        xorBlock[0] += 0x45;

        // Loop 1
        var i = 1;
        while (i < 8)
        {
            var tmp = xorBlock[i] + 0xD4 + xorBlock[i - 1];
            tmp ^= (xorBlock[i - 1]) << 2;
            tmp ^= 0x45;

            xorBlock[i] = (byte)tmp;
            i++;
        }

        Array.ConstrainedCopy(xorBlock, 0, xorTable, 0, xorBlock.Length);


        Log.Debug($"Block 0: {xorBlock[0]:X2} {xorBlock[1]:X2} {xorBlock[2]:X2} {xorBlock[3]:X2} " +
                  $"{xorBlock[4]:X2} {xorBlock[5]:X2} {xorBlock[6]:X2} {xorBlock[7]:X2}");


        // Loop 2
        i = 1;
        var previousXorBlock = BitConverter.ToUInt64(xorBlock, 0);
        var copyIndex = 8;

        while (i < 0x21)
        {
            var blockHalfA = (uint)(previousXorBlock & 0xFFFFFFFF);
            var blockHalfB = (uint)(previousXorBlock >> 32);

            var a = 5 * previousXorBlock;
            a ^= (ulong)blockHalfB << 32;

            ulong tmpBlockHalfA = (uint)(blockHalfA ^ a);
            ulong tmpBlockHalfB = (uint)(a >> 32);

            a = blockHalfA | (a & 0xFFFFFFFF00000000);

            var xorBlockHalfA = (uint)(a ^ tmpBlockHalfA);
            tmpBlockHalfB ^= blockHalfB;
            var xorBlockHalfB = (uint)tmpBlockHalfB;

            xorBlock = BitConverter.GetBytes(xorBlockHalfA).Concat(BitConverter.GetBytes(xorBlockHalfB)).ToArray();

            Array.ConstrainedCopy(xorBlock, 0, xorTable, copyIndex, xorBlock.Length);


            Log.Debug($"Block {i}: {xorBlock[0]:X2} {xorBlock[1]:X2} {xorBlock[2]:X2} {xorBlock[3]:X2} " +
                      $"{xorBlock[4]:X2} {xorBlock[5]:X2} {xorBlock[6]:X2} {xorBlock[7]:X2}");


            previousXorBlock = BitConverter.ToUInt64(xorBlock, 0);

            i++;
            copyIndex += 8;
        }

        //File.WriteAllBytes("KeysDump", xorTable);

        return xorTable;
    }
}