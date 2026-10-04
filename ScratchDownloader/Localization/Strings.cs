using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace ScratchDownloader.Localization;

public sealed record LanguageOption(string Code, string DisplayName)
{
    public override string ToString() => DisplayName;
}

public sealed class Strings : INotifyPropertyChanged
{
    private static readonly IReadOnlyDictionary<string, string> English = new Dictionary<string, string>
    {
        ["Language"] = "Language",
        ["English"] = "English",
        ["Spanish"] = "Español",
        ["SystemTheme"] = "System",
        ["LightTheme"] = "Light",
        ["DarkTheme"] = "Dark",
        ["SelectFile"] = "Select a file",
        ["Untitled"] = "Untitled",
        ["SelectSaveFolder"] = "Select Save Folder",
        ["Show"] = "Show",
        ["Exit"] = "Exit",
        ["Settings"] = "Settings",
        ["ConfigureApp"] = "Configure ScratchDownloader",
        ["Appearance"] = "Appearance",
        ["AppearanceDescription"] = "Change the appearance of the application, including theme and accent color.",
        ["Theme"] = "Theme",
        ["SelectTheme"] = "Select the theme for the application.",
        ["Application"] = "Application",
        ["ApplicationDescription"] = "Application settings allow you to customize the behavior of the application.",
        ["StartAtLogin"] = "Start at Login",
        ["StartAtLoginDescription"] = "Start the application when you log in.",
        ["StartMinimized"] = "Start Minimized",
        ["StartMinimizedDescription"] = "Start the application minimized in the system tray.",
        ["Notifications"] = "Notifications",
        ["NotificationsDescription"] = "Enable or disable notifications.",
        ["AutoShowWidget"] = "Auto Show Widget",
        ["AutoShowWidgetDescription"] = "Open the download widget when a download starts.",
        ["AutoCloseWidget"] = "Auto Close Widget",
        ["AutoCloseWidgetDescription"] = "Close the download widget when a download finishes.",
        ["AutoCloseWidgetInterval"] = "Auto Close Widget Interval",
        ["AutoCloseWidgetIntervalDescription"] = "Set the interval (in seconds) to automatically close the download widget after a download finishes.",
        ["Dashboard"] = "Dashboard",
        ["Category"] = "Category",
        ["Queue"] = "Queue",
        ["ResumeDownloadTitle"] = "Resume Download",
        ["PauseDownloadTitle"] = "Pause Download",
        ["ClearDownloadTitle"] = "Clear Download",
        ["StopDownloadTitle"] = "Stop Download",
        ["NewQueueName"] = "New Queue #{0}",
        ["SystemProtection"] = "System Protection",
        ["New"] = "New",
        ["NewDownload"] = "New Download",
        ["Resume"] = "Resume",
        ["ResumeAll"] = "Resume All",
        ["Pause"] = "Pause",
        ["PauseAll"] = "Pause All",
        ["StopAll"] = "Stop All",
        ["ClearDownloadHistory"] = "Clear Download History",
        ["Search"] = "Search...",
        ["Start"] = "Start",
        ["RestartDownload"] = "Restart Download",
        ["CopyUrl"] = "Copy URL",
        ["Open"] = "Open",
        ["OpenContainingFolder"] = "Open Containing Folder",
        ["MoveToQueue"] = "Move to Queue...",
        ["MoveToCategory"] = "Move to Category...",
        ["RemoveFromList"] = "Remove from list",
        ["DeleteFile"] = "Delete File",
        ["ShowWidget"] = "Show Widget",
        ["ShowProperties"] = "Show Properties",
        ["File"] = "File",
        ["Status"] = "Status",
        ["StatusInitializing"] = "Initializing",
        ["StatusQueued"] = "Queued",
        ["StatusDownloading"] = "Downloading",
        ["StatusPaused"] = "Paused",
        ["StatusStopped"] = "Stopped",
        ["StatusCompleted"] = "Completed",
        ["StatusFailed"] = "Failed",
        ["StatusCheckingChecksum"] = "Checking checksum",
        ["StatusChecksumFailed"] = "Checksum failed",
        ["DaySunday"] = "Sun",
        ["DayMonday"] = "Mon",
        ["DayTuesday"] = "Tue",
        ["DayWednesday"] = "Wed",
        ["DayThursday"] = "Thu",
        ["DayFriday"] = "Fri",
        ["DaySaturday"] = "Sat",
        ["OperationNothing"] = "Do nothing",
        ["OperationNotify"] = "Notify",
        ["OperationSleep"] = "Sleep",
        ["OperationShutdown"] = "Shut down",
        ["OperationRunScript"] = "Run script",
        ["Progress"] = "Progress",
        ["Speed"] = "Speed",
        ["Size"] = "Size",
        ["Eta"] = "ETA",
        ["Added"] = "ADDED",
        ["FileTab"] = "FILE",
        ["Name"] = "Name:",
        ["Path"] = "Path:",
        ["Host"] = "Host:",
        ["TotalSpeed"] = "Total Speed:",
        ["TotalSize"] = "Total Size:",
        ["Bytes"] = "bytes",
        ["BytesSuffix"] = " bytes )",
        ["SizeProperty"] = "Size:",
        ["AddedProperty"] = "Added:",
        ["DownloadFileName"] = "download",
        ["FileType"] = "FILE",
        ["Categories"] = "Categories",
        ["CategoriesDescription"] = "Categories auto-sort downloads by file extension. Each one owns the folder its files land in.",
        ["ExtensionsLabel"] = "EXTENSIONS, COMMA-SEPARATED, NO DOTS",
        ["SaveFolder"] = "SAVE FOLDER",
        ["DefaultQueue"] = "DEFAULT QUEUE",
        ["MainQueue"] = "Main",
        ["SecondaryQueue"] = "Secondary",
        ["ResetExtensions"] = "Reset extensions",
        ["QueueDescription"] = "Manage how your downloads use the internet, when they start, what happens when they finish, and more.",
        ["NewQueue"] = "New Queue",
        ["DeleteQueue"] = "Delete Queue",
        ["Segments"] = "SEGMENTS",
        ["MaxSpeedLimit"] = "MAX SPEED LIMIT (KB/s, 0 = UNLIMITED)",
        ["MaxConcurrentDownloads"] = "MAX CONCURRENT DOWNLOADS",
        ["StartTime"] = "START TIME",
        ["ScheduleDays"] = "SCHEDULE DAYS",
        ["OnCompletionAction"] = "ON COMPLETION ACTION",
        ["ResetParameters"] = "Reset Parameters",
        ["Url"] = "URL",
        ["Paste"] = "Paste",
        ["DetectingFileInfo"] = "Detecting file information...",
        ["ProbingLink"] = "Probing the link for filename and size.",
        ["SaveAs"] = "SAVE AS",
        ["Advanced"] = "Advanced",
        ["Checksum"] = "Checksum",
        ["StartDownloadAutomatically"] = "Start download automatically",
        ["Cancel"] = "Cancel",
        ["Download"] = "Download",
        ["SelectSaveLocation"] = "Select Save Location",
        ["AllFiles"] = "All Files",
        ["FileExists"] = "File Exists",
        ["FileExistsMessage"] = "The file \"{0}\" already exists. Do you want to rename it or overwrite the existing file?",
        ["RenameAndContinue"] = "Rename and Continue",
        ["Overwrite"] = "Overwrite",
        ["Ok"] = "OK",
        ["Yes"] = "Yes",
        ["No"] = "No",
        ["Error"] = "Error",
        ["DeleteQueueConfirmation"] = "Are you sure you want to delete the queue '{0}'?",
        ["CannotDeleteMainQueue"] = "The main queue cannot be deleted.",
        ["ClearHistoryConfirmation"] = "Are you sure you want to remove all completed, stopped and failed downloads? This action cannot be undone.",
        ["StopAllConfirmation"] = "Are you sure you want to stop all downloads? This action cannot be undone.",
        ["FailedNewDownload"] = "Failed to create a new download. Please try again.",
        ["FailedResumeAll"] = "Failed to resume all downloads. Please try again.",
        ["FailedPauseAll"] = "Failed to pause all downloads. Please try again.",
        ["FailedClearDownloads"] = "Failed to clear all downloads. Please try again.",
        ["FailedStopAll"] = "Failed to stop all downloads. Please try again.",
        ["DeleteDownloadedFile"] = "Are you sure you want to delete the downloaded file?",
        ["PlayPause"] = "Play/Pause",
        ["OpenFolder"] = "Open Folder",
        ["TopMost"] = "TopMost",
        ["Close"] = "Close",
        ["NotificationDownloadComplete"] = "Download completed",
        ["NotificationDownloadCompleteMessage"] = "Downloaded: {0}",
        ["NotificationDownloadFailed"] = "Download failed",
        ["NotificationDownloadFailedMessage"] = "Failed to download: {0}",
        ["NotificationChecksumFailed"] = "Checksum verification failed",
        ["NotificationChecksumFailedMessage"] = "The checksum did not match for: {0}",
        ["WidgetProgress"] = "PROGRESS",
        ["WidgetSpeed"] = "SPEED",
        ["WidgetEta"] = "ETA",
        ["WidgetSize"] = "SIZE",
        ["CategoryDocuments"] = "Documents",
        ["CategoryDocumentsDescription"] = "Text documents, spreadsheets, and presentations",
        ["CategoryVideos"] = "Videos",
        ["CategoryVideosDescription"] = "Movies, clips, and video files",
        ["CategoryMusic"] = "Music",
        ["CategoryMusicDescription"] = "Songs, podcasts, and audio files",
        ["CategoryImages"] = "Images",
        ["CategoryImagesDescription"] = "Photos, graphics, and vector images",
        ["CategoryArchives"] = "Archives",
        ["CategoryArchivesDescription"] = "Compressed archives and package files",
        ["CategoryApplications"] = "Applications",
        ["CategoryApplicationsDescription"] = "Software installers, executables, and disk images",
        ["CategoryOther"] = "Other",
        ["CategoryOtherDescription"] = "Everything the other categories don't claim",
        ["AllCategories"] = "All Categories",
        ["AllQueues"] = "All Queues",
        ["Now"] = "Now",
        ["MinutesAgo"] = "{0}m ago",
        ["HoursAgo"] = "{0}h ago",
        ["DaysAgo"] = "{0}d ago",
        ["Yesterday"] = "Yesterday {0}",
        ["UnknownSize"] = "Unknown size",
        ["LessThanOneSecond"] = "< 1 second",
        ["DownloadsCount"] = "Downloads: {0}",
        ["PausedCount"] = "Paused: {0}",
        ["CompletedCount"] = "Completed: {0}",
        ["FailedCount"] = "Failed: {0}",
        ["ActiveCount"] = "Active: {0}",
    };

    private static readonly IReadOnlyDictionary<string, string> Spanish = new Dictionary<string, string>
    {
        ["Language"] = "Idioma",
        ["English"] = "English",
        ["Spanish"] = "Español",
        ["SystemTheme"] = "Sistema",
        ["LightTheme"] = "Claro",
        ["DarkTheme"] = "Oscuro",
        ["SelectFile"] = "Seleccionar un archivo",
        ["Untitled"] = "Sin título",
        ["SelectSaveFolder"] = "Seleccionar carpeta de destino",
        ["Show"] = "Mostrar",
        ["Exit"] = "Salir",
        ["Settings"] = "Configuración",
        ["ConfigureApp"] = "Configurar ScratchDownloader",
        ["Appearance"] = "Apariencia",
        ["AppearanceDescription"] = "Cambia la apariencia de la aplicación, incluidos el tema y el color de énfasis.",
        ["Theme"] = "Tema",
        ["SelectTheme"] = "Selecciona el tema de la aplicación.",
        ["Application"] = "Aplicación",
        ["ApplicationDescription"] = "Personaliza el comportamiento de la aplicación.",
        ["StartAtLogin"] = "Iniciar al iniciar sesión",
        ["StartAtLoginDescription"] = "Inicia la aplicación al iniciar sesión.",
        ["StartMinimized"] = "Iniciar minimizada",
        ["StartMinimizedDescription"] = "Inicia la aplicación minimizada en la bandeja del sistema.",
        ["Notifications"] = "Notificaciones",
        ["NotificationsDescription"] = "Activa o desactiva las notificaciones.",
        ["AutoShowWidget"] = "Mostrar widget automáticamente",
        ["AutoShowWidgetDescription"] = "Abre el widget de descargas al iniciar una descarga.",
        ["AutoCloseWidget"] = "Cerrar widget automáticamente",
        ["AutoCloseWidgetDescription"] = "Cierra el widget cuando finaliza una descarga.",
        ["AutoCloseWidgetInterval"] = "Intervalo de cierre automático",
        ["AutoCloseWidgetIntervalDescription"] = "Tiempo en segundos para cerrar el widget tras finalizar una descarga.",
        ["Dashboard"] = "Panel",
        ["Category"] = "Categoría",
        ["Queue"] = "Cola",
        ["ResumeDownloadTitle"] = "Reanudar descarga",
        ["PauseDownloadTitle"] = "Pausar descarga",
        ["ClearDownloadTitle"] = "Borrar descargas",
        ["StopDownloadTitle"] = "Detener descarga",
        ["NewQueueName"] = "Nueva cola n.º {0}",
        ["SystemProtection"] = "Protección del sistema",
        ["New"] = "Nuevo",
        ["NewDownload"] = "Nueva descarga",
        ["Resume"] = "Reanudar",
        ["ResumeAll"] = "Reanudar todo",
        ["Pause"] = "Pausar",
        ["PauseAll"] = "Pausar todo",
        ["StopAll"] = "Detener todo",
        ["ClearDownloadHistory"] = "Borrar historial de descargas",
        ["Search"] = "Buscar...",
        ["Start"] = "Iniciar",
        ["RestartDownload"] = "Reiniciar descarga",
        ["CopyUrl"] = "Copiar URL",
        ["Open"] = "Abrir",
        ["OpenContainingFolder"] = "Abrir carpeta contenedora",
        ["MoveToQueue"] = "Mover a la cola...",
        ["MoveToCategory"] = "Mover a la categoría...",
        ["RemoveFromList"] = "Quitar de la lista",
        ["DeleteFile"] = "Eliminar archivo",
        ["ShowWidget"] = "Mostrar widget",
        ["ShowProperties"] = "Mostrar propiedades",
        ["File"] = "Archivo",
        ["Status"] = "Estado",
        ["StatusInitializing"] = "Preparando",
        ["StatusQueued"] = "En cola",
        ["StatusDownloading"] = "Descargando",
        ["StatusPaused"] = "En pausa",
        ["StatusStopped"] = "Detenida",
        ["StatusCompleted"] = "Completada",
        ["StatusFailed"] = "Fallida",
        ["StatusCheckingChecksum"] = "Verificando suma",
        ["StatusChecksumFailed"] = "Error de verificación",
        ["DaySunday"] = "Dom",
        ["DayMonday"] = "Lun",
        ["DayTuesday"] = "Mar",
        ["DayWednesday"] = "Mié",
        ["DayThursday"] = "Jue",
        ["DayFriday"] = "Vie",
        ["DaySaturday"] = "Sáb",
        ["OperationNothing"] = "No hacer nada",
        ["OperationNotify"] = "Notificar",
        ["OperationSleep"] = "Suspender",
        ["OperationShutdown"] = "Apagar",
        ["OperationRunScript"] = "Ejecutar script",
        ["Progress"] = "Progreso",
        ["Speed"] = "Velocidad",
        ["Size"] = "Tamaño",
        ["Eta"] = "Tiempo restante",
        ["Added"] = "AÑADIDO",
        ["FileTab"] = "ARCHIVO",
        ["Name"] = "Nombre:",
        ["Path"] = "Ruta:",
        ["Host"] = "Servidor:",
        ["TotalSpeed"] = "Velocidad total:",
        ["TotalSize"] = "Tamaño total:",
        ["Bytes"] = "bytes",
        ["BytesSuffix"] = " bytes )",
        ["SizeProperty"] = "Tamaño:",
        ["AddedProperty"] = "Añadido:",
        ["DownloadFileName"] = "descarga",
        ["FileType"] = "ARCHIVO",
        ["Categories"] = "Categorías",
        ["CategoriesDescription"] = "Las categorías organizan las descargas por extensión y carpeta de destino.",
        ["ExtensionsLabel"] = "EXTENSIONES SEPARADAS POR COMAS, SIN PUNTOS",
        ["SaveFolder"] = "CARPETA DE DESTINO",
        ["DefaultQueue"] = "COLA PREDETERMINADA",
        ["MainQueue"] = "Principal",
        ["SecondaryQueue"] = "Secundaria",
        ["ResetExtensions"] = "Restablecer extensiones",
        ["QueueDescription"] = "Administra el uso de Internet, el inicio y las acciones al finalizar tus descargas.",
        ["NewQueue"] = "Nueva cola",
        ["DeleteQueue"] = "Eliminar cola",
        ["Segments"] = "SEGMENTOS",
        ["MaxSpeedLimit"] = "LÍMITE DE VELOCIDAD (KB/s, 0 = ILIMITADO)",
        ["MaxConcurrentDownloads"] = "DESCARGAS SIMULTÁNEAS MÁXIMAS",
        ["StartTime"] = "HORA DE INICIO",
        ["ScheduleDays"] = "DÍAS PROGRAMADOS",
        ["OnCompletionAction"] = "ACCIÓN AL FINALIZAR",
        ["ResetParameters"] = "Restablecer parámetros",
        ["Url"] = "URL",
        ["Paste"] = "Pegar",
        ["DetectingFileInfo"] = "Detectando información del archivo...",
        ["ProbingLink"] = "Comprobando el enlace para obtener el nombre y el tamaño.",
        ["SaveAs"] = "GUARDAR COMO",
        ["Advanced"] = "Avanzado",
        ["Checksum"] = "Suma de verificación",
        ["StartDownloadAutomatically"] = "Iniciar descarga automáticamente",
        ["Cancel"] = "Cancelar",
        ["Download"] = "Descargar",
        ["SelectSaveLocation"] = "Seleccionar ubicación",
        ["AllFiles"] = "Todos los archivos",
        ["FileExists"] = "El archivo ya existe",
        ["FileExistsMessage"] = "El archivo \"{0}\" ya existe. ¿Quieres cambiarle el nombre o sobrescribirlo?",
        ["RenameAndContinue"] = "Cambiar nombre y continuar",
        ["Overwrite"] = "Sobrescribir",
        ["Ok"] = "Aceptar",
        ["Yes"] = "Sí",
        ["No"] = "No",
        ["Error"] = "Error",
        ["DeleteQueueConfirmation"] = "¿Quieres eliminar la cola '{0}'?",
        ["CannotDeleteMainQueue"] = "No se puede eliminar la cola principal.",
        ["ClearHistoryConfirmation"] = "¿Quieres quitar todas las descargas completadas, detenidas y fallidas? Esta acción no se puede deshacer.",
        ["StopAllConfirmation"] = "¿Quieres detener todas las descargas? Esta acción no se puede deshacer.",
        ["FailedNewDownload"] = "No se pudo crear la descarga. Inténtalo de nuevo.",
        ["FailedResumeAll"] = "No se pudieron reanudar todas las descargas. Inténtalo de nuevo.",
        ["FailedPauseAll"] = "No se pudieron pausar todas las descargas. Inténtalo de nuevo.",
        ["FailedClearDownloads"] = "No se pudieron borrar las descargas. Inténtalo de nuevo.",
        ["FailedStopAll"] = "No se pudieron detener todas las descargas. Inténtalo de nuevo.",
        ["DeleteDownloadedFile"] = "¿Quieres eliminar el archivo descargado?",
        ["PlayPause"] = "Reproducir/Pausar",
        ["OpenFolder"] = "Abrir carpeta",
        ["TopMost"] = "Siempre visible",
        ["Close"] = "Cerrar",
        ["NotificationDownloadComplete"] = "Descarga completada",
        ["NotificationDownloadCompleteMessage"] = "Descargado: {0}",
        ["NotificationDownloadFailed"] = "Error en la descarga",
        ["NotificationDownloadFailedMessage"] = "No se pudo descargar: {0}",
        ["NotificationChecksumFailed"] = "Error de verificación",
        ["NotificationChecksumFailedMessage"] = "La suma de verificación no coincide para: {0}",
        ["WidgetProgress"] = "PROGRESO",
        ["WidgetSpeed"] = "VELOCIDAD",
        ["WidgetEta"] = "RESTANTE",
        ["WidgetSize"] = "TAMAÑO",
        ["CategoryDocuments"] = "Documentos",
        ["CategoryDocumentsDescription"] = "Documentos de texto, hojas de cálculo y presentaciones",
        ["CategoryVideos"] = "Vídeos",
        ["CategoryVideosDescription"] = "Películas, clips y archivos de vídeo",
        ["CategoryMusic"] = "Música",
        ["CategoryMusicDescription"] = "Canciones, podcasts y archivos de audio",
        ["CategoryImages"] = "Imágenes",
        ["CategoryImagesDescription"] = "Fotos, gráficos e imágenes vectoriales",
        ["CategoryArchives"] = "Archivos comprimidos",
        ["CategoryArchivesDescription"] = "Archivos comprimidos y paquetes",
        ["CategoryApplications"] = "Aplicaciones",
        ["CategoryApplicationsDescription"] = "Instaladores, ejecutables e imágenes de disco",
        ["CategoryOther"] = "Otros",
        ["CategoryOtherDescription"] = "Todo lo que no pertenece a otras categorías",
        ["AllCategories"] = "Todas las categorías",
        ["AllQueues"] = "Todas las colas",
        ["Now"] = "Ahora",
        ["MinutesAgo"] = "hace {0} min",
        ["HoursAgo"] = "hace {0} h",
        ["DaysAgo"] = "hace {0} d",
        ["Yesterday"] = "Ayer {0}",
        ["UnknownSize"] = "Tamaño desconocido",
        ["LessThanOneSecond"] = "< 1 segundo",
        ["DownloadsCount"] = "Descargas: {0}",
        ["PausedCount"] = "En pausa: {0}",
        ["CompletedCount"] = "Completadas: {0}",
        ["FailedCount"] = "Fallidas: {0}",
        ["ActiveCount"] = "Activas: {0}",
    };

    private string _language = "en";

    private Strings()
    {
        Languages =
        [
            new LanguageOption("en", English["English"]),
            new LanguageOption("es", Spanish["Spanish"])
        ];
    }

    public static Strings Instance { get; } = new();
    public IReadOnlyList<LanguageOption> Languages { get; }
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Language
    {
        get => _language;
        set
        {
            var normalized = value == "es" ? "es" : "en";
            var culture = CultureInfo.GetCultureInfo(normalized == "es" ? "es-ES" : "en-US");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            if (_language == normalized)
                return;

            _language = normalized;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            RefreshAll();
        }
    }

    public string this[string key] =>
        (_language == "es" ? Spanish : English).TryGetValue(key, out var value)
            ? value
            : English.TryGetValue(key, out value) ? value : key;

    private static readonly Dictionary<string, LocValue> Bound = new();

    internal static LocValue For(string key)
    {
        if (!Bound.TryGetValue(key, out var value))
            Bound[key] = value = new LocValue(key);
        return value;
    }

    private static void RefreshAll()
    {
        foreach (var value in Bound.Values)
            value.Refresh();
    }

    public static string Get(string key) => Instance[key];

    public static bool IsTranslation(string key, string value) =>
        English.TryGetValue(key, out var english) && english == value ||
        Spanish.TryGetValue(key, out var spanish) && spanish == value;

    public static string Format(string key, params object?[] args) =>
        string.Format(System.Globalization.CultureInfo.CurrentCulture, Get(key), args);
}

public sealed class LocValue : INotifyPropertyChanged
{
    internal LocValue(string key)
    {
        Key = key;
    }

    public string Key { get; }
    public string Value => Strings.Get(Key);
    public event PropertyChangedEventHandler? PropertyChanged;

    internal void Refresh() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
}

public sealed class LocExtension : MarkupExtension
{
    public LocExtension(string key)
    {
        Key = key;
    }

    public string Key { get; }

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        new ReflectionBindingExtension(nameof(LocValue.Value))
        {
            Mode = BindingMode.OneWay,
            Source = Strings.For(Key)
        }.ProvideValue(serviceProvider);
}
