using TheRandomizer.Maui.ViewModels;
using System.ComponentModel;

namespace TheRandomizer.Maui.Views;

public partial class ImportGeneratorPage : ContentPage
{
    private readonly ImportGeneratorViewModel _viewModel;
    private readonly TaskCompletionSource<Boolean> _closeTcs = new();

	public ImportGeneratorPage(ImportGeneratorViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.InitializeCommand.Execute(null);
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    protected override void OnDisappearing()
    {
        _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        base.OnDisappearing();
    }

    public async Task<Boolean> WaitForCloseAsync() => await _closeTcs.Task;

    private async void OnImportClicked(Object? sender, EventArgs e)
    {
        _viewModel.ImportCommand.Execute(null);
        _viewModel.WasImported = true;
        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(Object? sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
        _closeTcs.TrySetResult(false);
    }

    private async void ViewModel_PropertyChanged(Object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ImportGeneratorViewModel.WasImported) && _viewModel.WasImported)
        {
            await Navigation.PopModalAsync();
            _closeTcs.TrySetResult(true);
        }
    }
}