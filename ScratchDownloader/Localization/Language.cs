namespace ScratchDownloader.Localization;

/// <summary>Keys of every translatable string, grouped by the screen or component that uses them.</summary>
public static class Language
{
    public static class Common
    {
        public const string Settings = "Common.Settings";
        public const string Category = "Common.Category";
        public const string Queue = "Common.Queue";
        public const string Size = "Common.Size";
        public const string Segments = "Common.Segments";
        public const string Yes = "Common.Yes";
        public const string No = "Common.No";
        public const string Ok = "Common.Ok";
        public const string Cancel = "Common.Cancel";
        public const string Error = "Common.Error";
        public const string AllFiles = "Common.AllFiles";
        public const string DeleteFile = "Common.DeleteFile";
        public const string UnknownSize = "Common.UnknownSize";
        public const string Bytes = "Common.Bytes";
    }

    public static class LanguageNames
    {
        public const string English = "LanguageNames.English";
        public const string Spanish = "LanguageNames.Spanish";
    }

    public static class Tray
    {
        public const string Show = "Tray.Show";
        public const string Exit = "Tray.Exit";
    }

    public static class SettingsPage
    {
        public const string Language = "SettingsPage.Language";
        public const string SystemTheme = "SettingsPage.SystemTheme";
        public const string LightTheme = "SettingsPage.LightTheme";
        public const string DarkTheme = "SettingsPage.DarkTheme";
        public const string ConfigureApp = "SettingsPage.ConfigureApp";
        public const string Appearance = "SettingsPage.Appearance";
        public const string AppearanceDescription = "SettingsPage.AppearanceDescription";
        public const string Theme = "SettingsPage.Theme";
        public const string SelectTheme = "SettingsPage.SelectTheme";
        public const string Application = "SettingsPage.Application";
        public const string ApplicationDescription = "SettingsPage.ApplicationDescription";
        public const string StartAtLogin = "SettingsPage.StartAtLogin";
        public const string StartAtLoginDescription = "SettingsPage.StartAtLoginDescription";
        public const string StartMinimized = "SettingsPage.StartMinimized";
        public const string StartMinimizedDescription = "SettingsPage.StartMinimizedDescription";
        public const string Notifications = "SettingsPage.Notifications";
        public const string NotificationsDescription = "SettingsPage.NotificationsDescription";
        public const string AutoShowWidget = "SettingsPage.AutoShowWidget";
        public const string AutoShowWidgetDescription = "SettingsPage.AutoShowWidgetDescription";
        public const string AutoCloseWidget = "SettingsPage.AutoCloseWidget";
        public const string AutoCloseWidgetDescription = "SettingsPage.AutoCloseWidgetDescription";
        public const string AutoCloseWidgetInterval = "SettingsPage.AutoCloseWidgetInterval";
        public const string AutoCloseWidgetIntervalDescription = "SettingsPage.AutoCloseWidgetIntervalDescription";
    }

    public static class MainPage
    {
        public const string Dashboard = "MainPage.Dashboard";
        public const string SystemProtection = "MainPage.SystemProtection";
    }

    public static class HomePage
    {
        public const string ResumeDownloadTitle = "HomePage.ResumeDownloadTitle";
        public const string PauseDownloadTitle = "HomePage.PauseDownloadTitle";
        public const string ClearDownloadTitle = "HomePage.ClearDownloadTitle";
        public const string StopDownloadTitle = "HomePage.StopDownloadTitle";
        public const string New = "HomePage.New";
        public const string NewDownload = "HomePage.NewDownload";
        public const string Resume = "HomePage.Resume";
        public const string ResumeAll = "HomePage.ResumeAll";
        public const string Pause = "HomePage.Pause";
        public const string PauseAll = "HomePage.PauseAll";
        public const string StopAll = "HomePage.StopAll";
        public const string ClearDownloadHistory = "HomePage.ClearDownloadHistory";
        public const string Search = "HomePage.Search";
        public const string Start = "HomePage.Start";
        public const string RestartDownload = "HomePage.RestartDownload";
        public const string CopyUrl = "HomePage.CopyUrl";
        public const string Open = "HomePage.Open";
        public const string OpenContainingFolder = "HomePage.OpenContainingFolder";
        public const string MoveToQueue = "HomePage.MoveToQueue";
        public const string MoveToCategory = "HomePage.MoveToCategory";
        public const string RemoveFromList = "HomePage.RemoveFromList";
        public const string ShowWidget = "HomePage.ShowWidget";
        public const string ShowProperties = "HomePage.ShowProperties";
        public const string File = "HomePage.File";
        public const string Status = "HomePage.Status";
        public const string Progress = "HomePage.Progress";
        public const string Speed = "HomePage.Speed";
        public const string Eta = "HomePage.Eta";
        public const string Added = "HomePage.Added";
        public const string FileTab = "HomePage.FileTab";
        public const string ClearHistoryConfirmation = "HomePage.ClearHistoryConfirmation";
        public const string StopAllConfirmation = "HomePage.StopAllConfirmation";
        public const string FailedNewDownload = "HomePage.FailedNewDownload";
        public const string FailedResumeAll = "HomePage.FailedResumeAll";
        public const string FailedPauseAll = "HomePage.FailedPauseAll";
        public const string FailedClearDownloads = "HomePage.FailedClearDownloads";
        public const string FailedStopAll = "HomePage.FailedStopAll";
        public const string DeleteDownloadedFile = "HomePage.DeleteDownloadedFile";
        public const string AllCategories = "HomePage.AllCategories";
        public const string AllQueues = "HomePage.AllQueues";
        public const string TotalSpeed = "HomePage.TotalSpeed";
        public const string TotalSize = "HomePage.TotalSize";
        public const string DownloadsCount = "HomePage.DownloadsCount";
        public const string PausedCount = "HomePage.PausedCount";
        public const string CompletedCount = "HomePage.CompletedCount";
        public const string FailedCount = "HomePage.FailedCount";
        public const string ActiveCount = "HomePage.ActiveCount";
    }

    public static class DownloadStatus
    {
        public const string Initializing = "DownloadStatus.Initializing";
        public const string Queued = "DownloadStatus.Queued";
        public const string Downloading = "DownloadStatus.Downloading";
        public const string Paused = "DownloadStatus.Paused";
        public const string Stopped = "DownloadStatus.Stopped";
        public const string Completed = "DownloadStatus.Completed";
        public const string Failed = "DownloadStatus.Failed";
        public const string CheckingChecksum = "DownloadStatus.CheckingChecksum";
        public const string ChecksumFailed = "DownloadStatus.ChecksumFailed";
    }

    public static class FilePropertiesTabControl
    {
        public const string Name = "FilePropertiesTabControl.Name";
        public const string Path = "FilePropertiesTabControl.Path";
        public const string Host = "FilePropertiesTabControl.Host";
        public const string BytesSuffix = "FilePropertiesTabControl.BytesSuffix";
        public const string SizeProperty = "FilePropertiesTabControl.SizeProperty";
        public const string AddedProperty = "FilePropertiesTabControl.AddedProperty";
        public const string DownloadFileName = "FilePropertiesTabControl.DownloadFileName";
        public const string FileType = "FilePropertiesTabControl.FileType";
    }

    public static class CategoryPage
    {
        public const string Categories = "CategoryPage.Categories";
        public const string CategoriesDescription = "CategoryPage.CategoriesDescription";
        public const string ExtensionsLabel = "CategoryPage.ExtensionsLabel";
        public const string SaveFolder = "CategoryPage.SaveFolder";
        public const string DefaultQueue = "CategoryPage.DefaultQueue";
        public const string ResetExtensions = "CategoryPage.ResetExtensions";
    }

    public static class QueuePage
    {
        public const string NewQueueName = "QueuePage.NewQueueName";
        public const string QueueDescription = "QueuePage.QueueDescription";
        public const string NewQueue = "QueuePage.NewQueue";
        public const string DeleteQueue = "QueuePage.DeleteQueue";
        public const string MaxSpeedLimit = "QueuePage.MaxSpeedLimit";
        public const string MaxConcurrentDownloads = "QueuePage.MaxConcurrentDownloads";
        public const string StartTime = "QueuePage.StartTime";
        public const string ScheduleDays = "QueuePage.ScheduleDays";
        public const string OnCompletionAction = "QueuePage.OnCompletionAction";
        public const string ResetParameters = "QueuePage.ResetParameters";
        public const string DeleteQueueConfirmation = "QueuePage.DeleteQueueConfirmation";
        public const string CannotDeleteMainQueue = "QueuePage.CannotDeleteMainQueue";
    }

    public static class QueueDay
    {
        public const string Sunday = "QueueDay.Sunday";
        public const string Monday = "QueueDay.Monday";
        public const string Tuesday = "QueueDay.Tuesday";
        public const string Wednesday = "QueueDay.Wednesday";
        public const string Thursday = "QueueDay.Thursday";
        public const string Friday = "QueueDay.Friday";
        public const string Saturday = "QueueDay.Saturday";
    }

    public static class OperationMode
    {
        public const string Nothing = "OperationMode.Nothing";
        public const string Notify = "OperationMode.Notify";
        public const string Sleep = "OperationMode.Sleep";
        public const string Shutdown = "OperationMode.Shutdown";
        public const string RunScript = "OperationMode.RunScript";
    }

    public static class AddUrlDialog
    {
        public const string Url = "AddUrlDialog.Url";
        public const string Paste = "AddUrlDialog.Paste";
        public const string DetectingFileInfo = "AddUrlDialog.DetectingFileInfo";
        public const string ProbingLink = "AddUrlDialog.ProbingLink";
        public const string SaveAs = "AddUrlDialog.SaveAs";
        public const string Advanced = "AddUrlDialog.Advanced";
        public const string Checksum = "AddUrlDialog.Checksum";
        public const string StartDownloadAutomatically = "AddUrlDialog.StartDownloadAutomatically";
        public const string Download = "AddUrlDialog.Download";
        public const string SelectSaveLocation = "AddUrlDialog.SelectSaveLocation";
        public const string FileExists = "AddUrlDialog.FileExists";
        public const string FileExistsMessage = "AddUrlDialog.FileExistsMessage";
        public const string RenameAndContinue = "AddUrlDialog.RenameAndContinue";
        public const string Overwrite = "AddUrlDialog.Overwrite";
    }

    public static class FileDialogs
    {
        public const string SelectFile = "FileDialogs.SelectFile";
        public const string Untitled = "FileDialogs.Untitled";
        public const string SelectSaveFolder = "FileDialogs.SelectSaveFolder";
    }

    public static class DownloadWidgetWindow
    {
        public const string PlayPause = "DownloadWidgetWindow.PlayPause";
        public const string OpenFolder = "DownloadWidgetWindow.OpenFolder";
        public const string TopMost = "DownloadWidgetWindow.TopMost";
        public const string Close = "DownloadWidgetWindow.Close";
        public const string WidgetProgress = "DownloadWidgetWindow.WidgetProgress";
        public const string WidgetSpeed = "DownloadWidgetWindow.WidgetSpeed";
        public const string WidgetEta = "DownloadWidgetWindow.WidgetEta";
        public const string WidgetSize = "DownloadWidgetWindow.WidgetSize";
    }

    public static class Notification
    {
        public const string DownloadComplete = "Notification.DownloadComplete";
        public const string DownloadCompleteMessage = "Notification.DownloadCompleteMessage";
        public const string DownloadFailed = "Notification.DownloadFailed";
        public const string DownloadFailedMessage = "Notification.DownloadFailedMessage";
        public const string ChecksumFailed = "Notification.ChecksumFailed";
        public const string ChecksumFailedMessage = "Notification.ChecksumFailedMessage";
    }

    public static class DefaultCategory
    {
        public const string Documents = "DefaultCategory.Documents";
        public const string DocumentsDescription = "DefaultCategory.DocumentsDescription";
        public const string Videos = "DefaultCategory.Videos";
        public const string VideosDescription = "DefaultCategory.VideosDescription";
        public const string Music = "DefaultCategory.Music";
        public const string MusicDescription = "DefaultCategory.MusicDescription";
        public const string Images = "DefaultCategory.Images";
        public const string ImagesDescription = "DefaultCategory.ImagesDescription";
        public const string Archives = "DefaultCategory.Archives";
        public const string ArchivesDescription = "DefaultCategory.ArchivesDescription";
        public const string Applications = "DefaultCategory.Applications";
        public const string ApplicationsDescription = "DefaultCategory.ApplicationsDescription";
        public const string Other = "DefaultCategory.Other";
        public const string OtherDescription = "DefaultCategory.OtherDescription";
        public const string MainQueue = "DefaultCategory.MainQueue";
        public const string SecondaryQueue = "DefaultCategory.SecondaryQueue";
    }

    public static class Time
    {
        public const string Now = "Time.Now";
        public const string MinutesAgo = "Time.MinutesAgo";
        public const string HoursAgo = "Time.HoursAgo";
        public const string DaysAgo = "Time.DaysAgo";
        public const string Yesterday = "Time.Yesterday";
        public const string LessThanOneSecond = "Time.LessThanOneSecond";
    }
}
