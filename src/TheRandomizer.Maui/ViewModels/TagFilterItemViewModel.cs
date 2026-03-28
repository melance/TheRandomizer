using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TheRandomizer.Maui.ViewModels;

public partial class TagFilterItemViewModel(String tag, Action changedCallback) : ObservableObject
{
    private readonly Action _changedCallback = changedCallback;
    public String Tag { get; } = tag;

    [ObservableProperty]
    public partial Boolean IsSelected { get; set; }

    partial void OnIsSelectedChanged(Boolean value)
    {
        _changedCallback.Invoke();
    }

    [RelayCommand]
    private void Toggle()
    {
        IsSelected = !IsSelected;
    }
}

