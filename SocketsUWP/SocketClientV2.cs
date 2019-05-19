using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;
using System.Collections.ObjectModel;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System.Diagnostics;
using Windows.UI.Core;

namespace SocketsUWP
{


    public   partial class SocketClientV2 
    {
        public ActionReceivedText OnRecvdText=null;
        public ActionReceivedLine OnRecvdLine=null;
        public ActionReceivedChar OnRecvdChar=null;

        public RailRoads Expectations { get; set; }

        public StatusHandlerUWP StatusHandler = null;

        public string Host { get;  set; } = "localhost";
        public string Port { get; set; } = "1234";

        private ListeningMode _ListeningMode = ListeningMode.ln;


        private string Title = "Socket Client Terminal UI App - UWP";

        private CancellationTokenSource ReadCancellationTokenSource;


        //public Mode SocketClientMode { get; set; } = Mode.Disconnected;


        public SocketClientV2()
        {
            Expectations = new SocketsUWP.RailRoads();

            StatusHandler = new StatusHandlerUWP();
            StatusHandler.SetStatus(SocketMode.Disconnected);
        }

         ~SocketClientV2()
        {
            
        }



        Windows.Storage.Streams.Buffer OutBuff;




 

        private StreamWriter _StreamWriter = null;
        private StreamReader _StreamReader = null;
        private Windows.Networking.Sockets.StreamSocket _StreamSocket = null;



        public string recvdText { get; private set; }


        private async Task StartSocketClient()
        {
            try
            {

                using (Windows.Networking.Sockets.StreamSocket streamSocket = new Windows.Networking.Sockets.StreamSocket())
                {
                    _StreamSocket = streamSocket;
                    // The server hostname that we will be establishing a connection to. In this example, the server and client are in the same process.

                    var hostName = new Windows.Networking.HostName(Host);

                    StatusHandler.Set("client is trying to connect...", SocketMode.Connecting);

                    await streamSocket.ConnectAsync(hostName, Port);

                    StatusHandler.Set("Socket Connected. Getting config data", SocketMode.JustConnected);

                    Stream outputStream = streamSocket.OutputStream.AsStreamForWrite();
                    Stream inputStream = streamSocket.InputStream.AsStreamForRead();

                    using (StreamWriter StreamWriter = new StreamWriter(outputStream))
                    {
                        using (StreamReader StreamReader = new StreamReader(inputStream))
                        {
                            _StreamReader = StreamReader;
                            _StreamWriter = StreamWriter;
                            int index = Common.HandshakeClientStartIndex;
                            Listening = false;
                            try
                            {
                                string handshake = Common.Handshake;

                                Expectations.StreamReader = StreamReader;
                                System.Diagnostics.Debug.WriteLine("Client Expecting " + handshake[index]);
                                while (await Expectations.Expect(handshake[index]))
                                {
                                    index++;
                                    if (index > handshake.Length - 1)
                                    {
                                        Listening = true;
                                        break;
                                    }
                                    System.Diagnostics.Debug.WriteLine("Client: Sending " + handshake[index]);

                                    StatusHandler.SetStatus((SocketMode)(index)); //(ack0, ack2 etc)
                                    await StreamWriter.WriteAsync(handshake[index]); //Send @,0,2 etc
                                    await StreamWriter.FlushAsync();
                                    index++;
                                    if (index > handshake.Length - 1)
                                    {
                                        Listening = true;
                                        break;
                                    }
                                    System.Diagnostics.Debug.WriteLine("Client Expecting " + handshake[index]);
                                }

                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine("Client Index: " + index.ToString() + " " + ex.Message);
                                StatusHandler.Set("Lost connection: " + ex.Message, SocketMode.Failed);
                                Listening = false;
                            }


                            if (Listening)
                            {
                                StatusHandler.Set("Connected", SocketMode.Connected);
                                await Listen();
                            }
                            await streamSocket?.CancelIOAsync();
                            Listening = false;
                            _StreamWriter = null;
                            _StreamReader = null;


                            StatusHandler.Set("Listening stopped", SocketMode.Disconnected);
                        }
                    }
                }
                _StreamSocket = null;
            }
            catch (Exception ex)
            {
                Windows.Networking.Sockets.SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                StatusHandler.Set(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message, SocketMode.Failed);
            }
        }






        public async Task CloseSocket()
        {
            try
            {
                if (StatusHandler.CurrentMode == SocketMode.Connected)
                {

                    if (_StreamSocket != null)
                        await _StreamSocket?.CancelIOAsync();
                    Listening = false;
                    _StreamWriter = null;
                    _StreamReader = null;

                    StatusHandler.Set("Socket Disconnected",SocketMode.Disconnected);
                }
                else
                    StatusHandler.Set("Socket Already Disconnected",SocketMode.Disconnected);
            }
            catch (Exception ex)
            {
                Windows.Networking.Sockets.SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                StatusHandler.SetStatusMsg(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
            }
        }

        public async Task Action_Cmd(string command, string optionalParam = "")
        {
            if (command != "")
            {
            switch (command)
            {
                case "Disconnect":
                    await this.CloseSocket();
                    StatusHandler = null;
                    OnRecvdText = null;
                    OnRecvdLine = null;
                    OnRecvdChar = null;
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
                case "Clear Recv":
                    this.recvdText = "";
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
                case "Connect":
                    await this.StartSocketClient();
                    break;

				}

            }
        }
    }
}
