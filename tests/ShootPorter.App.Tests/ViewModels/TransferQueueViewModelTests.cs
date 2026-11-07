using ShootPorter.App.ViewModels;

namespace ShootPorter.App.Tests.ViewModels;

/// <summary>
/// Tests for <see cref="TransferQueueViewModel"/> statistics and filtering.
/// </summary>
public sealed class TransferQueueViewModelTests
{
    private static TransferFileItem Item(string name, FileTransferStatus status = FileTransferStatus.Pending) => new()
    {
        FileName = name,
        SourcePath = Path.Combine("card", name),
        CaptureDate = new DateTime(2026, 11, 7),
        Status = status,
    };

    [Fact]
    public void WhenFilesAreAddedThenStatsAndChipCountsAreUpdated()
    {
        var vm = new TransferQueueViewModel();

        vm.AddFiles([Item("a.jpg"), Item("b.jpg", FileTransferStatus.NameClash), Item("c.jpg", FileTransferStatus.Error)]);

        Assert.Equal((3, 1, 1), (vm.TotalFiles, vm.ReadyToTransfer, vm.NameClashes));
        Assert.Equal(1, vm.FilterChips.Single(c => c.Key == TransferFilterKeys.Errors).Count);
    }

    [Fact]
    public void WhenStatusFilterIsSelectedThenOnlyMatchingFilesAreShown()
    {
        var vm = new TransferQueueViewModel();
        vm.AddFiles([Item("a.jpg"), Item("b.jpg", FileTransferStatus.Completed)]);

        vm.SelectedFilter = "Downloaded";

        Assert.Equal(["b.jpg"], vm.Files.Select(f => f.FileName));
    }

    [Fact]
    public void WhenStatsAreRecalculatedWithoutChangesThenTheVisibleListIsNotRebuilt()
    {
        var vm = new TransferQueueViewModel();
        vm.AddFiles([Item("a.jpg"), Item("b.jpg")]);
        var resets = 0;
        vm.Files.CollectionChanged += (_, _) => resets++;

        vm.AllFiles[0].Progress = 50;
        vm.RecalculateStats();

        Assert.Equal(0, resets);
    }

    [Fact]
    public void WhenSearchTextIsSetThenFilesAreFilteredByName()
    {
        var vm = new TransferQueueViewModel();
        vm.AddFiles([Item("IMG_0001.jpg"), Item("DSC_0002.jpg")]);

        vm.SearchText = "dsc";

        Assert.Equal(["DSC_0002.jpg"], vm.Files.Select(f => f.FileName));
    }
}
