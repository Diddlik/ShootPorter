using ShootPorter.Core.Discovery;

namespace ShootPorter.Core.Tests.Discovery;

/// <summary>
/// Tests for <see cref="SupportedFormats"/> extension checking.
/// </summary>
public sealed class SupportedFormatsTests
{
    [Theory]
    [InlineData(".jpg")]
    [InlineData(".jpeg")]
    [InlineData(".cr2")]
    [InlineData(".cr3")]
    [InlineData(".nef")]
    [InlineData(".arw")]
    [InlineData(".dng")]
    [InlineData(".raf")]
    [InlineData(".orf")]
    [InlineData(".pef")]
    [InlineData(".gpr")]
    [InlineData(".tiff")]
    [InlineData(".tif")]
    public void WhenCheckingImageExtensionThenIsImage(string ext)
    {
        Assert.True(SupportedFormats.IsImage(ext));
        Assert.True(SupportedFormats.IsSupported(ext));
        Assert.False(SupportedFormats.IsVideo(ext));
    }

    [Theory]
    [InlineData(".mov")]
    [InlineData(".mp4")]
    [InlineData(".avi")]
    [InlineData(".crm")]
    [InlineData(".mts")]
    [InlineData(".3gp")]
    [InlineData(".mxf")]
    public void WhenCheckingVideoExtensionThenIsVideo(string ext)
    {
        Assert.True(SupportedFormats.IsVideo(ext));
        Assert.True(SupportedFormats.IsSupported(ext));
        Assert.False(SupportedFormats.IsImage(ext));
    }

    [Theory]
    [InlineData(".txt")]
    [InlineData(".pdf")]
    [InlineData(".exe")]
    [InlineData(".png")]
    [InlineData(".doc")]
    public void WhenCheckingUnsupportedExtensionThenNotSupported(string ext)
    {
        Assert.False(SupportedFormats.IsSupported(ext));
    }

    [Theory]
    [InlineData(".JPG")]
    [InlineData(".Mov")]
    [InlineData(".NEF")]
    public void WhenCheckingExtensionCaseInsensitiveThenStillMatches(string ext)
    {
        Assert.True(SupportedFormats.IsSupported(ext));
    }

    [Fact]
    public void WhenGettingCategoryForImageThenReturnsImage()
    {
        Assert.Equal(FileCategory.Image, SupportedFormats.GetCategory(".jpg"));
    }

    [Fact]
    public void WhenGettingCategoryForVideoThenReturnsVideo()
    {
        Assert.Equal(FileCategory.Video, SupportedFormats.GetCategory(".mp4"));
    }

    [Fact]
    public void WhenGettingCategoryForUnsupportedThenReturnsNull()
    {
        Assert.Null(SupportedFormats.GetCategory(".txt"));
    }
}
