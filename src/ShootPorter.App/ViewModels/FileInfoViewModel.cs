using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using ShootPorter.Core.Metadata;

namespace ShootPorter.App.ViewModels;

/// <summary>
/// ViewModel for the file info popup displaying EXIF metadata.
/// </summary>
public partial class FileInfoViewModel : ViewModelBase
{
    private readonly IMetadataReader _metadataReader = new MetadataReader();

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _captureDateTime;

    [ObservableProperty]
    private string? _cameraInfo;

    [ObservableProperty]
    private string? _cameraSerial;

    [ObservableProperty]
    private string? _dimensions;

    [ObservableProperty]
    private string? _iso;

    [ObservableProperty]
    private string? _aperture;

    [ObservableProperty]
    private string? _shutterSpeed;

    [ObservableProperty]
    private string? _gpsCoordinates;

    [ObservableProperty]
    private string? _gpsAltitude;

    [ObservableProperty]
    private string? _artist;

    [ObservableProperty]
    private string? _copyright;

    [ObservableProperty]
    private string? _keywords;

    public async Task LoadMetadataAsync(string filePath)
    {
        FilePath = filePath;
        FileName = System.IO.Path.GetFileName(filePath);
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var metadata = await _metadataReader.ReadMetadataAsync(filePath);
            PopulateFromMetadata(metadata);
        }
        catch (System.Exception ex)
        {
            ErrorMessage = $"Failed to read metadata: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void PopulateFromMetadata(FileMetadata metadata)
    {
        CaptureDateTime = metadata.CaptureDateTime?.ToString("yyyy-MM-dd HH:mm:ss");
        
        CameraInfo = (metadata.CameraManufacturer, metadata.CameraModel) switch
        {
            (not null, not null) => $"{metadata.CameraManufacturer} {metadata.CameraModel}",
            (not null, null) => metadata.CameraManufacturer,
            (null, not null) => metadata.CameraModel,
            _ => null
        };

        CameraSerial = metadata.CameraSerialNumber;

        Dimensions = (metadata.ImageWidth, metadata.ImageHeight) switch
        {
            (not null, not null) => $"{metadata.ImageWidth} × {metadata.ImageHeight}",
            _ => null
        };

        Iso = metadata.IsoSpeed?.ToString();
        Aperture = metadata.Aperture;
        ShutterSpeed = metadata.ShutterSpeed;

        GpsCoordinates = (metadata.GpsLatitude, metadata.GpsLongitude) switch
        {
            (not null, not null) => $"{metadata.GpsLatitude:F6}, {metadata.GpsLongitude:F6}",
            _ => null
        };

        GpsAltitude = metadata.GpsAltitude?.ToString("F1") + " m";
        if (metadata.GpsAltitude is null) GpsAltitude = null;

        Artist = metadata.Artist;
        Copyright = metadata.Copyright;

        Keywords = metadata.Keywords.Count > 0 
            ? string.Join(", ", metadata.Keywords) 
            : null;
    }
}
