using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;

namespace SocketClientUWP
{
    public sealed partial class MainPage
    {
        private BackgroundTaskRegistration _task;
        private Guid _taskId;

        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            BgTaskConfig.UnregisterBackgroundTasks(BgTaskConfig.TaskName);

            while (true)
            {
                _task = BgTaskConfig.RegisterBackgroundTask(BgTaskConfig.TaskEntryPoint,
                    BgTaskConfig.TaskName, new SocketActivityTrigger());
                if (_task != null) break;
                UpdateUi();
                await Task.Delay(3000);
                Debug.WriteLine("** FAIL TO REGISTER! (trying again...) **");
            }

            _taskId = _task.TaskId;
            StartServer();
            UpdateUi();
        }

        private async void UpdateUi()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                BtnBgServerUnRegister.IsEnabled = BgTaskConfig.TaskRegStatus == "Registered";
                BtnSend.IsEnabled = BgTaskConfig.ServerStatus == "Running";
                TxtBlkStatusBgTask.Text = $"Status: {BgTaskConfig.TaskRegStatus}";
                TxtBlkStatusServer.Text = $"Status: {BgTaskConfig.ServerStatus}";
                TxtBlkClientRec.Text = $"Received: {BgTaskConfig.ServerReturnMessage}";
                TxtBlkServerRec.Text = $"Received: {BgTaskConfig.ServerReceivedMessage}";
            });
        }

        private async void StartServer()
        {
            try
            {
                const string serverPort = "1337";
                const string socketId = BgTaskConfig.TaskName;
                var sockets = SocketActivityInformation.AllSockets;
                if (!sockets.Keys.Contains(socketId))
                {
                    var socket = new StreamSocketListener();
                    socket.EnableTransferOwnership(_taskId, SocketActivityConnectedStandbyAction.DoNotWake);
                    await socket.BindServiceNameAsync(serverPort);
                    await Task.Delay(500);
                    await socket.CancelIOAsync();
                    socket.TransferOwnership(socketId);
                    BgTaskConfig.ServerStatus = "Running";
                }
            }
            catch (Exception e)
            {
                BgTaskConfig.ServerStatus = "Stopped";
                Debug.Write(e.Message);
            }
            UpdateUi();
        }

        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            var socket = new StreamSocket();
            var serverHost = new Windows.Networking.HostName("localhost");

            const string serverPort = "1337";
            await socket.ConnectAsync(serverHost, serverPort);

            //Write data to the echo server.
            var streamOut = socket.OutputStream.AsStreamForWrite();
            var writer = new StreamWriter(streamOut);
            var request = TxtBoxMessage.Text;
            await writer.WriteLineAsync(request);
            await writer.FlushAsync();

            try
            {
                //Read data from the echo server.
                var streamIn = socket.InputStream.AsStreamForRead();
                var reader = new StreamReader(streamIn);
                var response = await reader.ReadLineAsync();
                BgTaskConfig.ServerReturnMessage = response;
                Debug.WriteLine($"Response: {response}");

                var settings = ApplicationData.Current.LocalSettings;
                var key = BgTaskConfig.TaskName;
                BgTaskConfig.ServerReceivedMessage = settings.Values[key].ToString();
                UpdateUi();
            }
            catch (Exception er)
            {
                Debug.Write($"Error: {er.Message}");
            }

            await socket.CancelIOAsync();
        }

        private void BtnBGServerUnRegister_OnClick(object sender, RoutedEventArgs e)
        {
            BgTaskConfig.UnregisterBackgroundTasks(BgTaskConfig.TaskName);
            UpdateUi();
            CoreApplication.Exit();
        } 
    }
}
