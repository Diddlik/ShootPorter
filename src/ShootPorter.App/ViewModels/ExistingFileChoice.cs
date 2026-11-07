namespace ShootPorter.App.ViewModels;

/// <summary>
/// The user's answer when a download target already exists. <see cref="Cancel"/> is the default, so closing the dialog cancels.
/// </summary>
public enum ExistingFileChoice
{
    Cancel,
    Overwrite,
    OverwriteAll,
    AddSuffix,
    AddSuffixAll,
}
