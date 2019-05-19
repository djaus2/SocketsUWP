using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


namespace SocketsUWP
{

    /// <summary>
    /// A high level TCPIP Socket Client class. 
    /// A twin to the <see cref="SockerSvr"/> class.
    /// </summary>
    public partial class SocketServerV2
    {
        public ActionReceivedText OnRecvdText = null;
        public ActionReceivedLine OnRecvdLine = null;
        public ActionReceivedChar OnRecvdChar = null;

        /// <summary>
        /// Conduit of status updates and related messages back to the main app.
        /// </summary>
        public StatusHandlerUWP StatusHandler = null;

        //Currently not used. But should be able to cancel messages.
        private CancellationTokenSource ReadCancellationTokenSource=null;

        //True when socket is ready for connections

        private bool connected = false;

        private ListeningMode _ListeningMode = ListeningMode.ln;

        private SocketsUWP.RailRoads Expectations = null;

        /// <summary>
        /// Class constructor
        /// </summary>
        public SocketServerV2()
        {
            Expectations = new SocketsUWP.RailRoads();
            
            StatusHandler = new StatusHandlerUWP();
            StatusHandler.SetStatus(SocketMode.Disconnected);

            Listening = false;
            //Retrieve the ConnectionProfile
            //We want the internet connection
            string connectionProfileInfo = string.Empty;
            ConnectionProfile InternetConnectionProfile = NetworkInformation.GetInternetConnectionProfile();
            var id = InternetConnectionProfile.NetworkAdapter.NetworkAdapterId;

            //Gets server's IP address
            foreach (HostName localHostName in NetworkInformation.GetHostNames())
            {
                if (localHostName.IPInformation != null)
                {
                    if (localHostName.Type == HostNameType.Ipv4)
                    {
                        if (localHostName.IPInformation.NetworkAdapter.NetworkAdapterId == id)
                        {
                            tbSvrName = localHostName.ToString();
                            break;
                        }
                    }
                }
            }


        }

        // The server socket
        private Windows.Networking.Sockets.StreamSocketListener streamSocketListener = null;

        // TCPIP Port used. Is set from  <see cref="Common"/> class but can be updated in app.
        public static string PortNumber { get;  set; } = "1234";
        //Ipaddress as deteremined in teh constructor
        public string tbSvrName { get; private set; }
        
        //???
        public string recvdText { get; private set; }

        // StreamReader is created from connection and the code path to its use flows from that connection handler
        // <see cref="StreamSocketListener_ConnectionReceived"/>. 
        ///Need to save the StreamWriter from the connection for adhoc sends.
        private StreamWriter _StreamWriter = null;
        private StreamReader _StreamReader = null;



        /// <summary>
        /// Start the socket (listening) service
        /// </summary>
        private async Task StartServer()
        {
            if (StatusHandler.CurrentMode == SocketMode.Disconnected)
            {
                try
                {
                    StatusHandler.SetStatus(SocketMode.Starting);
                    streamSocketListener = new Windows.Networking.Sockets.StreamSocketListener();

                    // The ConnectionReceived event is raised when connections are received.
                    streamSocketListener.ConnectionReceived += this.StreamSocketListener_ConnectionReceived;

                    // Start listening for incoming TCP connections on the specified port. You can specify any port that's not currently in use.
                    await streamSocketListener.BindServiceNameAsync(PortNumber);
                    //_Mode = Mode.JustConnected;

                    StatusHandler.Set(string.Format("Server is listening on Port: {0}", SocketServerV2.PortNumber), SocketMode.Listening);
                }
                catch (Exception ex)
                {
                    Windows.Networking.Sockets.SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                    StatusHandler.SetStatusMsg(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
                }
            }
        }


        /// <summary>
        /// Handles any socket connection to the server, including the initial handshaking between the client and server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void StreamSocketListener_ConnectionReceived(Windows.Networking.Sockets.StreamSocketListener sender, Windows.Networking.Sockets.StreamSocketListenerConnectionReceivedEventArgs args)
        {
            char[] chars = new char[10];
            chars[1] = 'Z';

            string remoteHost = args.Socket.Information.RemoteHostName.DisplayName;

            try
            {
                using (var streamReader = new StreamReader(args.Socket.InputStream.AsStreamForRead()))
                {
                    using (Stream outputStream = args.Socket.OutputStream.AsStreamForWrite())
                    {
                        using (var streamWriter = new StreamWriter(outputStream))
                        {
                            int index = -1; // Common.HandshakeSvrStartIndex;
                            Listening = true;
                            connected = true;
                            try
                            {
                                StatusHandler.SetStatus(SocketMode.JustConnected);

                                //String of characters to send and expect in initail handshake
                                string handshake = Common.Handshake;

                                Listening = false;


                                Expectations.StreamReader = streamReader;
                                do
                                {
                                    index++;
                                    if (index  > handshake.Length - 1)
                                    {
                                        Listening = true;
                                        break;
                                    }
                                    System.Diagnostics.Debug.WriteLine("Svr: Sending " + handshake[index]);

                                    StatusHandler.SetStatus((SocketMode)index); //(ack0, ack2 etc)
                                    await streamWriter.WriteAsync(handshake[index]); //Send @,0,2 etc
                                    await streamWriter.FlushAsync();
                                    index++;
                                    if (index  > handshake.Length - 1)
                                    {
                                        Listening = true;
                                        break;
                                    }
                                    System.Diagnostics.Debug.WriteLine("Svr Expecting " + handshake[index]);
                                }
                                while (await Expectations.Expect(handshake[index]));
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine("Svr Index: " + index.ToString() + " " + ex.Message);
                                StatusHandler.Set("Lost connection:\r\n" + ex.Message, SocketMode.Failed);
                                Listening = false;
                            }
                            if (Listening)
                            { 
                                StatusHandler.Set("Connected to: " + remoteHost, SocketMode.Connected);
                                _StreamWriter = streamWriter;
                                _StreamReader = streamReader;
                                await Listen();
                                //If Listen returns then no longer listening so break connection.
                            }
                            Listening = false;
                            connected = false;
                            _StreamWriter = null;
                            _StreamReader = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StatusHandler.SetStatusMsg("Lost connection:\r\n" + ex.Message);
            }

            //await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => status.Text = string.Format("server sent back the response: \"{0}\"", request));
            sender.Dispose();

            //await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => status.Text = "server closed its socket"); ;
            
        }







        /// <summary>
        /// <param name="command">Start Listening,Stop Listening or <see cref="SendasLine"/></param>
        /// <param name="optionalParam">Optional string parameter</param>
        /// <summary>
        public async Task Action_Cmd(string command, string optionalParam="")
        {
            if (command != "")
            {
                switch (command)
                {
                    case "Start Listening":
                        await StartServer();
                        break;
                    case "Stop Listening":
                        await streamSocketListener.CancelIOAsync();
                        streamSocketListener.Dispose();
                        _StreamWriter = null;
                        StatusHandler.Set( "Socket Server stopped.", SocketMode.Disconnected);
                        Listening = false;
                        break;
                    case "Send Line":
                    if (optionalParam != "")
                    {
                        await SendasLine(optionalParam);
                    }
                    break;
                    case "Send Buffer":
                        if (optionalParam != "")
                        {
                            await SendBuffer(optionalParam.ToArray<Char>());
                        }
                        break;
                    case "Start Listen":
                        StatusHandler.Set("Starting Listen", SocketMode.Connecting);
                        Listening = true;
                        await Listen();
                        StatusHandler.Set("Connected and Listening", SocketMode.Connected);
                        break;
                    case "Stop Listen":
                        //CancelReadTask();
                        break;
				}

            }
        }
    }
}

