using TheRandomizer.Maui.Views;

namespace TheRandomizer.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(GeneratorMetadataEditor), typeof(GeneratorMetadataEditor));
    }
}
