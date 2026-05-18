using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Data.Converters;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShootPorter.Core.Metadata;
using ShootPorter.Core.Profiles;
using ShootPorter.Core.Tokens;

namespace ShootPorter.App.ViewModels;

/// <summary>
/// ViewModel for application settings and preferences with tabbed navigation.
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    public static readonly IValueConverter TabBgConverter = new TabBackgroundConverter();
    public static readonly IValueConverter TabFgConverter = new TabForegroundConverter();
    public static readonly IValueConverter TabBorderConverter = new TabBorderBrushConverter();

    private static string SettingsDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ShootPorter");

    private static string SettingsFilePath =>
        Path.Combine(SettingsDirectory, "settings.json");

    private readonly AppSettingsStore _settingsStore = new(SettingsFilePath);
    private readonly TokenRegistry _tokenRegistry = new();
    private readonly TokenParser _tokenParser;
    private readonly IMetadataReader _metadataReader = new MetadataReader();

    private static readonly DateTimeOffset FallbackSampleDateTime = new(2026, 3, 29, 14, 9, 0, TimeSpan.Zero);
    private const string FallbackOriginalFileName = "DSC_0312";
    private const string FallbackExtension = ".jpg";

    private FileMetadata? _previewFileMetadata;
    private bool _isLoadingProfile;
    private bool _isLoadingSettings;
    private CancellationTokenSource? _saveCts;

    [ObservableProperty]
    private string _previewFileName = string.Empty;

    [ObservableProperty]
    private string _currentTab = "DownloadPath";

    // Download Path Tab
    [ObservableProperty]
    private string _destinationTemplate = @"{Y}{m}{D}_{H}{M}{S}_{T8}_{r}";

    [ObservableProperty]
    private string _destinationRoot = @"R:\New Fotos\";

    [ObservableProperty]
    private string _jobCode = string.Empty;

    [ObservableProperty]
    private bool _promptForJobCode;

    [ObservableProperty]
    private int _sequenceStart = 1;

    [ObservableProperty]
    private string _currentProfileName = "STANDARD PHOTOS";

    [ObservableProperty]
    private ProfileItem? _selectedProfile;

    // General Tab
    [ObservableProperty]
    private bool _checkForUpdates = true;

    [ObservableProperty]
    private bool _isAutoDetect;

    [ObservableProperty]
    private bool _isCameraUsb;

    [ObservableProperty]
    private bool _isCardReader = true;

    [ObservableProperty]
    private string _driveLetter = "J";

    [ObservableProperty]
    private bool _disablePtpDownloads = true;

    [ObservableProperty]
    private bool _useCaptureTime = true;

    [ObservableProperty]
    private bool _autoEjectMedia = true;

    [ObservableProperty]
    private bool _saveReadOnly;

    [ObservableProperty]
    private bool _autoRotateJpegs;

    [ObservableProperty]
    private bool _renameJpeToJpg;

    [ObservableProperty]
    private bool _convertToLowerCase;

    [ObservableProperty]
    private bool _addIptcXmpData;

    // Camera Mapping Tab
    [ObservableProperty]
    private bool _identifyCameraBySerial = true;

    [ObservableProperty]
    private string _detectedCamera = "X-T2";

    // Custom Actions Tab
    [ObservableProperty]
    private bool _enableCustomButton = true;

    [ObservableProperty]
    private string _customButtonCaption = "Exif+Move";

    [ObservableProperty]
    private string _customScriptPath = @"P:\AppZ\Scripts\exif.cmd";

    // Legacy properties for compatibility
    [ObservableProperty]
    private string _backupPath1 = string.Empty;

    [ObservableProperty]
    private string _backupPath2 = string.Empty;

    [ObservableProperty]
    private int _maxParallelism = 2;

    [ObservableProperty]
    private bool _verifyAfterCopy = true;

    [ObservableProperty]
    private bool _autoDeleteSource;

    [ObservableProperty]
    private string _selectedSourcePath = string.Empty;

    public ObservableCollection<ProfileItem> Profiles { get; } = [];
    public ObservableCollection<TokenInfo> Tokens { get; } = [];
    public ObservableCollection<CameraMappingItem> CameraMappings { get; } = [];
    public ObservableCollection<string> RecentSourcePaths { get; } = [];

    public bool IsDownloadPathTab => CurrentTab == "DownloadPath";
    public bool IsGeneralTab => CurrentTab == "General";
    public bool IsCameraMappingTab => CurrentTab == "CameraMapping";
    public bool IsCustomActionsTab => CurrentTab == "CustomActions";

    public string PreviewPath => GeneratePreviewPath();

    public SettingsViewModel()
    {
        _tokenParser = new TokenParser(_tokenRegistry);
        LoadDefaultProfiles();
        LoadTokens();
        LoadCameraMappings();
    }

    public async Task InitializeAsync()
    {
        var settings = await _settingsStore.LoadAsync().ConfigureAwait(false);
        ApplySettings(settings);
    }

    partial void OnDestinationTemplateChanged(string value)
    {
        if (!_isLoadingProfile && SelectedProfile is { } profile)
        {
            profile.NamingTemplate = value;
        }
        OnPropertyChanged(nameof(PreviewPath));
        ScheduleSave();
    }

    partial void OnDestinationRootChanged(string value)
    {
        if (!_isLoadingProfile && SelectedProfile is { } profile)
        {
            profile.Path = value;
        }
        OnPropertyChanged(nameof(PreviewPath));
        ScheduleSave();
    }

    partial void OnJobCodeChanged(string value)
    {
        OnPropertyChanged(nameof(PreviewPath));
    }

    partial void OnCheckForUpdatesChanged(bool value) => ScheduleSave();
    partial void OnIsAutoDetectChanged(bool value) => ScheduleSave();
    partial void OnIsCameraUsbChanged(bool value) => ScheduleSave();
    partial void OnIsCardReaderChanged(bool value) => ScheduleSave();
    partial void OnDriveLetterChanged(string value) => ScheduleSave();
    partial void OnDisablePtpDownloadsChanged(bool value) => ScheduleSave();
    partial void OnUseCaptureTimeChanged(bool value) => ScheduleSave();
    partial void OnAutoEjectMediaChanged(bool value) => ScheduleSave();
    partial void OnSaveReadOnlyChanged(bool value) => ScheduleSave();
    partial void OnAutoRotateJpegsChanged(bool value) => ScheduleSave();
    partial void OnRenameJpeToJpgChanged(bool value) => ScheduleSave();
    partial void OnConvertToLowerCaseChanged(bool value) => ScheduleSave();
    partial void OnAddIptcXmpDataChanged(bool value) => ScheduleSave();
    partial void OnIdentifyCameraBySerialChanged(bool value) => ScheduleSave();
    partial void OnEnableCustomButtonChanged(bool value) => ScheduleSave();
    partial void OnCustomButtonCaptionChanged(string value) => ScheduleSave();
    partial void OnCustomScriptPathChanged(string value) => ScheduleSave();
    partial void OnBackupPath1Changed(string value) => ScheduleSave();
    partial void OnBackupPath2Changed(string value) => ScheduleSave();
    partial void OnMaxParallelismChanged(int value) => ScheduleSave();
    partial void OnVerifyAfterCopyChanged(bool value) => ScheduleSave();
    partial void OnAutoDeleteSourceChanged(bool value) => ScheduleSave();
    partial void OnSelectedSourcePathChanged(string value) => ScheduleSave();

    partial void OnSelectedProfileChanged(ProfileItem? value)
    {
        if (value is null) return;

        _isLoadingProfile = true;
        try
        {
            foreach (var profile in Profiles)
            {
                profile.IsActive = profile == value;
            }

            DestinationRoot = value.Path;
            DestinationTemplate = value.NamingTemplate;
            CurrentProfileName = value.Name.ToUpperInvariant();
        }
        finally
        {
            _isLoadingProfile = false;
        }

        ScheduleSave();
    }

    private string GeneratePreviewPath()
    {
        try
        {
            var meta = _previewFileMetadata;
            var hasRealFile = meta is not null;

            var captureDateTime = (hasRealFile ? meta!.CaptureDateTime : null) ?? FallbackSampleDateTime;
            var originalFileName = hasRealFile
                ? Path.GetFileNameWithoutExtension(meta!.FilePath)
                : FallbackOriginalFileName;
            var extension = hasRealFile
                ? Path.GetExtension(meta!.FilePath)
                : FallbackExtension;
            var sourceFolderName = hasRealFile && Path.GetDirectoryName(meta!.FilePath) is { } dir
                ? Path.GetFileName(dir)
                : string.Empty;

            var cameraMappings = BuildCameraMappingsDictionary(meta?.CameraModel, meta?.CameraSerialNumber);

            var context = new TokenContext(
                CaptureDateTime: captureDateTime,
                OriginalFileName: originalFileName,
                Extension: extension,
                JobCode: string.IsNullOrEmpty(JobCode) ? "JOB001" : JobCode,
                SequenceNumber: SequenceStart,
                CustomTokens: new Dictionary<string, string>())
            {
                ImageNumber = TokenContext.ExtractImageNumber(originalFileName),
                SourceFolderName = sourceFolderName,
                SourceFolderNumber = TokenContext.ExtractImageNumber(sourceFolderName),
                CameraManufacturer = meta?.CameraManufacturer,
                CameraModel = meta?.CameraModel,
                CameraSerialNumber = meta?.CameraSerialNumber,
                IsoSpeed = meta?.IsoSpeed,
                Aperture = meta?.Aperture,
                ShutterSpeed = meta?.ShutterSpeed,
                Copyright = meta?.Copyright,
                Owner = meta?.Artist,
                CameraMappings = cameraMappings,
            };

            var generatedName = _tokenParser.Parse(DestinationTemplate, context);
            return Path.Combine(DestinationRoot, generatedName + extension);
        }
        catch
        {
            return Path.Combine(DestinationRoot, "preview_error.jpg");
        }
    }

    public async Task SetPreviewFileAsync(string filePath)
    {
        try
        {
            _previewFileMetadata = await _metadataReader.ReadMetadataAsync(filePath);
            PreviewFileName = Path.GetFileName(filePath);
        }
        catch
        {
            _previewFileMetadata = new FileMetadata { FilePath = filePath };
            PreviewFileName = Path.GetFileName(filePath);
        }

        OnPropertyChanged(nameof(PreviewPath));
    }

    partial void OnCurrentTabChanged(string value)
    {
        OnPropertyChanged(nameof(IsDownloadPathTab));
        OnPropertyChanged(nameof(IsGeneralTab));
        OnPropertyChanged(nameof(IsCameraMappingTab));
        OnPropertyChanged(nameof(IsCustomActionsTab));
    }

    [RelayCommand]
    private void NavigateTab(string tab)
    {
        CurrentTab = tab;
    }

    [RelayCommand]
    private void ResetSequence()
    {
        SequenceStart = 1;
    }

    [ObservableProperty]
    private bool _isSavingToProfile;

    [ObservableProperty]
    private string _saveToProfileName = string.Empty;

    [RelayCommand]
    private void SaveToProfile()
    {
        SaveToProfileName = string.Empty;
        IsSavingToProfile = true;
    }

    [RelayCommand]
    private void ConfirmSaveToProfile()
    {
        if (string.IsNullOrWhiteSpace(SaveToProfileName))
            return;

        var newProfile = new ProfileItem(SelectProfile, DeleteProfile, OnProfileSaved)
        {
            Name = SaveToProfileName.Trim(),
            Path = DestinationRoot,
            NamingTemplate = DestinationTemplate,
            IsActive = false,
            CanDelete = true
        };
        Profiles.Add(newProfile);
        SelectProfile(newProfile);

        IsSavingToProfile = false;
        SaveToProfileName = string.Empty;
    }

    [RelayCommand]
    private void CancelSaveToProfile()
    {
        IsSavingToProfile = false;
        SaveToProfileName = string.Empty;
    }

    [RelayCommand]
    private void NewProfile()
    {
        var newProfile = new ProfileItem(SelectProfile, DeleteProfile, OnProfileSaved)
        {
            Name = "New Profile",
            Path = @"C:\Photos\",
            NamingTemplate = "{Y}{m}{D}_{o}",
            IsActive = false,
            CanDelete = true,
            IsEditing = true
        };
        Profiles.Add(newProfile);
    }

    private void SelectProfile(ProfileItem selected)
    {
        SelectedProfile = selected;
    }

    private void DeleteProfile(ProfileItem profile)
    {
        if (!profile.CanDelete || profile.IsActive)
            return;

        Profiles.Remove(profile);
    }

    [RelayCommand]
    private void DeleteSelectedProfile()
    {
        if (SelectedProfile is not { CanDelete: true } toDelete)
            return;

        Profiles.Remove(toDelete);
        SelectedProfile = Profiles.Count > 0 ? Profiles[0] : null;
    }

    private void OnProfileSaved(ProfileItem profile)
    {
        if (profile.IsActive)
        {
            DestinationRoot = profile.Path;
            DestinationTemplate = profile.NamingTemplate;
            CurrentProfileName = profile.Name.ToUpperInvariant();
        }
        ScheduleSave();
    }

    private void LoadDefaultProfiles()
    {
        Profiles.Add(new ProfileItem(SelectProfile, DeleteProfile, OnProfileSaved)
        {
            Name = "Standard Photos",
            Path = @"R:\New Fotos\",
            NamingTemplate = "{Y}{m}{D}_{H}{M}{S}_{T8}_{r}",
            IsActive = true,
            CanDelete = false
        });
        Profiles.Add(new ProfileItem(SelectProfile, DeleteProfile, OnProfileSaved)
        {
            Name = "Work / Clients",
            Path = @"N:\Work\Projects\",
            NamingTemplate = "{J}_{Y}{m}{D}_{seq#}",
            IsActive = false,
            CanDelete = true
        });
        Profiles.Add(new ProfileItem(SelectProfile, DeleteProfile, OnProfileSaved)
        {
            Name = "Long-term Archive",
            Path = @"S:\Archive\Raw\",
            NamingTemplate = @"{Y}\{m}\{Y}{m}{D}_{o}",
            IsActive = false,
            CanDelete = true
        });

        if (Profiles.Count > 0)
        {
            SelectedProfile = Profiles[0];
        }
    }

    private void ApplySettings(AppSettings settings)
    {
        _isLoadingSettings = true;
        try
        {
            CheckForUpdates = settings.CheckForUpdates;
            IsAutoDetect = settings.IsAutoDetect;
            IsCameraUsb = settings.IsCameraUsb;
            IsCardReader = settings.IsCardReader;
            DriveLetter = settings.DriveLetter;
            DisablePtpDownloads = settings.DisablePtpDownloads;
            UseCaptureTime = settings.UseCaptureTime;
            AutoEjectMedia = settings.AutoEjectMedia;
            SaveReadOnly = settings.SaveReadOnly;
            AutoRotateJpegs = settings.AutoRotateJpegs;
            RenameJpeToJpg = settings.RenameJpeToJpg;
            ConvertToLowerCase = settings.ConvertToLowerCase;
            AddIptcXmpData = settings.AddIptcXmpData;
            IdentifyCameraBySerial = settings.IdentifyCameraBySerial;
            EnableCustomButton = settings.EnableCustomButton;
            CustomButtonCaption = settings.CustomButtonCaption;
            CustomScriptPath = settings.CustomScriptPath;
            BackupPath1 = settings.BackupPath1;
            BackupPath2 = settings.BackupPath2;
            MaxParallelism = settings.MaxParallelism;
            VerifyAfterCopy = settings.VerifyAfterCopy;
            AutoDeleteSource = settings.AutoDeleteSource;
            SelectedSourcePath = settings.SelectedSourcePath;
            RecentSourcePaths.Clear();
            foreach (var path in settings.RecentSourcePaths.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                RecentSourcePaths.Add(path);
            }

            if (settings.Profiles.Count > 0)
            {
                Profiles.Clear();
                foreach (var saved in settings.Profiles)
                {
                    Profiles.Add(new ProfileItem(SelectProfile, DeleteProfile, OnProfileSaved)
                    {
                        Name = saved.Name,
                        Path = saved.Path,
                        NamingTemplate = saved.NamingTemplate,
                        CanDelete = saved.CanDelete,
                        IsActive = false
                    });
                }
            }

            if (settings.CameraMappings.Count > 0)
            {
                CameraMappings.Clear();
                foreach (var saved in settings.CameraMappings)
                {
                    CameraMappings.Add(new CameraMappingItem(RemoveCameraMapping)
                    {
                        CameraModel = saved.CameraModel,
                        T8Value = saved.T8Value,
                        T9Value = saved.T9Value
                    });
                }
            }

            var match = Profiles.FirstOrDefault(
                p => p.Name.Equals(settings.SelectedProfileName, StringComparison.OrdinalIgnoreCase));
            SelectedProfile = match ?? (Profiles.Count > 0 ? Profiles[0] : null);
        }
        finally
        {
            _isLoadingSettings = false;
        }
    }

    private AppSettings CaptureSettings() => new()
    {
        CheckForUpdates = CheckForUpdates,
        IsAutoDetect = IsAutoDetect,
        IsCameraUsb = IsCameraUsb,
        IsCardReader = IsCardReader,
        DriveLetter = DriveLetter,
        DisablePtpDownloads = DisablePtpDownloads,
        UseCaptureTime = UseCaptureTime,
        AutoEjectMedia = AutoEjectMedia,
        SaveReadOnly = SaveReadOnly,
        AutoRotateJpegs = AutoRotateJpegs,
        RenameJpeToJpg = RenameJpeToJpg,
        ConvertToLowerCase = ConvertToLowerCase,
        AddIptcXmpData = AddIptcXmpData,
        IdentifyCameraBySerial = IdentifyCameraBySerial,
        EnableCustomButton = EnableCustomButton,
        CustomButtonCaption = CustomButtonCaption,
        CustomScriptPath = CustomScriptPath,
        BackupPath1 = BackupPath1,
        BackupPath2 = BackupPath2,
        MaxParallelism = MaxParallelism,
        VerifyAfterCopy = VerifyAfterCopy,
        AutoDeleteSource = AutoDeleteSource,
        SelectedSourcePath = SelectedSourcePath,
        RecentSourcePaths = RecentSourcePaths.ToList(),
        SelectedProfileName = SelectedProfile?.Name ?? string.Empty,
        Profiles = Profiles.Select(p => new SavedProfile
        {
            Name = p.Name,
            Path = p.Path,
            NamingTemplate = p.NamingTemplate,
            CanDelete = p.CanDelete
        }).ToList(),
        CameraMappings = CameraMappings.Select(c => new SavedCameraMapping
        {
            CameraModel = c.CameraModel,
            T8Value = c.T8Value,
            T9Value = c.T9Value
        }).ToList()
    };

    private void ScheduleSave()
    {
        if (_isLoadingSettings) return;

        _saveCts?.Cancel();
        _saveCts = new CancellationTokenSource();
        var ct = _saveCts.Token;
        _ = SaveAfterDelayAsync(ct);
    }

    private async Task SaveAfterDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            await _settingsStore.SaveAsync(CaptureSettings(), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Debounce cancelled by a newer save request
        }
    }

    public async Task SaveSettingsNowAsync()
    {
        _saveCts?.Cancel();
        await _settingsStore.SaveAsync(CaptureSettings()).ConfigureAwait(false);
    }

    public void SetSourceHistory(string selectedSourcePath, IEnumerable<string> recentSourcePaths)
    {
        SelectedSourcePath = selectedSourcePath;

        RecentSourcePaths.Clear();
        foreach (var path in recentSourcePaths.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            RecentSourcePaths.Add(path);
        }

        ScheduleSave();
    }

    private void LoadTokens()
    {
        // Date/Time tokens
        Tokens.Add(new TokenInfo("{d}", "Date YYMMDD"));
        Tokens.Add(new TokenInfo("{t}", "Time HHMMSS"));
        Tokens.Add(new TokenInfo("{y}", "2-digit year"));
        Tokens.Add(new TokenInfo("{Y}", "4-digit year"));
        Tokens.Add(new TokenInfo("{m}", "Month (01-12)"));
        Tokens.Add(new TokenInfo("{b}", "Month abbrev"));
        Tokens.Add(new TokenInfo("{B}", "Month name"));
        Tokens.Add(new TokenInfo("{D}", "Day (01-31)"));
        Tokens.Add(new TokenInfo("{j}", "Day of year"));
        Tokens.Add(new TokenInfo("{W}", "Week number"));
        Tokens.Add(new TokenInfo("{WI}", "ISO week"));
        Tokens.Add(new TokenInfo("{a}", "Weekday abbrev"));
        Tokens.Add(new TokenInfo("{A}", "Weekday name"));
        Tokens.Add(new TokenInfo("{P}", "Quarter (1-4)"));
        Tokens.Add(new TokenInfo("{H}", "Hour (00-23)"));
        Tokens.Add(new TokenInfo("{I}", "Hour (01-12)"));
        Tokens.Add(new TokenInfo("{M}", "Minutes"));
        Tokens.Add(new TokenInfo("{S}", "Seconds"));
        Tokens.Add(new TokenInfo("{p}", "AM/PM"));

        // Camera tokens
        Tokens.Add(new TokenInfo("{c}", "Serial number"));
        Tokens.Add(new TokenInfo("{i}", "ISO"));
        Tokens.Add(new TokenInfo("{K1}", "Focal length"));
        Tokens.Add(new TokenInfo("{K2}", "Aperture"));
        Tokens.Add(new TokenInfo("{K3}", "Shutter speed"));
        Tokens.Add(new TokenInfo("{O}", "Owner"));
        Tokens.Add(new TokenInfo("{T}", "Camera model"));
        Tokens.Add(new TokenInfo("{T2}", "Full camera"));
        Tokens.Add(new TokenInfo("{T8}", "Camera mapping"));

        // File tokens
        Tokens.Add(new TokenInfo("{e}", "Extension"));
        Tokens.Add(new TokenInfo("{f}", "First 3 chars"));
        Tokens.Add(new TokenInfo("{F}", "Filename"));
        Tokens.Add(new TokenInfo("{o}", "Folder name"));
        Tokens.Add(new TokenInfo("{r}", "Image number"));

        // Job/Sequence tokens
        Tokens.Add(new TokenInfo("{J}", "Job code"));
        Tokens.Add(new TokenInfo("{seq#}", "Sequence"));
        Tokens.Add(new TokenInfo("{seq#4}", "Seq (4 digits)"));
        Tokens.Add(new TokenInfo("{l}", "Uniqueness"));
        Tokens.Add(new TokenInfo("{R}", "Daily count"));
    }

    [RelayCommand]
    private void AddCameraMapping()
    {
        if (string.IsNullOrWhiteSpace(DetectedCamera))
            return;

        var existing = CameraMappings.FirstOrDefault(
            m => m.CameraModel.Equals(DetectedCamera, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
            return;

        CameraMappings.Add(new CameraMappingItem(RemoveCameraMapping)
        {
            CameraModel = DetectedCamera,
            T8Value = DetectedCamera,
            T9Value = DetectedCamera
        });

        ScheduleSave();
    }

    private void RemoveCameraMapping(CameraMappingItem mapping)
    {
        CameraMappings.Remove(mapping);
        ScheduleSave();
    }

    /// <summary>
    /// Updates the detected camera display from EXIF metadata of scanned files.
    /// </summary>
    public void UpdateDetectedCamera(string? cameraModel, string? serialNumber)
    {
        if (IdentifyCameraBySerial && !string.IsNullOrWhiteSpace(serialNumber))
        {
            DetectedCamera = $"{cameraModel} ({serialNumber})";
        }
        else if (!string.IsNullOrWhiteSpace(cameraModel))
        {
            DetectedCamera = cameraModel;
        }
    }

    /// <summary>
    /// Builds a camera mappings dictionary for token resolution from the current mapping entries.
    /// Matches the given camera identifier against stored mappings.
    /// </summary>
    public IReadOnlyDictionary<string, string> BuildCameraMappingsDictionary(string? cameraModel, string? serialNumber)
    {
        var result = new Dictionary<string, string>();

        if (CameraMappings.Count == 0)
            return result;

        var identifier = IdentifyCameraBySerial && !string.IsNullOrWhiteSpace(serialNumber)
            ? $"{cameraModel} ({serialNumber})"
            : cameraModel;

        if (string.IsNullOrWhiteSpace(identifier))
            return result;

        var match = CameraMappings.FirstOrDefault(
            m => m.CameraModel.Equals(identifier, StringComparison.OrdinalIgnoreCase));

        if (match is not null)
        {
            result["T8"] = match.T8Value;
            result["T9"] = match.T9Value;
        }

        return result;
    }

    private void LoadCameraMappings()
    {
        CameraMappings.Add(new CameraMappingItem(RemoveCameraMapping)
        {
            CameraModel = "Default mapping",
            T8Value = "{T1}",
            T9Value = "{T1}"
        });
    }
}

public partial class ProfileItem : ObservableObject
{
    public static readonly IValueConverter BorderConverter = new ProfileBorderConverter();
    public static readonly IValueConverter ButtonTextConverter = new ProfileButtonTextConverter();
    public static readonly IValueConverter ButtonBgConverter = new ProfileButtonBgConverter();
    public static readonly IValueConverter ButtonFgConverter = new ProfileButtonFgConverter();

    private readonly Action<ProfileItem>? _onSelect;
    private readonly Action<ProfileItem>? _onDelete;
    private readonly Action<ProfileItem>? _onSave;

    private string _backupName = string.Empty;
    private string _backupPath = string.Empty;
    private string _backupNamingTemplate = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _path = string.Empty;

    [ObservableProperty]
    private string _namingTemplate = string.Empty;

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private bool _canDelete = true;

    [ObservableProperty]
    private bool _isEditing;

    public ProfileItem()
    {
    }

    public ProfileItem(Action<ProfileItem> onSelect, Action<ProfileItem> onDelete, Action<ProfileItem> onSave)
    {
        _onSelect = onSelect;
        _onDelete = onDelete;
        _onSave = onSave;
    }

    [RelayCommand]
    private void Select()
    {
        _onSelect?.Invoke(this);
    }

    [RelayCommand]
    private void Delete()
    {
        _onDelete?.Invoke(this);
    }

    [RelayCommand]
    private void Edit()
    {
        _backupName = Name;
        _backupPath = Path;
        _backupNamingTemplate = NamingTemplate;
        IsEditing = true;
    }

    [RelayCommand]
    private void SaveEdit()
    {
        IsEditing = false;
        _onSave?.Invoke(this);
    }

    [RelayCommand]
    private void CancelEdit()
    {
        Name = _backupName;
        Path = _backupPath;
        NamingTemplate = _backupNamingTemplate;
        IsEditing = false;
    }
}

public record TokenInfo(string Token, string Description);

/// <summary>
/// Represents a single camera-to-token mapping entry in the settings UI.
/// </summary>
public partial class CameraMappingItem : ObservableObject
{
    private readonly Action<CameraMappingItem>? _onDelete;

    [ObservableProperty]
    private string _cameraModel = string.Empty;

    [ObservableProperty]
    private string _t8Value = string.Empty;

    [ObservableProperty]
    private string _t9Value = string.Empty;

    public string MappingDisplay => $"{{T8}}={T8Value}  |  {{T9}}={T9Value}";

    public CameraMappingItem() { }

    public CameraMappingItem(Action<CameraMappingItem> onDelete)
    {
        _onDelete = onDelete;
    }

    [RelayCommand]
    private void Delete()
    {
        _onDelete?.Invoke(this);
    }
}

// Converters for tab navigation
public class TabBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string current && parameter is string tab)
        {
            return current == tab
                ? new SolidColorBrush(Color.Parse("#eff6ff"))
                : Brushes.Transparent;
        }
        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class TabForegroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string current && parameter is string tab)
        {
            return current == tab
                ? new SolidColorBrush(Color.Parse("#2563eb"))
                : new SolidColorBrush(Color.Parse("#475569"));
        }
        return new SolidColorBrush(Color.Parse("#475569"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class TabBorderBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string current && parameter is string tab)
        {
            return current == tab
                ? new SolidColorBrush(Color.Parse("#2563eb"))
                : Brushes.Transparent;
        }
        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// Converters for profile cards
public class ProfileBorderConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive)
        {
            return isActive
                ? new SolidColorBrush(Color.Parse("#2563eb"))
                : new SolidColorBrush(Color.Parse("#e2e8f0"));
        }
        return new SolidColorBrush(Color.Parse("#e2e8f0"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class ProfileButtonTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool isActive && isActive ? "ACTIVE" : "SELECT";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class ProfileButtonBgConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive)
        {
            return isActive
                ? new SolidColorBrush(Color.Parse("#2563eb"))
                : Brushes.Transparent;
        }
        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class ProfileButtonFgConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive)
        {
            return isActive
                ? Brushes.White
                : new SolidColorBrush(Color.Parse("#475569"));
        }
        return new SolidColorBrush(Color.Parse("#475569"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
