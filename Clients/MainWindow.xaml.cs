using System.Net.Http;
using System.Windows;

namespace Clients;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly static HttpClient _httpClient = new HttpClient();
    public MainWindow()
    {
        DataContext = new MainViewModel(new MessageService(_httpClient));
        InitializeComponent();
    }
}