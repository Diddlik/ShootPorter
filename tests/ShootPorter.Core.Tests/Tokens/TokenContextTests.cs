using ShootPorter.Core.Metadata;
using ShootPorter.Core.Tokens;

namespace ShootPorter.Core.Tests.Tokens;

/// <summary>
/// Tests for <see cref="TokenContext.ForFile"/>.
/// </summary>
public sealed class TokenContextTests
{
    [Fact]
    public void WhenBuiltForFileThenPathPartsAndMetadataAreMapped()
    {
        var path = Path.Combine("card", "DCIM", "100CANON", "IMG_0042.CR3");
        var metadata = new FileMetadata { FilePath = path, CameraModel = "EOS R5", CameraSerialNumber = "123" };
        var date = new DateTimeOffset(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);

        var context = TokenContext.ForFile(path, metadata, date, "JOB", 5, new Dictionary<string, string>());

        Assert.Equal(
            ("IMG_0042", ".CR3", "0042", "100CANON", "100", "EOS R5", "123", 5),
            (context.OriginalFileName, context.Extension, context.ImageNumber, context.SourceFolderName,
                context.SourceFolderNumber, context.CameraModel, context.CameraSerialNumber, context.SequenceNumber));
    }
}
