using TheRandomizer.Application.Models;
using TheRandomizer.Maui.ViewModels;
using TheRandomizer.Maui.Views;

namespace TheRandomizer.Maui;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;

    public MainPage(MainViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _serviceProvider = serviceProvider;
        _viewModel = viewModel;
    }

    private void TapGestureRecognizer_Tapped(Object sender, TappedEventArgs e)
    {
        if (DeviceInfo.Current.Platform == DevicePlatform.WinUI
            && sender is CollectionView view
            && view.SelectedItem is GeneratorFileItem item
            && BindingContext is MainViewModel vm)
        {
            vm.SelectedFile = item;

            if (vm.LoadFileCommand.CanExecute(null)) vm.LoadFileCommand.Execute(null);
        }
    }

    private async void OnImportClicked(Object sender, EventArgs e)
    {
        var page = _serviceProvider.GetRequiredService<ImportGeneratorPage>();

        await Navigation.PushModalAsync(page);
        var imported = await page.WaitForCloseAsync();

        if (imported)
            await _viewModel.RefreshFilesCommand.ExecuteAsync(null);
    }

    private async void OnEditMetaDataClicked(Object sender, EventArgs e)
    {
        var page = _serviceProvider.GetRequiredService<GeneratorMetadataEditor>();

        await Navigation.PushModalAsync(page);

        await _viewModel.RefreshFilesCommand.ExecuteAsync(null);
    }
}
