using System.Buffers.Binary;
using System.Text;
using ShootPorter.Core.Metadata;

namespace ShootPorter.Core.Tests.Metadata;

/// <summary>
/// Tests for <see cref="MetadataReader"/> using minimal hand-built JPEG and MP4 files.
/// </summary>
public sealed class MetadataReaderTests
{
    [Fact]
    public async Task WhenSerialNumberIsInExifSubIfdThenItIsRead()
    {
        var path = await WriteTempFileAsync(".jpg", BuildJpegWithSubIfdSerial("ABC12345"));
        try
        {
            var metadata = await new MetadataReader().ReadMetadataAsync(path);

            Assert.Equal("ABC12345", metadata.CameraSerialNumber);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task WhenVideoHasMovieHeaderThenCaptureDateIsCreationTimeInUtc()
    {
        var created = new DateTime(2024, 5, 1, 10, 0, 0, DateTimeKind.Utc);
        var path = await WriteTempFileAsync(".mp4", BuildMp4(created));
        try
        {
            var metadata = await new MetadataReader().ReadMetadataAsync(path);

            Assert.Equal(created, metadata.CaptureDateTime?.UtcDateTime);
            Assert.Equal(TimeZoneInfo.Local.GetUtcOffset(created), metadata.CaptureDateTime?.Offset);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task WhenVideoTimeIsLocalThenMovieHeaderDateIsTakenAsLocalWallClock()
    {
        var created = new DateTime(2024, 5, 1, 10, 0, 0, DateTimeKind.Utc);
        var path = await WriteTempFileAsync(".mp4", BuildMp4(created));
        try
        {
            var metadata = await new MetadataReader(videoTimeIsLocal: true).ReadMetadataAsync(path);

            Assert.Equal(new DateTime(2024, 5, 1, 10, 0, 0), metadata.CaptureDateTime?.DateTime);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task WhenSonyClipSidecarExistsThenCaptureDateIsItsCameraClock()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"sp-meta-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            var path = Path.Combine(dir, "C0001.MP4");
            await File.WriteAllBytesAsync(path, BuildMp4(new DateTime(2026, 11, 5, 15, 44, 59, DateTimeKind.Utc)));
            await File.WriteAllTextAsync(Path.Combine(dir, "C0001M01.XML"),
                """<?xml version="1.0" encoding="UTF-8"?><NonRealTimeMeta xmlns="urn:schemas-professionalDisc:nonRealTimeMeta:ver.2.20"><CreationDate value="2026-11-05T17:44:59+02:00"/></NonRealTimeMeta>""");

            var metadata = await new MetadataReader().ReadMetadataAsync(path);

            Assert.Equal(new DateTime(2026, 11, 5, 17, 44, 59), metadata.CaptureDateTime?.DateTime);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static async Task<string> WriteTempFileAsync(string extension, byte[] content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"sp-meta-{Guid.NewGuid():N}{extension}");
        await File.WriteAllBytesAsync(path, content);
        return path;
    }

    // Little-endian TIFF: IFD0 holds only the Exif SubIFD pointer; the SubIFD holds BodySerialNumber.
    private static byte[] BuildJpegWithSubIfdSerial(string serial)
    {
        var value = Encoding.ASCII.GetBytes(serial + "\0");
        const int subIfdOffset = 26;
        const int valueOffset = 44;

        var tiff = new byte[valueOffset + value.Length];
        "II*\0"u8.CopyTo(tiff);
        BinaryPrimitives.WriteUInt32LittleEndian(tiff.AsSpan(4), 8);
        WriteIfdEntry(tiff, 8, tag: 0x8769, type: 4, count: 1, value: subIfdOffset);
        WriteIfdEntry(tiff, subIfdOffset, tag: 0xA431, type: 2, count: (uint)value.Length, value: valueOffset);
        value.CopyTo(tiff, valueOffset);

        using var jpeg = new MemoryStream();
        jpeg.Write([0xFF, 0xD8, 0xFF, 0xE1]);
        var segmentLength = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(segmentLength, (ushort)(2 + 6 + tiff.Length));
        jpeg.Write(segmentLength);
        jpeg.Write("Exif\0\0"u8);
        jpeg.Write(tiff);
        jpeg.Write([0xFF, 0xD9]);
        return jpeg.ToArray();
    }

    private static void WriteIfdEntry(byte[] tiff, int ifdOffset, ushort tag, ushort type, uint count, uint value)
    {
        var span = tiff.AsSpan(ifdOffset);
        BinaryPrimitives.WriteUInt16LittleEndian(span, 1);
        BinaryPrimitives.WriteUInt16LittleEndian(span[2..], tag);
        BinaryPrimitives.WriteUInt16LittleEndian(span[4..], type);
        BinaryPrimitives.WriteUInt32LittleEndian(span[6..], count);
        BinaryPrimitives.WriteUInt32LittleEndian(span[10..], value);
        BinaryPrimitives.WriteUInt32LittleEndian(span[14..], 0);
    }

    private static byte[] BuildMp4(DateTime createdUtc)
    {
        var seconds = (uint)(createdUtc - new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

        var mvhd = new byte[108];
        BinaryPrimitives.WriteUInt32BigEndian(mvhd, 108);
        "mvhd"u8.CopyTo(mvhd.AsSpan(4));
        BinaryPrimitives.WriteUInt32BigEndian(mvhd.AsSpan(12), seconds);
        BinaryPrimitives.WriteUInt32BigEndian(mvhd.AsSpan(16), seconds);
        BinaryPrimitives.WriteUInt32BigEndian(mvhd.AsSpan(20), 1000);
        BinaryPrimitives.WriteUInt32BigEndian(mvhd.AsSpan(28), 0x00010000);
        BinaryPrimitives.WriteUInt16BigEndian(mvhd.AsSpan(32), 0x0100);
        BinaryPrimitives.WriteUInt32BigEndian(mvhd.AsSpan(104), 1);

        using var mp4 = new MemoryStream();
        var header = new byte[8];
        BinaryPrimitives.WriteUInt32BigEndian(header, 20);
        "ftyp"u8.CopyTo(header.AsSpan(4));
        mp4.Write(header);
        mp4.Write("isom\0\0\u0002\0isom"u8);
        BinaryPrimitives.WriteUInt32BigEndian(header, (uint)(8 + mvhd.Length));
        "moov"u8.CopyTo(header.AsSpan(4));
        mp4.Write(header);
        mp4.Write(mvhd);
        return mp4.ToArray();
    }
}
