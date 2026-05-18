using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShootPorter.Core.Discovery;

namespace ShootPorter.App.ViewModels;

/// <summary>
/// ViewModel for the source file browser page.
/// </summary>
public partial class SourceBrowserViewModel : ViewModelBase
{
    private readonly FileSystemScanner _scanner = new();
    private readonly List<SourceFileItem> _allFiles = new();

    [ObservableProperty]
    private string _sourcePath = string.Empty;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _statusText = "Select a source folder to begin.";

    [ObservableProperty]
    private string? _selectedDate;

    public ObservableCollection<SourceFileItem> Files { get; } = new();
    public ObservableCollection<string> Dates { get; } = new();

    partial void OnSelectedDateChanged(string? value)
    {
        FilterFiles();
    }

    private void FilterFiles()
    {
        Files.Clear();
        var filtered = string.IsNullOrEmpty(SelectedDate) || SelectedDate == "All"
            ? _allFiles
            : _allFiles.Where(f => f.FileDate?.ToString("yyyy/MM/dd") == SelectedDate);

        foreach (var item in filtered)
        {
            Files.Add(item);
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ScanAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(SourcePath))
        {
            StatusText = "Please enter a source folder path.";
            return;
        }

        IsScanning = true;
        _allFiles.Clear();
        Files.Clear();
        Dates.Clear();
        StatusText = "Scanning...";
        var count = 0;

        try
        {
            await foreach (var file in _scanner.ScanDirectoryAsync(SourcePath, recursive: true, cancellationToken))
            {
                var fileDate = File.GetCreationTime(file.FullPath); // Basic date extraction for now
                var item = new SourceFileItem
                {
                    FileName = file.FileName,
                    Extension = file.Extension,
                    SizeBytes = file.SizeBytes,
                    FileType = file.Category == FileCategory.Image ? "Image" : "Video",
                    FullPath = file.FullPath,
                    Status = "New file",
                    FileDate = fileDate,
                    DownloadPath = "" // Would be set by orchestrator/routing
                };

                _allFiles.Add(item);
                count++;
            }

            var uniqueDates = _allFiles
                .Where(f => f.FileDate.HasValue)
                .Select(f => f.FileDate!.Value.ToString("yyyy/MM/dd"))
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Dates.Add("All");
                foreach (var d in uniqueDates)
                {
                    Dates.Add(d);
                }
                SelectedDate = "All"; // Will trigger FilterFiles
            });

            StatusText = $"{count} files found.";
        }
        catch (OperationCanceledException)
        {
            StatusText = $"Scan cancelled. Found {count} file(s).";
        }
        catch (DirectoryNotFoundException)
        {
            StatusText = "Directory not found.";
        }
        finally
        {
            IsScanning = false;
        }
    }
}
