using TheRandomizer.Maui.ViewModels;

namespace TheRandomizer.Maui.Views;

public partial class AboutPage : ContentPage
{
	public AboutPage(AboutViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
	}
}