using TheRandomizer.Maui.ViewModels;

namespace TheRandomizer.Maui.Views;

public partial class GeneratorMetadataEditor : ContentPage
{
	public GeneratorMetadataEditor(GeneratorMetadataEditorViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
	}
}