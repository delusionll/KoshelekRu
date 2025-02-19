using CommunityToolkit.Mvvm.Input;

using System.Windows.Input;

namespace Clients;

using System.Windows;

using CommunityToolkit.Mvvm.ComponentModel;

public class MainViewModel(MessageService messService) : ObservableObject
{
    private string _client1TextBlock;
    private bool _isSending;
    private readonly MessageService _messageService = messService;
    private ICommand _sendMessageCommand;

    public string Client1TextBlock
    {
        get => _client1TextBlock;
        set
        {
            if(string.IsNullOrEmpty(value))
            {
                return;
            }

            if(value.Length > 128)
            {
                MessageBox.Show("string length is more than 128;", "Error", MessageBoxButton.OK);
                return;
            }

            _client1TextBlock = value;
            OnPropertyChanged();
        }
    }

    public Visibility IsSending
    {
        get => _isSending ? Visibility.Visible : Visibility.Collapsed;
    }

    public async Task SendMessageAsync()
    {
        _isSending = true;
        OnPropertyChanged(nameof(IsSending));
        try
        {
            await _messageService.SendMessageAsync(Client1TextBlock).ConfigureAwait(true);
        }
        catch
        {
            // ignore
        }
        finally
        {
            _isSending = false;
            OnPropertyChanged(nameof(IsSending));
        }
    }

    public ICommand SendMessageCommand => _sendMessageCommand ??= new RelayCommand(SendMessage);

    private async void SendMessage()
    {
        await SendMessageAsync().ConfigureAwait(true);
    }
}