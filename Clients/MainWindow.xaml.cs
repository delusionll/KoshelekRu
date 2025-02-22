namespace Clients;
using System.Net.Http;
using System.Net.WebSockets;
using System.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly static HttpClient _httpClient = new();
    private readonly ClientWebSocket _webSocketClient = new();
    public MainWindow()
    {
        DataContext = new MainViewModel(new MessageService(_httpClient, _webSocketClient));
        InitializeComponent();
    }
}