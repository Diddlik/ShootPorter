using System;
using System.Threading;
using System.Threading.Tasks;
using ShootPorter.Core.Download;

namespace ShootPorter.App.ViewModels;

/// <summary>
/// Asks the user what to do with existing download targets for one batch and remembers "... all" answers.
/// Parallel copies can hit conflicts at the same time, so only one question is asked at a time.
/// </summary>
public sealed class ExistingFilePrompt(Func<string, Task<ExistingFileChoice>>? ask, Action cancelBatch) : IDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private ConflictResolution? _choiceForAll;

    public async Task<ConflictResolution> ResolveAsync(string path, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_choiceForAll is { } remembered)
                return remembered;

            var choice = ask is null ? ExistingFileChoice.Cancel : await ask(path);
            switch (choice)
            {
                case ExistingFileChoice.OverwriteAll:
                    _choiceForAll = ConflictResolution.Overwrite;
                    return ConflictResolution.Overwrite;
                case ExistingFileChoice.AddSuffixAll:
                    _choiceForAll = ConflictResolution.AddSuffix;
                    return ConflictResolution.AddSuffix;
                case ExistingFileChoice.Overwrite:
                    return ConflictResolution.Overwrite;
                case ExistingFileChoice.AddSuffix:
                    return ConflictResolution.AddSuffix;
                default:
                    cancelBatch();
                    throw new OperationCanceledException(cancellationToken);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose() => _lock.Dispose();
}
