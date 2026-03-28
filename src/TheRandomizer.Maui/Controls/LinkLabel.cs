namespace TheRandomizer.Maui.Controls;

public partial class LinkLabel : Label
{
    public static readonly BindableProperty UrlProperty =
        BindableProperty.Create(nameof(Url), typeof(string), typeof(LinkLabel), null);

    public String Url 
    { 
        get => (String)GetValue(UrlProperty); 
        set => SetValue(UrlProperty, value); 
    }

    public LinkLabel()
    {
        TextDecorations = TextDecorations.Underline;
        GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(OpenUrlAsync)
        });
    }

    private async void OpenUrlAsync()
    {
        if (!String.IsNullOrWhiteSpace(Url)) await Launcher.OpenAsync(Url);
    }
}

