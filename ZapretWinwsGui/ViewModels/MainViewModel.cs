using CommunityToolkit.Mvvm.ComponentModel;

namespace ZapretWinwsGui.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _greeting = "Zapret Windows GUI";
}
