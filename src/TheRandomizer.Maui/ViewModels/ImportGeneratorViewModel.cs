using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using TheRandomizer.Application.Interfaces;

namespace TheRandomizer.Maui.ViewModels;

public partial class ImportGeneratorViewModel(IAppSettingsService settings,
                                              IGeneratorFileBrowser fileBrowser,
                                              IGeneratorManagementService _importer) : ObservableObject
{
    #region Members
    private readonly IAppSettingsService _settings = settings;
    private readonly IGeneratorFileBrowser _fileBrowser = fileBrowser;
    private readonly IGeneratorManagementService _importer = _importer;
    #endregion

    #region Properties
    public Boolean CanImport => !IsBusy && !String.IsNullOrWhiteSpace(SourceFilePath);

    public String? EffectiveSubfolder =>
        !String.IsNullOrWhiteSpace(NewSubfolder)
            ? NewSubfolder.Trim()
            : String.IsNullOrWhiteSpace(SelectedSubfolder)
                ? null
                : SelectedSubfolder;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EffectiveSubfolder))]
    public partial String FileName { get; set; } = "";

    [ObservableProperty]
    public partial String ImportedPath { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanImport))]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
    public partial Boolean IsBusy { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EffectiveSubfolder))]
    public partial String NewSubfolder { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EffectiveSubfolder))]
    public partial String SelectedSubfolder { get; set; } = "";

    public String SourceFileName => String.IsNullOrWhiteSpace(SourceFilePath)
                                    ? ""
                                    : Path.GetFileName(SourceFilePath);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanImport))]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
    public partial String SourceFilePath { get; set; } = "";

    [ObservableProperty]
    public partial String StatusMessage { get; set; } = "";

    public ObservableCollection<String> Subfolders { get; } = [];

    [ObservableProperty]
    public partial Boolean WasConfirmed { get; set; }

    [ObservableProperty]
    public partial Boolean WasImported { get; set; }
    #endregion

    partial void OnSourceFilePathChanged(String value)
    {
        OnPropertyChanged(nameof(SourceFileName));
    }

    partial void OnIsBusyChanged(Boolean value)
    {
        OnPropertyChanged(nameof(IsBusy));
    }

    #region Commands
    [RelayCommand]
    public async Task InitializeAsync()
    {
        Subfolders.Clear();

        var root = _settings.GeneratorFolder;
        if (String.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            return;

        var folders = await _fileBrowser.GetSubfoldersAsync(root);

        foreach (var folder in folders)
            Subfolders.Add(folder);
    }

    private Boolean CanBrowseSourceFile() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanBrowseSourceFile))]
    private async Task BrowseSourceFileAsync()
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions()
        {
            PickerTitle = "Select File to Import"
        });

        if (result is not null)
        {
            SourceFilePath = result.FullPath ?? "";
            StatusMessage = "";
        }
    }

    [RelayCommand(CanExecute = nameof(CanImport))]
    private async Task ImportAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = String.Empty;
            WasImported = false;
            ImportedPath = String.Empty;

            if (String.IsNullOrWhiteSpace(SourceFilePath) || !File.Exists(SourceFilePath))
            {
                StatusMessage = "Select a file to import.";
            }
            else
            {
                await using var stream = File.OpenRead(SourceFilePath);

                var result = await _importer.ImportAsync(stream, 
                                                         Path.GetFileName(SourceFilePath), 
                                                         EffectiveSubfolder);

                if (result.Success)
                {
                    WasImported = true;
                    ImportedPath = result.ImportedPath ?? String.Empty;
                    StatusMessage = "Import complete.";
                }
                else
                {
                    StatusMessage = string.Join(Environment.NewLine, result.Diagnostics);
                }

            }
        }
        catch(Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Confirm() => WasConfirmed = true;
    [RelayCommand]
    private void Cancel() => WasConfirmed = false; 
    #endregion
}

