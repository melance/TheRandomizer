using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace TheRandomizer.Maui.Controls;

public partial class IconButton : ContentView
{
    public IconButton()
    {
        InitializeComponent();
        SetCurrentState(false);
    }

    public static readonly BindableProperty GlyphProperty =
        BindableProperty.Create(nameof(Glyph), typeof(string), typeof(IconButton), string.Empty,
            propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty PressedGlyphProperty =
        BindableProperty.Create(nameof(PressedGlyph), typeof(string), typeof(IconButton), null,
            propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty IconColorProperty =
        BindableProperty.Create(nameof(IconColor), typeof(Color), typeof(IconButton), Colors.Black,
            propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty PressedTextColorProperty =
        BindableProperty.Create(nameof(PressedTextColor), typeof(Color), typeof(IconButton), null,
            propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty ButtonBackgroundColorProperty =
        BindableProperty.Create(nameof(ButtonBackgroundColor), typeof(Color), typeof(IconButton), Colors.Transparent,
            propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty PressedBackgroundColorProperty =
        BindableProperty.Create(nameof(PressedBackgroundColor), typeof(Color), typeof(IconButton), null,
            propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty DisabledBackgroundColorProperty =
        BindableProperty.Create(nameof(DisabledBackgroundColor), typeof(Color), typeof(IconButton), Colors.Gray,
            propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty DisabledTextColorProperty =
        BindableProperty.Create(nameof(DisabledBackgroundColor), typeof(Color), typeof(IconButton), Colors.White,
            propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(IconButton), propertyChanged: OnCommandChanged);

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(IconButton), propertyChanged: OnCommandParameterChanged);

    public static readonly BindableProperty CornerRadiusProperty =
        BindableProperty.Create(nameof(CornerRadius), typeof(float), typeof(IconButton), 8f);

    public static readonly BindableProperty IconSizeProperty =
        BindableProperty.Create(nameof(IconSize), typeof(double), typeof(IconButton), 20d);

    public static readonly BindableProperty ButtonSizeProperty =
        BindableProperty.Create(nameof(ButtonSize), typeof(double), typeof(IconButton), 36d);

    public static readonly BindableProperty ButtonPaddingProperty =
        BindableProperty.Create(nameof(ButtonPadding), typeof(Thickness), typeof(IconButton), new Thickness(8));

    public static readonly BindableProperty FontFamilyNameProperty =
        BindableProperty.Create(nameof(FontFamilyName), typeof(string), typeof(IconButton), "Fluent",
            propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty TextColorProperty =
        BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(IconButton), Colors.Black, propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty CurrentGlyphProperty =
        BindableProperty.Create(nameof(CurrentGlyph), typeof(string), typeof(IconButton), string.Empty);

    public static readonly BindableProperty CurrentBackgroundColorProperty =
        BindableProperty.Create(nameof(CurrentBackgroundColor), typeof(Color), typeof(IconButton), Colors.Transparent);

    public static readonly BindableProperty CurrentTextColorProperty =
        BindableProperty.Create(nameof(CurrentTextColor), typeof(Color), typeof(IconButton), Colors.Transparent);

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public string? PressedGlyph
    {
        get => (string?)GetValue(PressedGlyphProperty);
        set => SetValue(PressedGlyphProperty, value);
    }

    public Color IconColor
    {
        get => (Color)GetValue(IconColorProperty);
        set => SetValue(IconColorProperty, value);
    }

    public Color? PressedTextColor
    {
        get => (Color?)GetValue(PressedTextColorProperty);
        set => SetValue(PressedTextColorProperty, value);
    }

    public Color ButtonBackgroundColor
    {
        get => (Color)GetValue(ButtonBackgroundColorProperty);
        set => SetValue(ButtonBackgroundColorProperty, value);
    }

    public Color? PressedBackgroundColor
    {
        get => (Color?)GetValue(PressedBackgroundColorProperty);
        set => SetValue(PressedBackgroundColorProperty, value);
    }

    public Color? DisabledBackgroundColor
    {
        get => (Color?)GetValue(DisabledBackgroundColorProperty);
        set => SetValue(DisabledBackgroundColorProperty, value);
    }

    public Color DisabledTextColor
    {
        get => (Color)GetValue(DisabledTextColorProperty);
        set => SetValue(DisabledTextColorProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public float CornerRadius
    {
        get => (float)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public double ButtonSize
    {
        get => (double)GetValue(ButtonSizeProperty);
        set => SetValue(ButtonSizeProperty, value);
    }

    public Thickness ButtonPadding
    {
        get => (Thickness)GetValue(ButtonPaddingProperty);
        set => SetValue(ButtonPaddingProperty, value);
    }

    public string FontFamilyName
    {
        get => (string)GetValue(FontFamilyNameProperty);
        set => SetValue(FontFamilyNameProperty, value);
    }

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public string CurrentGlyph
    {
        get => (string)GetValue(CurrentGlyphProperty);
        private set => SetValue(CurrentGlyphProperty, value);
    }

    public Color CurrentBackgroundColor
    {
        get => (Color)GetValue(CurrentBackgroundColorProperty);
        private set => SetValue(CurrentBackgroundColorProperty, value);
    }

    public Color CurrentTextColor
    {
        get => (Color)GetValue(CurrentTextColorProperty);
        private set => SetValue(CurrentTextColorProperty, value);
    }

    private Boolean _canExecute = true;

    public event EventHandler? Clicked;

    private static void OnCommandChanged(BindableObject bindable, Object? oldValue, Object? newValue)
    {
        var control = (IconButton)bindable;

        if (oldValue is ICommand oldCommand)
            oldCommand.CanExecuteChanged -= control.OnCommandCanExecuteChanged;

        if (newValue is ICommand newCommand)
            newCommand.CanExecuteChanged += control.OnCommandCanExecuteChanged;

        control.UpdateCanExecute();
    }

    private static void OnCommandParameterChanged(BindableObject bindable, Object? oldValue, Object? newValue)
    {
        ((IconButton)bindable).UpdateCanExecute();
    }

    private void OnCommandCanExecuteChanged(Object? sender, EventArgs e)
    {
        UpdateCanExecute();
    }

    private void UpdateCanExecute()
    {
        _canExecute = Command?.CanExecute(CommandParameter) ?? true;

        InnerButton.IsEnabled = _canExecute;
        SetCurrentState(_isPressed);
    }

    protected override void OnPropertyChanged([CallerMemberName] String? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == nameof(IsEnabled))
            SetCurrentState(_isPressed);
    }

    private static void OnVisualPropertyChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        if (bindable is IconButton iconButton)
            iconButton.SetCurrentState(iconButton._isPressed);
    }

    private bool _isPressed;

    private void OnPressed(object? sender, EventArgs e)
    {
        _isPressed = true;
        InnerButton.Scale = 0.92;
        SetCurrentState(true);
    }

    private void OnReleased(object? sender, EventArgs e)
    {
        _isPressed = false;
        InnerButton.Scale = 1;
        SetCurrentState(false);
    }

    private void OnClicked(object? sender, EventArgs e)
    {
        if (_canExecute)
            Clicked?.Invoke(this, EventArgs.Empty);
    }

    private void SetCurrentState(Boolean pressed)
    {
        CurrentGlyph = pressed && !string.IsNullOrWhiteSpace(PressedGlyph)
            ? PressedGlyph!
            : Glyph;

        CurrentTextColor = !_canExecute
            ? DisabledTextColor
            : pressed && PressedTextColor is not null
                ? PressedTextColor
                : TextColor;
                
        CurrentBackgroundColor = !_canExecute
            ? DisabledBackgroundColor!
            : pressed && PressedBackgroundColor is not null
                ? PressedBackgroundColor
                : ButtonBackgroundColor;
    }
}