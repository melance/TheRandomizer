using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LB.Utility.Extensions;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Collections.ObjectModel;
using TheRandomizer.Application.Enumerators;
using TheRandomizer.Application.Interfaces;
using TheRandomizer.Application.Models;
using TheRandomizer.Application.Models.Results;
using TheRandomizer.Maui.Interfaces;
using TheRandomizer.Maui.Models;
using TheRandomizer.Maui.Resources.Templates;
using TheRandomizer.Maui.Views;

namespace TheRandomizer.Maui.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    #region Members
    private readonly IGeneratorLoader _loader;
    private readonly IGeneratorRunner _runner;
    private readonly IGeneratorFileBrowser _fileBrowser;
    private readonly IAppSettingsService _settings;
    private readonly INavigationService _navigationService;
    #endregion

    #region Properties
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadFileCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditMetaDataCommand))]
    public partial GeneratorFileItem? SelectedFile { get; set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AllFiles))]
    public partial String SelectedFolder { get; set; } = String.Empty;
    [ObservableProperty]
    public partial GeneratorResult? Output { get; set; }
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadFileCommand))]
    [NotifyCanExecuteChangedFor(nameof(RunGeneratorCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditMetaDataCommand))]
    public partial Boolean IsBusy { get; set; }
    [ObservableProperty]
    public partial HtmlWebViewSource? OutputSource { get; set; }
    public Boolean CanRun => !IsBusy && CurrentGenerator is not null;
    public ObservableCollection<GeneratorFileItem> AllFiles { get; } = [];
    public ObservableCollection<GeneratorFileItem> FilteredFiles { get; } = [];
    public ObservableCollection<AppDiagnostic> Diagnostics { get; } = [];
    public ObservableCollection<ParameterEditorItem> Parameters { get; } = [];
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunGeneratorCommand))]
    public partial BaseGenerator? CurrentGenerator { get; set; }
    [ObservableProperty]
    public partial String? SearchText { get; set; } = String.Empty;
    [ObservableProperty]
    public partial ObservableCollection<TagFilterItemViewModel> AvailableTags { get; set; } = [];
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TagPanelHeight))]
    [NotifyPropertyChangedFor(nameof(TagToggleGlyph))]
    public partial Boolean TagsExpanded { get; set; }
    public Double TagPanelHeight => TagsExpanded ? 300 : 120;
    public String TagToggleGlyph => TagsExpanded ? "Show Less" : "Show More";
    #endregion

    #region Constructor
    public MainViewModel(IGeneratorLoader loader,
                         IGeneratorRunner runner,
                         IGeneratorFileBrowser fileBrowser,
                         IAppSettingsService settings,
                         INavigationService navigationService)
    {
        _loader = loader;
        _runner = runner;
        _fileBrowser = fileBrowser;
        _settings = settings;
        _navigationService = navigationService;
                
        SelectedFolder = Preferences.Default.Get(nameof(SelectedFolder), String.Empty);
        RefreshFilesAsync().Wait();
    } 
    #endregion

    #region Commands
    private Boolean CanCopyToClipboard() => !IsBusy && !String.IsNullOrWhiteSpace(Output?.Text);
    [RelayCommand(CanExecute = nameof(CanCopyToClipboard))]
    private async Task CopyToClipboard(Boolean raw)
    {
        if (OutputSource is not null && !String.IsNullOrWhiteSpace(OutputSource.Html))
        {
            if (raw)
                await Clipboard.Default.SetTextAsync(Output!.Text);
            else
                await Clipboard.Default.SetTextAsync(OutputSource.Html);
        }
    }

    private Boolean CanEditMetaData() => !IsBusy && SelectedFile is not null;
    [RelayCommand(CanExecute = nameof(CanEditMetaData))]
    private async Task EditMetaData()
    {
        if (SelectedFile is not null) {
            var result = _loader.LoadFromFile(SelectedFile.FullPath);
            if (result.Success && result.Generator is not null)
            {
                await _navigationService.GoToAsync(nameof(GeneratorMetadataEditor),
                                                   true,
                                                   (nameof(GeneratorMetadataEditorViewModel.Generator), result.Generator));
            }
        }
    }

    private Boolean CanLoadFile() => !IsBusy && SelectedFile is not null;
    [RelayCommand(CanExecute = nameof(CanLoadFile))]
    private void LoadFile()
    {
        if (SelectedFile is not null)
        {
            try
            {
                IsBusy = true;
                ClearDiagnostics();
                Output = null;

                var result = _loader.LoadFromFile(SelectedFile.FullPath);

                Parameters.Clear();

                if (!result.Success || result.Generator == null)
                {
                    CurrentGenerator = null;
                    ApplyDiagnostics(result.Diagnostics);
                }
                else
                {
                    CurrentGenerator = result.Generator;
                    foreach (var parameter in CurrentGenerator.Parameters)
                    {
                        Parameters.Add(ParameterEditorItem.FromParameter(parameter));
                    }
                    ApplyDiagnostics(result.Diagnostics);
                }
            }
            catch (Exception ex)
            {
                AddDiagnostic(ex);
                CurrentGenerator = null;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    private Boolean CanRunGenerator() => !IsBusy && CurrentGenerator is not null;
    [RelayCommand(CanExecute = nameof(CanRunGenerator))]
    private void RunGenerator()
    {
        if (CurrentGenerator is not null)
        {
            try
            {
                IsBusy = true;
                var parameters = Parameters.ToDictionary(p => p.Name, p => p.Value) ?? [];
                var result = _runner.Run(CurrentGenerator, parameters);
                Diagnostics.Clear();
                foreach (var diagnostic in result.Diagnostics)
                    Diagnostics.Add(diagnostic);
                Output = result.Content;
                OutputSource = new()
                {
                    Html = GenerateHtml(Output)
                };
            }
            catch (Exception ex)
            {
                AddDiagnostic(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    private Boolean CanRefreshFiles() => !IsBusy;
    [RelayCommand(CanExecute = nameof(CanRefreshFiles))]
    private async Task RefreshFilesAsync()
    {
        try
        {
            IsBusy = true;
            ClearDiagnostics();
            AllFiles.Clear();
            SelectedFile = null;

            var files = await _fileBrowser.GetFilesAsync(_settings.GeneratorFolder);

            foreach (var file in files)
                AllFiles.Add(file);
            ApplyFilter();
            BuildAvailableTags();
        }
        catch(Exception ex)
        {
            AddDiagnostic(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Boolean ToggleTagsExpanderCanExecute => !IsBusy;
    [RelayCommand(CanExecute = nameof(ToggleTagsExpanderCanExecute))]
    private void ToggleTagsExpander()
    {
        TagsExpanded = !TagsExpanded;
    }
    #endregion

    #region Private Methods
    partial void OnSearchTextChanged(String? value) => ApplyFilter();

    private void BuildAvailableTags()
    {
        AvailableTags.Clear();

        var tags = AllFiles
                    .SelectMany(f => f.Summary.Tags ?? [])
                    .Where(t => !String.IsNullOrWhiteSpace(t))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(t => t, StringComparer.OrdinalIgnoreCase);
        foreach (var tag in tags)
            AvailableTags.Add(new(tag, ApplyFilter));
    }

    private void ApplyFilter()
    {
        IEnumerable<GeneratorFileItem> filteredFiles = AllFiles;

        var criteria = SearchText?.Trim();
        var selectedTags = AvailableTags.Where(t => t.IsSelected);

        if (!String.IsNullOrWhiteSpace(criteria))
            filteredFiles = filteredFiles.Where(t => MatchesText(t, criteria));

        if (selectedTags?.Count() > 0)
            filteredFiles = filteredFiles.Where(t => MatchesTags(t, selectedTags));
        
        FilteredFiles.Clear();
        foreach (var file in filteredFiles)
            FilteredFiles.Add(file);
    }

    private static Boolean MatchesText(GeneratorFileItem fileItem, String criteria)
    {
        if (fileItem.Name.Contains(criteria, StringComparison.OrdinalIgnoreCase)) return true;
        if (fileItem.Summary.Description is not null && fileItem.Summary.Description.Contains(criteria, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static Boolean MatchesTags(GeneratorFileItem fileItem, IEnumerable<TagFilterItemViewModel> selectedTags)
    {
        var generatorTags = (from t in fileItem.Summary.Tags
                             where !String.IsNullOrWhiteSpace(t)
                             select t)
                             .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return selectedTags.All(t => generatorTags.Contains(t.Tag));
    }

    public void LoadGeneratorFromDefinition(String definition, String? seed = null)
    {
        var result = _loader.Load(definition, seed);

        Diagnostics.Clear();
        foreach (var diagnostic in result.Diagnostics)
        {
            Diagnostics.Add(diagnostic);
        }

        if (!result.Success || result.Generator is null)
        {
            CurrentGenerator = null;
            Output = null;
        }
        else
        {
            CurrentGenerator = result.Generator;
        }
    }

    private static String GenerateHtml(GeneratorResult output)
    {
        // Todo: Add custom CSS
        // Todo: Handle dark mode
        var template = new Output(output);
        return template.TransformText();
    }

    private void ClearDiagnostics()
    {
        Diagnostics.Clear();
    }

    private void ApplyDiagnostics(IEnumerable<AppDiagnostic> diagnostics)
    {
        ClearDiagnostics();
        AppendDiagnostics(diagnostics);
    }

    private void AppendDiagnostics(IEnumerable<AppDiagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
            Diagnostics.Add(diagnostic);
    }

    private void AddDiagnostic(String message, Severity severity = Severity.Error)
    {
        Diagnostics.Add(new AppDiagnostic(severity, message));
    }

    private void AddDiagnostic(Exception ex) => AddDiagnostic(ex.Message); 
    #endregion
}

