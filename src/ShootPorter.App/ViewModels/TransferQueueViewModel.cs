using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShootPorter.Core.Metadata;

namespace ShootPorter.App.ViewModels;

/// <summary>
/// ViewModel for the Transfer Queue page displaying files ready for download.
/// </summary>
public partial class TransferQueueViewModel : ViewModelBase
{
    private readonly List<TransferFileItem> _allFiles = [];
    private bool _suppressDateFilter;
    private string _selectedFilterKey = TransferFilterKeys.All;

    [ObservableProperty]
    private int _overallProgressPercent;

    [ObservableProperty]
    private int _completedFiles;

    [ObservableProperty]
    private int _totalFiles;

    [ObservableProperty]
    private int _readyToTransfer;

    [ObservableProperty]
    private int _nameClashes;

    [ObservableProperty]
    private string _destinationPath = string.Empty;

    [ObservableProperty]
    private string _downloadSpeedText = string.Empty;

    [ObservableProperty]
    private string _estimatedTimeRemainingText = string.Empty;

    [ObservableProperty]
    private bool _isDownloadActive;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedFilter = "All";

    public ObservableCollection<DateGroupItem> DateGroups { get; } = [];
    public ObservableCollection<TransferFileItem> Files { get; } = [];
    public IReadOnlyList<TransferFileItem> AllFiles => _allFiles;
    public ObservableCollection<TransferFilterChip> FilterChips { get; } = [];
    public ObservableCollection<string> FilterOptions { get; } = ["All", "Ready", "Conflicts", "Downloaded", "Downloading", "Errors"];

    public TransferQueueViewModel()
    {
        FilterChips.Add(new TransferFilterChip(TransferFilterKeys.All, "All", SelectFilter));
        FilterChips.Add(new TransferFilterChip(TransferFilterKeys.Ready, "Ready", SelectFilter));
        FilterChips.Add(new TransferFilterChip(TransferFilterKeys.Conflicts, "Conflicts", SelectFilter));
        FilterChips.Add(new TransferFilterChip(TransferFilterKeys.Downloaded, "Downloaded", SelectFilter));
        FilterChips.Add(new TransferFilterChip(TransferFilterKeys.Downloading, "Downloading", SelectFilter));
        FilterChips.Add(new TransferFilterChip(TransferFilterKeys.Errors, "Errors", SelectFilter));
        UpdateFilterChips();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyAllFilters();
    }

    partial void OnSelectedFilterChanged(string value)
    {
        _selectedFilterKey = value switch
        {
            "Pending" or "Ready" => TransferFilterKeys.Ready,
            "Completed" or "Downloaded" => TransferFilterKeys.Downloaded,
            "Name Clash" or "Conflicts" => TransferFilterKeys.Conflicts,
            "Downloading" => TransferFilterKeys.Downloading,
            "Errors" => TransferFilterKeys.Errors,
            _ => TransferFilterKeys.All
        };
        UpdateFilterChips();
        ApplyAllFilters();
    }

    public void RecalculateStats()
    {
        ReadyToTransfer = _allFiles.Count(f => f.Status == FileTransferStatus.Pending);
        NameClashes = _allFiles.Count(f => f.Status == FileTransferStatus.NameClash);
        UpdateFilterChips();
        ApplyAllFilters();
    }

    [RelayCommand]
    private void SelectAllDates()
    {
        _suppressDateFilter = true;
        foreach (var group in DateGroups)
            group.IsSelected = true;
        _suppressDateFilter = false;
        FilterFilesBySelectedDates();
    }

    [RelayCommand]
    private void DeselectAllDates()
    {
        _suppressDateFilter = true;
        foreach (var group in DateGroups)
            group.IsSelected = false;
        _suppressDateFilter = false;
        FilterFilesBySelectedDates();
    }

    public void ClearFiles()
    {
        _allFiles.Clear();
        Files.Clear();
        DateGroups.Clear();
        TotalFiles = 0;
        ReadyToTransfer = 0;
        NameClashes = 0;
        OverallProgressPercent = 0;
        CompletedFiles = 0;
        DownloadSpeedText = string.Empty;
        EstimatedTimeRemainingText = string.Empty;
        IsDownloadActive = false;
        UpdateFilterChips();
    }

    public void AddFile(TransferFileItem file)
    {
        _allFiles.Add(file);
        TotalFiles = _allFiles.Count;
        ReadyToTransfer = _allFiles.Count(f => f.Status == FileTransferStatus.Pending);
        NameClashes = _allFiles.Count(f => f.Status == FileTransferStatus.NameClash);
        UpdateFilterChips();
        ApplyAllFilters();
    }

    public void RebuildDateGroups()
    {
        var selectedDates = DateGroups
            .Where(d => d.IsSelected)
            .Select(d => d.Date.Date)
            .ToHashSet();

        foreach (var group in DateGroups)
        {
            group.DateSelectionChanged -= OnDateSelectionChanged;
        }
        DateGroups.Clear();

        var groups = _allFiles
            .GroupBy(f => f.CaptureDate.Date)
            .OrderByDescending(g => g.Key)
            .Select(g => new DateGroupItem
            {
                Date = g.Key,
                FileCount = g.Count(),
                IsSelected = selectedDates.Count == 0 || selectedDates.Contains(g.Key)
            });

        foreach (var group in groups)
        {
            group.DateSelectionChanged += OnDateSelectionChanged;
            DateGroups.Add(group);
        }
    }

    private void OnDateSelectionChanged(object? sender, EventArgs e)
    {
        if (!_suppressDateFilter)
            FilterFilesBySelectedDates();
    }

    public void FilterFilesBySelectedDates()
    {
        var selectedDates = DateGroups
            .Where(d => d.IsSelected)
            .Select(d => d.Date.Date)
            .ToHashSet();

        if (DateGroups.Count > 0)
        {
            foreach (var file in _allFiles)
                file.IsSelected = selectedDates.Contains(file.CaptureDate.Date);
        }

        ApplyAllFilters();
    }

    private void ApplyAllFilters()
    {
        var selectedDates = DateGroups
            .Where(d => d.IsSelected)
            .Select(d => d.Date.Date)
            .ToHashSet();

        IEnumerable<TransferFileItem> filtered;
        if (selectedDates.Count > 0)
            filtered = _allFiles.Where(f => selectedDates.Contains(f.CaptureDate.Date));
        else if (DateGroups.Count > 0)
            filtered = [];
        else
            filtered = _allFiles;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();
            filtered = filtered.Where(f =>
                f.FileName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                f.DestinationPreview.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                f.SourcePath.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (_selectedFilterKey != TransferFilterKeys.All)
        {
            filtered = _selectedFilterKey switch
            {
                TransferFilterKeys.Ready => filtered.Where(f => f.Status == FileTransferStatus.Pending),
                TransferFilterKeys.Downloaded => filtered.Where(f => f.Status == FileTransferStatus.Completed),
                TransferFilterKeys.Conflicts => filtered.Where(f => f.Status == FileTransferStatus.NameClash),
                TransferFilterKeys.Downloading => filtered.Where(f => f.Status == FileTransferStatus.Downloading),
                TransferFilterKeys.Errors => filtered.Where(f => f.Status == FileTransferStatus.Error),
                _ => filtered
            };
        }

        Files.Clear();

        foreach (var file in filtered)
        {
            Files.Add(file);
        }
    }

    private void SelectFilter(TransferFilterChip chip)
    {
        _selectedFilterKey = chip.Key;
        SelectedFilter = chip.Key;
        UpdateFilterChips();
        ApplyAllFilters();
    }

    private void UpdateFilterChips()
    {
        foreach (var chip in FilterChips)
        {
            chip.Count = chip.Key switch
            {
                TransferFilterKeys.All => _allFiles.Count,
                TransferFilterKeys.Ready => _allFiles.Count(f => f.Status == FileTransferStatus.Pending),
                TransferFilterKeys.Downloaded => _allFiles.Count(f => f.Status == FileTransferStatus.Completed),
                TransferFilterKeys.Conflicts => _allFiles.Count(f => f.Status == FileTransferStatus.NameClash),
                TransferFilterKeys.Downloading => _allFiles.Count(f => f.Status == FileTransferStatus.Downloading),
                TransferFilterKeys.Errors => _allFiles.Count(f => f.Status == FileTransferStatus.Error),
                _ => 0
            };
            chip.IsActive = chip.Key == _selectedFilterKey;
        }
    }
}

public static class TransferFilterKeys
{
    public const string All = "All";
    public const string Ready = "Ready";
    public const string Conflicts = "Conflicts";
    public const string Downloaded = "Downloaded";
    public const string Downloading = "Downloading";
    public const string Errors = "Errors";
}

public partial class TransferFilterChip : ObservableObject
{
    private readonly Action<TransferFilterChip> _onSelect;

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private int _count;

    public TransferFilterChip(string key, string label, Action<TransferFilterChip> onSelect)
    {
        Key = key;
        Label = label;
        _onSelect = onSelect;
    }

    public string Key { get; }
    public string Label { get; }
    public string DisplayText => $"{Label} {Count}";

    public IBrush Background => IsActive
        ? new SolidColorBrush(Color.Parse("#2563eb"))
        : new SolidColorBrush(Color.Parse("#111827"));

    public IBrush BorderBrush => IsActive
        ? new SolidColorBrush(Color.Parse("#60a5fa"))
        : new SolidColorBrush(Color.Parse("#2c384b"));

    public IBrush Foreground => IsActive
        ? Brushes.White
        : new SolidColorBrush(Color.Parse("#cbd5e1"));

    partial void OnIsActiveChanged(bool value)
    {
        OnPropertyChanged(nameof(Background));
        OnPropertyChanged(nameof(BorderBrush));
        OnPropertyChanged(nameof(Foreground));
    }

    partial void OnCountChanged(int value)
    {
        OnPropertyChanged(nameof(DisplayText));
    }

    [RelayCommand]
    private void Select() => _onSelect(this);
}

public partial class DateGroupItem : ObservableObject
{
    [ObservableProperty]
    private DateTime _date;

    [ObservableProperty]
    private int _fileCount;

    [ObservableProperty]
    private bool _isSelected;

    public event EventHandler? DateSelectionChanged;

    public string DisplayDate => Date.ToString("dd MMM");

    partial void OnIsSelectedChanged(bool value)
    {
        DateSelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Toggle() => IsSelected = !IsSelected;
}

public enum FileTransferStatus
{
    Pending,
    Downloading,
    Completed,
    NameClash,
    Error
}

public partial class TransferFileItem : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected = true;

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private FileTransferStatus _status;

    [ObservableProperty]
    private int _progress;

    [ObservableProperty]
    private long _sizeBytes;

    [ObservableProperty]
    private DateTime _captureDate;

    [ObservableProperty]
    private string _destinationPreview = string.Empty;

    [ObservableProperty]
    private string _sourcePath = string.Empty;

    public FileMetadata? Metadata { get; init; }

    public string SizeDisplay => SizeBytes switch
    {
        < 1024 => $"{SizeBytes} B",
        < 1024 * 1024 => $"{SizeBytes / 1024.0:F1} KB",
        _ => $"{SizeBytes / (1024.0 * 1024.0):F1} MB",
    };

    public string CaptureDateDisplay => CaptureDate.ToString("dd/MM/yy");
    public string CaptureTimeDisplay => CaptureDate.ToString("HH:mm:ss");

    public string SourceFolderName => Path.GetDirectoryName(SourcePath) is { } dir
        ? Path.GetFileName(dir)
        : string.Empty;

    public string StatusText => Status switch
    {
        FileTransferStatus.Completed => "Completed",
        FileTransferStatus.Downloading => "Downloading",
        FileTransferStatus.NameClash => "Name clash",
        FileTransferStatus.Error => "Error",
        _ => "Pending"
    };

    public string StatusIcon => Status switch
    {
        FileTransferStatus.Completed => "Checkmark",
        FileTransferStatus.Downloading => "Sync",
        FileTransferStatus.NameClash => "Warning",
        FileTransferStatus.Error => "Dismiss",
        _ => "Info"
    };

    public IBrush StatusBackground => Status switch
    {
        FileTransferStatus.Completed => new SolidColorBrush(Color.Parse("#dcfce7")),
        FileTransferStatus.Downloading => new SolidColorBrush(Color.Parse("#dbeafe")),
        FileTransferStatus.NameClash => new SolidColorBrush(Color.Parse("#fef3c7")),
        FileTransferStatus.Error => new SolidColorBrush(Color.Parse("#fee2e2")),
        _ => new SolidColorBrush(Color.Parse("#f1f5f9"))
    };

    public IBrush StatusBorder => Status switch
    {
        FileTransferStatus.Completed => new SolidColorBrush(Color.Parse("#86efac")),
        FileTransferStatus.Downloading => new SolidColorBrush(Color.Parse("#93c5fd")),
        FileTransferStatus.NameClash => new SolidColorBrush(Color.Parse("#fcd34d")),
        FileTransferStatus.Error => new SolidColorBrush(Color.Parse("#fca5a5")),
        _ => new SolidColorBrush(Color.Parse("#e2e8f0"))
    };

    public IBrush StatusForeground => Status switch
    {
        FileTransferStatus.Completed => new SolidColorBrush(Color.Parse("#166534")),
        FileTransferStatus.Downloading => new SolidColorBrush(Color.Parse("#1d4ed8")),
        FileTransferStatus.NameClash => new SolidColorBrush(Color.Parse("#92400e")),
        FileTransferStatus.Error => new SolidColorBrush(Color.Parse("#991b1b")),
        _ => new SolidColorBrush(Color.Parse("#475569"))
    };

    public IBrush ProgressColor => Status switch
    {
        FileTransferStatus.Completed => new SolidColorBrush(Color.Parse("#10b981")),
        FileTransferStatus.Downloading => new SolidColorBrush(Color.Parse("#3b82f6")),
        FileTransferStatus.NameClash => new SolidColorBrush(Color.Parse("#f59e0b")),
        _ => new SolidColorBrush(Color.Parse("#cbd5e1"))
    };
}
