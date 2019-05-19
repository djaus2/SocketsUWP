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

namespace TestSocket_Svr
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

        //SocketsUWP.SocketServer socketSvr = null;
        SocketsUWP.SocketServerV2 socketSvr = null;



        private void OnrecvText(string recvTxt)
        {
            Task.Run(async () =>
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    tb4.Text = recvTxt;
                });
            });
        }

        private void OnRecvdChar(char ch)
        {
            Task.Run(async () =>
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    tb5.Text = "" + ch;
                });
            });
        }

        private void OnStatusChanged(SocketMode mode)
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
                string content = ((string)btn.Content).ToLower();
                await Action(content);
            }
        }


        private async Task Action(string action)
        { 

            switch (action.ToLower())
            {
                case "start":
                    if (socketSvr == null)
                    {
                        int prt;
                        if (int.TryParse(tbPort.Text, out prt))
                        {
                            //SocketServer.PortNumber = tbPort.Text;
                            //socketSvr = new SocketServer();
                            SocketServerV2.PortNumber = tbPort.Text;
                            socketSvr = new SocketServerV2();
                            tb1.Text = string.Format("{0}:{1}", socketSvr.HostIP, SocketServerV2.PortNumber);
                            socketSvr.OnRecvdChar = OnRecvdChar;
                            socketSvr.OnRecvdText = OnrecvText;
                            socketSvr.StatusHandler.OnStatusMsgUpdate = OnStatusMsgUpdate;
                            socketSvr.StatusHandler.OnStatusChanged = OnStatusChanged;

                            await socketSvr.Action_Cmd("Start Listening");
                        }
                    }
                    break;
                case "send":
                    if (socketSvr != null)
                    {
                        OnrecvText("");
                        OnRecvdChar(' ');
                        OnStatusXXXUpdate("");
                        string param = tb2.Text;
                        await socketSvr.Action_Cmd("Send Line", param);
                    }
                    else
                        OnStatusMsgUpdate("Send: Not connected.");
                    break;
                case "buffer send":
                    if (socketSvr != null)
                    {
                        OnrecvText("");
                        OnRecvdChar(' ');
                        OnStatusXXXUpdate("");
                        string param = tb2_1.Text;
                        await socketSvr.Action_Cmd("Send Buffer", param);
                    }
                    else
                        OnStatusMsgUpdate("Send: Not connected.");
                    break;
                case "stop": //Need to differentiate between disconnect and stop server
                    if (socketSvr != null)
                    {
                        await socketSvr.Action_Cmd("Stop Listening");
                        socketSvr = null;
                        tb1.Text = "";
                    }
                    else
                        OnStatusMsgUpdate("Disconnect: Not connected.");
                    break;
                case "exit":
                    socketSvr = null;
                    Application.Current.Exit();
                    break;
            }
        }
        

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {

            tbPort.Text = Common.Port.ToString();
            OnStatusChanged(SocketMode.Disconnected);
            tb4.Text = "";
            tb5.Text = "";
            tb6.Text = "";
            await Action("start");
        }
    }
}
