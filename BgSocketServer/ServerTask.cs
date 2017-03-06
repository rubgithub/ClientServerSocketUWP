using System;
using System.Diagnostics;
using System.IO;
using Windows.ApplicationModel.Background;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Streams;

namespace BgSocketServer
{
    public sealed class ServerTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _bgDeferral;
        private string _key;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            Debug.WriteLine($"\n{taskInstance.Task.Name} starting...");
            //taskInstance.Canceled += OnCanceled;

            var socketActivate = taskInstance.TriggerDetails as SocketActivityTriggerDetails;
            Debug.WriteLine("Socket: " + socketActivate?.Reason);
            if (socketActivate?.SocketInformation.SocketKind == SocketActivityKind.StreamSocketListener &&
                socketActivate.Reason == SocketActivityTriggerReason.ConnectionAccepted)
            {
                try
                {
                    Debug.WriteLine($"Details {socketActivate.SocketInformation.StreamSocketListener}");
                    var list = socketActivate.SocketInformation.StreamSocketListener;
                    _bgDeferral = taskInstance.GetDeferral();
                    list.ConnectionReceived += OnConectionReceivedHost;
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Error Run:  {e.Message}");
                }
            }

            // Write to LocalSettings to indicate that this background task ran.
            var settings = ApplicationData.Current.LocalSettings;
            _key = taskInstance.Task.Name;
            settings.Values[_key] = "";
            Debug.WriteLine("ServicingComplete " + taskInstance.Task.Name);
        }

        private async void OnConectionReceivedHost(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            Debug.WriteLine("OnConectionReceivedHost");
            using (var retSock = new DataWriter(args.Socket.OutputStream))
            {
                //Read line from the remote client.
                var inStream = args.Socket.InputStream.AsStreamForRead();
                var reader = new StreamReader(inStream);
                var request = await reader.ReadLineAsync();
                Debug.WriteLine($"Received {request}");

                // Write to LocalSettings message received from client.
                var settings = ApplicationData.Current.LocalSettings;
                settings.Values[_key] = request;

                retSock.WriteString($"HTTP/1.1 200 OK {DateTime.Now}\r\nContent-Length: 2\r\nConnection: close\r\n\r\nOK");
                await retSock.StoreAsync();
                //_deferral.Complete(); //if enabled, it finishs background task and stops working.
            }
        }
    }
}
