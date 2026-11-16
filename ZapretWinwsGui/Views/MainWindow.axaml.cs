using Avalonia.Controls;

namespace ZapretWinwsGui.Views;

public partial class MainWindow : Window
{
    private bool _isExplicitExit = false;

    public MainWindow()
    {
        InitializeComponent();
    }

    public void RequestExplicitExit()
    {
        _isExplicitExit = true;
        Close();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!_isExplicitExit)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnClosing(e);
    }
}
