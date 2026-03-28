using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TheRandomizer.Maui.ViewModels;

public partial class TagViewModel : ObservableObject
{
    public TagViewModel() { }
    public TagViewModel(String name, Action<TagViewModel>? deleteAction)
    {
        Name = name;
        DeleteAction = deleteAction;
    }

    [ObservableProperty]
    public partial String Name { get; set; } = String.Empty;
    
    private Action<TagViewModel>? DeleteAction { get; set; }

    [RelayCommand]
    public void Delete()
    {
        DeleteAction?.Invoke(this);
    }
}

