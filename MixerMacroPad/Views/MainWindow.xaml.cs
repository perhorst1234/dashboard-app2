using System;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using MixerMacroPad.Services;
using MixerMacroPad.ViewModels;

namespace MixerMacroPad.Views;

public partial class MainWindow : Window
{
    private readonly ConfigService _configService = new();
    private readonly AudioService _audioService = new();
    private MainViewModel ViewModel => (MainViewModel)DataContext;
    private SerialService _serialService;
    private ButtonActionService _buttonActions;

    public MainWindow()
    {
        InitializeComponent();
        _serialService = new SerialService(_configService);
        _buttonActions = new ButtonActionService(_audioService);
        DataContext = new MainViewModel(_configService, _serialService, _audioService, _buttonActions);
        _serialService.Start();
    }

    private void RefreshPorts_Click(object sender, RoutedEventArgs e) => ViewModel.RefreshPorts();
    private void Connect_Click(object sender, RoutedEventArgs e)
    {
        var selected = ViewModel.SelectedComPort ?? ViewModel.ComPorts.FirstOrDefault();
        if (selected != null)
        {
            ViewModel.SelectedComPort = selected;
            ViewModel.ConnectSerial(selected);
        }
    }

    private void Disconnect_Click(object sender, RoutedEventArgs e) => ViewModel.DisconnectSerial();

    private void SaveConfig_Click(object sender, RoutedEventArgs e) => ViewModel.SaveConfig();

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var sfd = new SaveFileDialog { Filter = "JSON|*.json" };
        if (sfd.ShowDialog() == true)
        {
            _configService.Save();
            _configService.Export(sfd.FileName);
        }
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog { Filter = "JSON|*.json" };
        if (ofd.ShowDialog() == true)
        {
            _configService.Import(ofd.FileName);
        }
    }

    private void RefreshSessions_Click(object sender, RoutedEventArgs e) => ViewModel.RefreshSessions();
}
