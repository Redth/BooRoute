namespace BooRoute;

public partial class App : Application
{
    private readonly AppShell shell;

    public App(AppShell shell)
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Dark;
        this.shell = shell;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(shell);
        window.Resumed += async (_, _) => await shell.RefreshAsync();
        return window;
    }
}