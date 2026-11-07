using ShootPorter.Core.Profiles;

namespace ShootPorter.Core.Tests.Profiles;

/// <summary>
/// Tests for <see cref="CameraMappingResolver"/>.
/// </summary>
public sealed class CameraMappingResolverTests
{
    private static readonly SavedCameraMapping[] Mappings =
    [
        new() { CameraModel = "EOS R5", T8Value = "R5", T9Value = "Main" },
        new() { CameraModel = "EOS R5 (123)", T8Value = "R5-A", T9Value = "Body A" },
    ];

    [Fact]
    public void WhenModelMatchesThenT8AndT9AreReturned()
    {
        var result = CameraMappingResolver.Resolve(Mappings, "eos r5", "123", identifyBySerial: false);

        Assert.Equal(("R5", "Main"), (result["T8"], result["T9"]));
    }

    [Fact]
    public void WhenIdentifyingBySerialThenSerialSpecificMappingIsUsed()
    {
        var result = CameraMappingResolver.Resolve(Mappings, "EOS R5", "123", identifyBySerial: true);

        Assert.Equal("R5-A", result["T8"]);
    }

    [Fact]
    public void WhenNoMappingMatchesThenResultIsEmpty()
    {
        var result = CameraMappingResolver.Resolve(Mappings, "Z 9", null, identifyBySerial: true);

        Assert.Empty(result);
    }
}
