namespace FontSymbolsViewer;

internal static class TtfCodepointReader
{
    public static IReadOnlyList<int> ReadCodepoints(string fontPath)
    {
        var resolvedPath = ResolvePath(fontPath);
        var data = File.ReadAllBytes(resolvedPath);
        var cmapOffset = FindTable(data, "cmap");
        if (cmapOffset < 0)
        {
            throw new InvalidDataException("The font does not contain a cmap table.");
        }

        var subtableOffsets = ReadCmapSubtableOffsets(data, cmapOffset)
            .OrderByDescending(offset => ReadUInt16(data, offset) == 12)
            .ThenByDescending(offset => ReadUInt16(data, offset) == 4)
            .Distinct()
            .ToList();

        var codepoints = new SortedSet<int>();
        foreach (var offset in subtableOffsets)
        {
            var format = ReadUInt16(data, offset);
            if (format == 4)
            {
                AddFormat4Codepoints(data, offset, codepoints);
            }
            else if (format == 12)
            {
                AddFormat12Codepoints(data, offset, codepoints);
            }
        }

        return codepoints
            .Where(codePoint => codePoint > 0 && codePoint <= 0x10FFFF)
            .ToArray();
    }

    private static string ResolvePath(string fontPath)
    {
        var candidates = new[]
        {
            fontPath,
            Path.Combine(AppContext.BaseDirectory, fontPath),
            Path.Combine(Directory.GetCurrentDirectory(), fontPath)
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        throw new FileNotFoundException($"Font file not found: {fontPath}", fontPath);
    }

    private static int FindTable(byte[] data, string tag)
    {
        var tableCount = ReadUInt16(data, 4);
        for (var i = 0; i < tableCount; i++)
        {
            var tableOffset = 12 + i * 16;
            if (ReadTag(data, tableOffset) == tag)
            {
                return (int)ReadUInt32(data, tableOffset + 8);
            }
        }

        return -1;
    }

    private static IEnumerable<int> ReadCmapSubtableOffsets(byte[] data, int cmapOffset)
    {
        var subtableCount = ReadUInt16(data, cmapOffset + 2);
        for (var i = 0; i < subtableCount; i++)
        {
            var recordOffset = cmapOffset + 4 + i * 8;
            var subtableOffset = (int)ReadUInt32(data, recordOffset + 4);
            yield return cmapOffset + subtableOffset;
        }
    }

    private static void AddFormat4Codepoints(byte[] data, int offset, ISet<int> codepoints)
    {
        var length = ReadUInt16(data, offset + 2);
        var end = offset + length;
        var segCount = ReadUInt16(data, offset + 6) / 2;
        var endCodeOffset = offset + 14;
        var startCodeOffset = endCodeOffset + 2 * segCount + 2;
        var idDeltaOffset = startCodeOffset + 2 * segCount;
        var idRangeOffsetOffset = idDeltaOffset + 2 * segCount;

        for (var segment = 0; segment < segCount; segment++)
        {
            var startCode = ReadUInt16(data, startCodeOffset + 2 * segment);
            var endCode = ReadUInt16(data, endCodeOffset + 2 * segment);
            var idDelta = ReadInt16(data, idDeltaOffset + 2 * segment);
            var rangeOffsetAddress = idRangeOffsetOffset + 2 * segment;
            var idRangeOffset = ReadUInt16(data, rangeOffsetAddress);

            if (startCode == 0xFFFF && endCode == 0xFFFF)
            {
                continue;
            }

            for (var codePoint = startCode; codePoint <= endCode; codePoint++)
            {
                var glyphId = idRangeOffset == 0
                    ? (codePoint + idDelta) & 0xFFFF
                    : ReadGlyphId(data, end, codePoint, startCode, idDelta, rangeOffsetAddress, idRangeOffset);

                if (glyphId != 0)
                {
                    codepoints.Add(codePoint);
                }
            }
        }
    }

    private static int ReadGlyphId(
        byte[] data,
        int subtableEnd,
        int codePoint,
        int startCode,
        short idDelta,
        int rangeOffsetAddress,
        int idRangeOffset)
    {
        var glyphIndexAddress = rangeOffsetAddress + idRangeOffset + 2 * (codePoint - startCode);
        if (glyphIndexAddress < 0 || glyphIndexAddress + 1 >= subtableEnd)
        {
            return 0;
        }

        var glyphId = ReadUInt16(data, glyphIndexAddress);
        return glyphId == 0 ? 0 : (glyphId + idDelta) & 0xFFFF;
    }

    private static void AddFormat12Codepoints(byte[] data, int offset, ISet<int> codepoints)
    {
        var groupCount = ReadUInt32(data, offset + 12);
        var groupOffset = offset + 16;

        for (var group = 0; group < groupCount; group++)
        {
            var recordOffset = groupOffset + (int)group * 12;
            var startCharCode = ReadUInt32(data, recordOffset);
            var endCharCode = ReadUInt32(data, recordOffset + 4);
            var startGlyphId = ReadUInt32(data, recordOffset + 8);

            for (var codePoint = startCharCode; codePoint <= endCharCode && codePoint <= 0x10FFFF; codePoint++)
            {
                if (startGlyphId + codePoint - startCharCode != 0)
                {
                    codepoints.Add((int)codePoint);
                }
            }
        }
    }

    private static string ReadTag(byte[] data, int offset) =>
        new(new[]
        {
            (char)data[offset],
            (char)data[offset + 1],
            (char)data[offset + 2],
            (char)data[offset + 3]
        });

    private static ushort ReadUInt16(byte[] data, int offset) =>
        (ushort)((data[offset] << 8) | data[offset + 1]);

    private static short ReadInt16(byte[] data, int offset) =>
        unchecked((short)ReadUInt16(data, offset));

    private static uint ReadUInt32(byte[] data, int offset) =>
        ((uint)data[offset] << 24) |
        ((uint)data[offset + 1] << 16) |
        ((uint)data[offset + 2] << 8) |
        data[offset + 3];
}
