using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TheRandomizer.Application.Interfaces;

namespace TheRandomizer.Maui.ViewModels;

public partial class GeneratorMetadataEditorViewModel(IGeneratorManagementService managmentService) : ObservableObject, IQueryAttributable
{
    private readonly IGeneratorManagementService _managmentService = managmentService;

    [ObservableProperty]
    public partial BaseGenerator? Generator { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<TagViewModel> Tags { get; set; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddTagCommand))]
    public partial String NewTag { get; set; } = String.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveGeneratorCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteGeneratorCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveTagCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddTagCommand))]
    public partial Boolean IsBusy { get; set; } = false;

    private Boolean CanAddTag => !IsBusy && Generator is not null && !String.IsNullOrWhiteSpace(NewTag);
    [RelayCommand(CanExecute = nameof(CanAddTag))]
    public async Task AddTag()
    {
        Tags.Add(new(NewTag, RemoveTag));
    }

    private Boolean CanRemoveTag => !IsBusy && Generator is not null;
    [RelayCommand(CanExecute = nameof(CanRemoveTag))]
    public void RemoveTag(TagViewModel tag)
    {
        Tags.Remove(tag);
    }

    private Boolean CanDelete() => !IsBusy && Generator is not null;
    [RelayCommand(CanExecute = nameof(CanDelete))]
    public async Task DeleteGenerator()
    {
        if (Generator is not null)
        {
            Boolean confirmed = await Shell.Current.DisplayAlertAsync(
                                        "Delete Generator",
                                        $"Delete {Generator.Name}? This cannot be undone.",
                                        "Delete",
                                        "Cancel");

            if (confirmed)
            {
                try
                {
                    IsBusy = true;

                    _managmentService.Delete(Generator.FilePath);

                    await Shell.Current.GoToAsync("..");
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }
    }

    private Boolean CanSave() => !IsBusy && Generator is not null;
    [RelayCommand(CanExecute = nameof(CanSave))]
    public async Task SaveGenerator()
    {
        var tags = (from t in Tags
                   where !String.IsNullOrWhiteSpace(t.Name)
                   select t.Name.Trim())
                   .Distinct()
                   .ToList();
        if (Generator is not null)
        {
            Generator.Tags = tags;
            await _managmentService.SaveAsync(Generator);
        }
    }

    public void Initialize(BaseGenerator generator)
    {
        if (!IsBusy)
        {
            try
            {
                IsBusy = true;
                Generator = generator;

                Tags.Clear();

                foreach (var tag in generator.Tags)
                {
                    Tags.Add(new TagViewModel(tag, RemoveTag));
                }
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    public void ApplyQueryAttributes(IDictionary<String, Object> query)
    {
        if (query.TryGetValue(nameof(Generator), out var value) && value is BaseGenerator generator)
        {
            Initialize(generator);
            Generator = generator;
        }
    }
}