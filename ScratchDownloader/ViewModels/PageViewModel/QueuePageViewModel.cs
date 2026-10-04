using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScratchDownloader.Helper;
using ScratchDownloader.Localization;
using ScratchDownloader.Models;
using ScratchDownloader.Services;

namespace ScratchDownloader.ViewModels.PageViewModel;

public partial class QueuePageViewModel : ViewModelBase, IViewModel
{
    [ObservableProperty] private ObservableCollection<Queue> _queues = new();


    public QueuePageViewModel()
    {
        Strings.Instance.PropertyChanged += OnStringsChanged;
        InitializeQueues();
        if (Queues.FirstOrDefault() is { } firstQueue) firstQueue.IsExpanded = true;
    }

    private void OnStringsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != "Item[]")
            return;

        OnPropertyChanged(nameof(QueueDays));
        OnPropertyChanged(nameof(OperationModes));
    }

    public IReadOnlyList<QueueDaysOfWeek> QueueDays => Enum.GetValues<QueueDaysOfWeek>();
    public IReadOnlyList<OperationMode> OperationModes => Enum.GetValues<OperationMode>();
    public ObservableCollection<int> SegmentOptions { get; } = [1, 2, 4, 8, 16, 32];

    private void InitializeQueues()
    {
        foreach (var queue in SettingsService.Settings.Queues.Values)
        {
            queue.PropertyChanged += OnQueuePropertyChanged;
            Queues.Add(queue);
        }
    }

    private void OnQueuePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Queue.IsExpanded) && sender is Queue expandedQueue && expandedQueue.IsExpanded)
            foreach (var queue in Queues)
                if (queue != expandedQueue)
                    queue.IsExpanded = false;
    }

    [RelayCommand]
    private void AddQueue()
    {
        var newId = Guid.NewGuid().ToString("N");
        var newQueue = new Queue
        {
            Id = newId,
            Name = Strings.Format("NewQueueName", Queues.Count + 1),
            IsExpanded = true // Automatically expands the new queue (which collapses others)
        };
        foreach (var queue in Queues) queue.IsExpanded = false;

        newQueue.PropertyChanged += OnQueuePropertyChanged;
        Queues.Add(newQueue);


        // Persist to Settings
        SettingsService.Settings.Queues[newQueue.Id] = newQueue;
        // SettingsService.Save();
    }

    [RelayCommand]
    private void DeleteQueue(Queue? queue)
    {
        if (queue is null)
            return;
        if (queue.Id == "Main")
        {
            DialogManager.ShowMessage(MessageDialogType.Error, Strings.Get("Error"), Strings.Get("CannotDeleteMainQueue"));
            return;
        }

        DialogManager.ShowMessage(MessageDialogType.Warning, Strings.Get("DeleteQueue"),
            Strings.Format("DeleteQueueConfirmation", queue.Name),
            Strings.Get("Yes"),
            new RelayCommand(() => ConfirmDeleteQueue(queue)),
            Strings.Get("No"));
    }

    private void ConfirmDeleteQueue(Queue? queue)
    {
        if (queue is null)
            return;
        queue.PropertyChanged -= OnQueuePropertyChanged;
        Queues.Remove(queue);

        if (SettingsService.Settings.Queues.ContainsKey(queue.Id)) SettingsService.Settings.Queues.Remove(queue.Id);
        // SettingsService.Save();
        // Expand the first available queue if none are open
        if (Queues.Any() && !Queues.Any(q => q.IsExpanded)) Queues.First().IsExpanded = true;
    }

    [RelayCommand]
    private void ToggleDay(QueueDayToggleArgs? args)
    {
        if (args?.Queue is null)
            return;

        if (args.Queue.DaysOfWeek.Contains(args.Day))
            args.Queue.DaysOfWeek.Remove(args.Day);
        else
            args.Queue.DaysOfWeek.Add(args.Day);
    }

    [RelayCommand]
    private void ResetQueue(Queue? queue)
    {
        if (queue is null)
            return;

        queue.MaxConcurrentDownloads = 8;
        queue.MaxSpeedLimitInKiloBytes = 0;
        queue.StartTime = null;
        queue.DaysOfWeek.Clear();
        queue.AfterComplete = OperationMode.Nothing;
    }

    public override void OnNavigatedFrom()
    {
        SettingsService.Save();
        base.OnNavigatedFrom();
    }
}

/// <summary>
///     Helper argument class used when passing both Queue and QueueDaysOfWeek from XAML bindings.
/// </summary>
public record QueueDayToggleArgs(Queue Queue, QueueDaysOfWeek Day);