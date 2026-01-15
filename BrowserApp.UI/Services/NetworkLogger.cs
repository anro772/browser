using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using BrowserApp.Core.Interfaces;
using BrowserApp.Core.Models;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;

namespace BrowserApp.UI.Services;

/// <summary>
/// Background network request logger using Channel for non-blocking writes.
/// Batches database writes for optimal performance.
/// </summary>
public class NetworkLogger : INetworkLogger
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Channel<NetworkRequest> _channel;
    private readonly CancellationTokenSource _cts;
    private Task? _processingTask;
    private bool _isDisposed;

    private const int BatchSize = 100;
    private const int ChannelCapacity = 10000;

    public NetworkLogger(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _cts = new CancellationTokenSource();

        // Bounded channel with drop-oldest policy to prevent memory issues
        _channel = Channel.CreateBounded<NetworkRequest>(
            new BoundedChannelOptions(ChannelCapacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleWriter = false,
                SingleReader = true
            });
    }

    /// <inheritdoc/>
    public Task StartAsync()
    {
        _processingTask = ProcessRequestsAsync(_cts.Token);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task StopAsync()
    {
        _cts.Cancel();
        _channel.Writer.Complete();

        if (_processingTask != null)
        {
            try
            {
                await _processingTask;
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
        }
    }

    /// <inheritdoc/>
    public async Task LogRequestAsync(NetworkRequest request)
    {
        if (_isDisposed) return;

        try
        {
            await _channel.Writer.WriteAsync(request, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Shutting down, ignore
        }
        catch (ChannelClosedException)
        {
            // Channel closed, ignore
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<NetworkRequest>> GetRecentRequestsAsync(int count)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INetworkLogRepository>();

        var entities = await repository.GetRecentAsync(count);
        return entities.Select(MapToModel);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<NetworkRequest>> GetFilteredRequestsAsync(
        NetworkRequestFilter filter,
        string? currentPageHost = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INetworkLogRepository>();

        var entities = await repository.GetByFilterAsync(filter, currentPageHost);
        return entities.Select(MapToModel);
    }

    /// <inheritdoc/>
    public async Task<NetworkLogStats> GetStatsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INetworkLogRepository>();

        var total = await repository.GetCountAsync();
        var blocked = await repository.GetBlockedCountAsync();
        var totalBytes = await repository.GetTotalSizeAsync();

        // Format saved bytes (blocked requests assumed to save their full size)
        var formattedSaved = FormatBytes(totalBytes);

        return new NetworkLogStats(total, blocked, totalBytes, formattedSaved);
    }

    /// <inheritdoc/>
    public async Task ClearAllAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INetworkLogRepository>();
        await repository.ClearAllAsync();
    }

    /// <summary>
    /// Background task that processes requests from the channel.
    /// Batches writes every second or when batch size is reached.
    /// </summary>
    private async Task ProcessRequestsAsync(CancellationToken ct)
    {
        var buffer = new List<NetworkRequest>(BatchSize);
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        try
        {
            while (!ct.IsCancellationRequested)
            {
                // Collect requests until batch size or timeout
                var timerTask = timer.WaitForNextTickAsync(ct).AsTask();

                while (buffer.Count < BatchSize)
                {
                    if (_channel.Reader.TryRead(out var request))
                    {
                        buffer.Add(request);
                    }
                    else
                    {
                        // No more items available, wait for timer or new item
                        var readTask = _channel.Reader.WaitToReadAsync(ct).AsTask();
                        var completedTask = await Task.WhenAny(timerTask, readTask);

                        if (completedTask == timerTask)
                        {
                            // Timer elapsed, flush buffer
                            break;
                        }
                    }
                }

                // Flush buffer if we have items
                if (buffer.Count > 0)
                {
                    await FlushBufferAsync(buffer);
                    buffer.Clear();
                }

                // Wait for next timer tick if we didn't hit it yet
                if (!timerTask.IsCompleted)
                {
                    await timerTask;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Shutting down
        }
        finally
        {
            // Flush any remaining items
            while (_channel.Reader.TryRead(out var request))
            {
                buffer.Add(request);
            }

            if (buffer.Count > 0)
            {
                await FlushBufferAsync(buffer);
            }
        }
    }

    /// <summary>
    /// Writes a batch of requests to the database.
    /// </summary>
    private async Task FlushBufferAsync(List<NetworkRequest> buffer)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<INetworkLogRepository>();

            var entities = buffer.Select(MapToEntity).ToList();
            await repository.AddBatchAsync(entities);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"NetworkLogger flush error: {ex.Message}");
        }
    }

    /// <summary>
    /// Maps a NetworkRequest model to a database entity.
    /// </summary>
    private static NetworkLogEntity MapToEntity(NetworkRequest request)
    {
        return new NetworkLogEntity
        {
            Url = request.Url,
            Method = request.Method,
            StatusCode = request.StatusCode,
            ResourceType = request.ResourceType,
            ContentType = request.ContentType,
            Size = request.Size,
            WasBlocked = request.WasBlocked,
            BlockedByRuleId = request.BlockedByRuleId,
            Timestamp = request.Timestamp
        };
    }

    /// <summary>
    /// Maps a database entity to a NetworkRequest model.
    /// </summary>
    private static NetworkRequest MapToModel(NetworkLogEntity entity)
    {
        return new NetworkRequest
        {
            Url = entity.Url,
            Method = entity.Method,
            StatusCode = entity.StatusCode,
            ResourceType = entity.ResourceType,
            ContentType = entity.ContentType,
            Size = entity.Size,
            WasBlocked = entity.WasBlocked,
            BlockedByRuleId = entity.BlockedByRuleId,
            Timestamp = entity.Timestamp
        };
    }

    /// <summary>
    /// Formats bytes as a human-readable string.
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        return bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
            _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
        };
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;

        _isDisposed = true;
        await StopAsync();
        _cts.Dispose();

        GC.SuppressFinalize(this);
    }
}
