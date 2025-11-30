namespace WhiteBinTools.Crypto;

internal static class CryptoBase
{
    private static uint BlockCounterEval { get; set; }

    private static uint BlockCounterFval { get; set; }

    public static void BlockCounterSetup(uint blockCounter, ref uint xorTableOffset)
    {
        var blockCounterLowerMost = (ushort)(blockCounter & 0xFFFF);

        blockCounterLowerMost >>= 3;
        blockCounterLowerMost <<= 3;
        xorTableOffset = (uint)blockCounterLowerMost & 0xF8;

        var blockCounterABshiftVal = (long)blockCounter << 10;
        var blockCounterCDshiftVal = (long)blockCounter << 20;
        var blockCounterEFshiftVal = (long)blockCounter << 30;

        var blockCounterAval = (uint)(blockCounterABshiftVal & 0xFFFFFFFF);
        var blockCounterBval = (uint)(blockCounterABshiftVal >> 32);

        var blockCounterCval = (uint)(blockCounterCDshiftVal & 0xFFFFFFFF);
        var blockCounterDval = (uint)(blockCounterCDshiftVal >> 32);

        blockCounterCval |= blockCounterAval;
        blockCounterDval |= blockCounterBval;

        BlockCounterEval = (uint)(blockCounterEFshiftVal & 0xFFFFFFFF);
        BlockCounterFval = (uint)(blockCounterEFshiftVal >> 32);

        BlockCounterEval |= blockCounter;
        BlockCounterFval |= 0;

        BlockCounterEval |= blockCounterCval;
        BlockCounterFval |= blockCounterDval;
    }

    public static void XORblockSetup(byte[] xorTable, uint tableOffset, ref uint xorBlockLowerVal, ref uint xorBlockHigherVal)
    {
        var currentXORblock = new[] { xorTable[tableOffset + 0], xorTable[tableOffset + 1], 
            xorTable[tableOffset + 2], xorTable[tableOffset + 3], xorTable[tableOffset + 4], 
            xorTable[tableOffset + 5], xorTable[tableOffset + 6], xorTable[tableOffset + 7] };

        xorBlockLowerVal = BitConverter.ToUInt32(currentXORblock, 0);
        xorBlockHigherVal = BitConverter.ToUInt32(currentXORblock, 4);
    }

    public static void SpecialKeySetup(ref uint carryFlag, ref long specialKey1, ref long specialKey2)
    {
        carryFlag = BlockCounterEval > ~0xA1652347 ? (uint)1 : (uint)0;

        specialKey1 = (long)BlockCounterEval + 0xA1652347;
        specialKey2 = (long)BlockCounterFval + carryFlag;
    }
}