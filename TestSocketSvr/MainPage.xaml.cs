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
using SocketsUWP;
using System.Threading.Tasks;
using Windows.UI.Core;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TestSocket_Client
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        //SocketsUWP.SocketClient socketClient=null;
        SocketsUWP.SocketClientV2 socketClient = null;

        bool InitialCallToConnect = true;
        private  void Page_Loaded(object sender, RoutedEventArgs e)
        {
            tbPort.Text = Common.Port.ToString();
            tbHost.Text = Common.SvrHost;
            //This Text value isn't ready yet so for connect call at startup use the Common value for the port.
            InitialCallToConnect = true;
            Task.Run(async  () => {
                 await Action("connect");
            });
            
        }

        private void OnrecvText(string recvTxt)
        {
            Task.Run(async () => {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    tb5.Text = recvTxt;
                });
            });
        }

        private void OnRecvdChar(char ch)
        {
            Task.Run(async () => {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    tb4.Text = ""+ch;
                    //Server sends ~ before sending a buffer
                });
            });
        }

        private  void OnStatusUpdate(SocketMode mode)
        {
            Task.Run(async () => {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    tb3.Text = mode.ToString();
                });
            });         
        }

        private void OnStatusMsgUpdate(string msg)
        {
            Task.Run(async () => {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    tb6.Text = msg;
                });
            });
        }

        private void OnStatusXXXUpdate(string msg)
        {
            Task.Run(async () => {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    tb6_1.Text = msg;
                });
            });
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            if (btn != null)
            {
                string content = ((string)btn.Content);
                 await Action(content);
            }
        }

        private async Task  Action (string action)
        { 

            switch (action.ToLower())
            {
                case "connect":
                    if (socketClient == null)
                    {
                        int prt;
                        string portTxt = Common.Port.ToString();
                        string hostTxt = Common.SvrHost;
                        //This tbPort.Text value isn't ready at startup so for that use the Common value for the port.
                        if (!InitialCallToConnect)
                        {
                            portTxt = tbPort.Text;
                            hostTxt = tbHost.Text;
                            InitialCallToConnect = false;
                        }
                        if (int.TryParse(portTxt, out prt))
                        {
                            //socketClient = new SocketClient();
                            socketClient = new SocketClientV2();
                            socketClient.OnRecvdChar = OnRecvdChar;
                            socketClient.OnRecvdText = OnrecvText;
                            socketClient.StatusHandler.OnStatusChanged = OnStatusUpdate;
                            socketClient.StatusHandler.OnStatusMsgUpdate = OnStatusMsgUpdate;
                            socketClient.Host = hostTxt;
                            socketClient.Port = portTxt;
                            await socketClient.Action_Cmd("Connect");
                        }
                    }
                    break;
                case "send":
                    if (socketClient != null)
                    {
                        OnrecvText("");
                        OnRecvdChar(' ');
                        OnStatusXXXUpdate("");
                        string param = tb2.Text;
                        await socketClient.Action_Cmd("Send Line",param);
                    }
                    else
                        OnStatusMsgUpdate("Send: Not connected.");
                    break;
                case "buffer send":
                    if (socketClient != null)
                    {
                        OnrecvText("");
                        OnRecvdChar(' ');
                        OnStatusXXXUpdate("");
                        string param = tb2_1.Text;
                        await socketClient.Action_Cmd("Send Buffer", param);
                    }
                    else
                        OnStatusMsgUpdate("Send: Not connected.");
                    break;
                case "disconnect":
                    if (socketClient != null)
                    {
                        await socketClient.Action_Cmd("Disconnect");
                        socketClient = null;
                    }
                    else
                        OnStatusMsgUpdate("Disconnect: Not connected.");
                    break;
                case "exit":
                    socketClient = null;
                    Application.Current.Exit();
                    break;
                case "stop":
                     await socketClient.Action_Cmd("Stop Listen");
                    break;
            }
        }
        
    }
}
