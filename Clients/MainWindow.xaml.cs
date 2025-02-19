using System.Net.Http;
using System.Net.WebSockets;
using System.Windows;

namespace Clients;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly static HttpClient _httpClient = new HttpClient();
    private readonly ClientWebSocket _webSocketClient = new ClientWebSocket();
    public MainWindow()
    {
        DataContext = new MainViewModel(new MessageService(_httpClient, _webSocketClient));
        InitializeComponent();
    }
}