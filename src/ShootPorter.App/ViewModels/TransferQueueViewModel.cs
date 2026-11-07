using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShootPorter.Core.Download;
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
        UpdateFilterChips();
        ReadyToTransfer = FilterChips.First(c => c.Key == TransferFilterKeys.Ready).Count;
        NameClashes = FilterChips.First(c => c.Key == TransferFilterKeys.Conflicts).Count;
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

    public void AddFiles(IReadOnlyCollection<TransferFileItem> files)
    {
        _allFiles.AddRange(files);
        TotalFiles = _allFiles.Count;
        RecalculateStats();
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
                TransferFilterKeys.Downloaded => filtered.Where(f => f.Status is FileTransferStatus.Completed or FileTransferStatus.AlreadyDownloaded),
                TransferFilterKeys.Conflicts => filtered.Where(f => f.Status == FileTransferStatus.NameClash),
                TransferFilterKeys.Downloading => filtered.Where(f => f.Status == FileTransferStatus.Downloading),
                TransferFilterKeys.Errors => filtered.Where(f => f.Status == FileTransferStatus.Error),
                _ => filtered
            };
        }

        // Status changes during a download call this per file; rebuilding an unchanged list resets the view.
        var result = filtered.ToList();
        if (result.SequenceEqual(Files))
            return;

        Files.Clear();
        foreach (var file in result)
            Files.Add(file);
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
        var counts = new int[Enum.GetValues<FileTransferStatus>().Length];
        foreach (var file in _allFiles)
            counts[(int)file.Status]++;

        foreach (var chip in FilterChips)
        {
            chip.Count = chip.Key switch
            {
                TransferFilterKeys.All => _allFiles.Count,
                TransferFilterKeys.Ready => counts[(int)FileTransferStatus.Pending],
                TransferFilterKeys.Downloaded => counts[(int)FileTransferStatus.Completed] + counts[(int)FileTransferStatus.AlreadyDownloaded],
                TransferFilterKeys.Conflicts => counts[(int)FileTransferStatus.NameClash],
                TransferFilterKeys.Downloading => counts[(int)FileTransferStatus.Downloading],
                TransferFilterKeys.Errors => counts[(int)FileTransferStatus.Error],
                _ => 0
            };
            chip.IsActive = chip.Key == _selectedFilterKey;
        }
    }
}
