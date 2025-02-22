using CommunityToolkit.Mvvm.Input;

using System.Windows.Input;

namespace Clients;

using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;

public class MainViewModel(MessageService messService) : ObservableObject
{
    private string _client1TextBlock;
    private bool _isSending;
    private readonly MessageService _messageService = messService;
    private ICommand _sendMessageCommand;
    private RelayCommand _toggleListening;
    private RelayCommand _getLastMessages;

    private async void ConnectSocket()
    {
        await _messageService.ConnectAsync("ws://localhost:5249/ws").ConfigureAwait(true);
        var res = _messageService.ReceiveMessagesAsync();
        await foreach (var m in res)
        {
            MessagesList.Add(m);
        }
    }

    private async void SendMessage()
    {
        await SendMessageAsync().ConfigureAwait(true);
    }

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

    public ObservableCollection<Message> MessagesList { get; } = [];

    public ICommand SendMessageCommand => _sendMessageCommand ??= new RelayCommand(SendMessage);
    public ICommand ToggleListening => _toggleListening ??= new RelayCommand(ConnectSocket);

    public ObservableCollection<Message> LastMessages { get; } = [];

    public ICommand GetLastMessages => _getLastMessages ??= new RelayCommand(PerformGetLastMessages);

    private async void PerformGetLastMessages()
    {
        await foreach (var m in _messageService.GetLastMessages().ConfigureAwait(true))
        {
            LastMessages.Add(m);
        }
    }
}