# SocketsUWP
A High Level UWP TCPIP Sockets library for Socket Client and Server. Includes sample Client and Server apps.

# About
The project consists of 3 subprojects:
- **SocketsWP**: UWP Class library. Contains the Socket Client and Server classes
- **TestSocketClient**: XAML-UWP The Client test app
- **TestSocketSVR**: XAML-UWP The Server test app

To add Socket capability to a a UWP app you include the **SocketsUWP** class library project in the app's solution 
and make reference to it by the app. If you want the app to be the socket client you initiate the **SocketSvrV2** class. 
Otherwise you instatiate the **SocketCientV2** class for a Client app.

The client and server class both contain methods for asynchronous sending and receiving of messages.
String messages are are sent as lines using WriteLine and so the receiving end reads until the string is terminated by a new line (which is removed), using ReadLine. There is also a capability to send and receive chars as you might do in a command mode.
Finally, there is a a mode to send and receive a buffer (as an array of chars). 

The client and server need the same TCPIP port setting of course (set to default to 1234 in the Common class). The server starts in a listening mode. The client intiates. There is some handshaking where both end send some characters and the other end expects and receives certain characters. 
The server only supports one connection at a time, as doe sthe client.

The Client and Server classes use delegates to communicate back to the hosting app when data is received.

*In the class namespace:*
```
public delegate void ActionReceivedText(string recvTxt);
```
*In the class:*

```
 public ActionReceivedText OnRecvdText = null;

//Having got a message:
 OnRecvdText?.Invoke(response);
```
*In the app:*
```
socketSvr.OnRecvdText = OnrecvText;

//The OnrecvText method:
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
```

#### NB:
There are four characters reserved for commands which should not be the first character of any message. These are listed in the Common class.
They should be esacaped in used in messages. Add to this list newline characters.

# Target Platforms
Windows 10 Desktop x64,x86, IoT-Core x64,x86 and ARM32, Windows Phone 10.

##### *NB:*
*The Target Windows version is Creators Update (15063). That way the app can be deployed to Windows Phones.
There is plenty of lattitude though as to what target build you use. The librray and app build need to match of course.*

## ToDos
- Enable Cancellation of Socket IO.
- Automatically esacpe command characters.
- Some refactoring of code, especially of common functionality between the client and server classes.

### Hint:
I run the server on my desktop. 
In my Broadband modem WAN settings at home, I direct the port to my dektop. 
Then I can communicate with the server from my phone, laptop or IoT-Core device when connected to the internet anywhere; subject to routing port restrictions in the mobile device's location.

*This can be a way also for getting around the UWP restriction of apps running on the same host can't communicate over TCPIP. They way you can start both apps simulataneously in Debug mode on the desktop and set teh IP of the host in the Clientto your Broadband WAN IP Address.*

[Ref:](https://docs.microsoft.com/en-us/windows/uwp/networking/sockets)
> [!NOTE]
> As a consequence of [network isolation](https://msdn.microsoft.com/library/windows/apps/hh770532.aspx), Windows disallows establishing a socket connection (Sockets or WinSock) between two UWP apps running on the same machine; whether that's via the local loopback address (127.0.0.0), or by explicitly specifying the local IP address. For details about mechanisms by which UWP apps can communicate with one another, 
> see [App-to-app communication](https://docs.microsoft.com/en-us/windows/uwp/app-to-app/index).

I also use an X64 Windows 10-IoT-Core device, running nearby my desktop as my second target during development

# Usage
Create a new UWP app project. Include the libabry in your app solution and refrence it. Add Internte/Network Capabilities to the app's Package.appmanifest. NB: For IoT-Core there is no need for IOT Extensions. for the Sockets.

## Server
Instantiate the SocketSvr class. 
Implment the **OnrecvText** method in the main app and assign it to the server instance *(Same for OnrecvBuffer)*.
Use the following interface to it:
```
    public interface ISocketServerV2
    {
        string HostIP { get; }

        // static string PortNumber { get; set; }

        Task Action_Cmd(string command, string optionalParam = "");
        Task SendBuffer(char[] request);
        Task SendCh(char ch);
    }
```
The main Client Action commands  are:
- "Start Listening"
- "Stop Listening"
- "Send Line"
- "Send Buffer"

The Port needs to be set before starting the service. 
It is a static property to the class enabling it to be set before the class is instatiated. The app can get and display its IP Address once the service is started.
The last two commands require a suitable parameter.

## Client
Instantiate the SocketClient class. 
Implment the **OnrecvText** method in the main app and assign it to the server instance. *(Same for OnrecvBuffer)*
Use the following interface to it:
```
    public interface ISocketClientV2
    {
        string Host { get; set; }
        string Port { get; set; }
        Task Action_Cmd(string command, string optionalParam = "");
    }
```
The main Client Action commands  are:
- "Connect"
- "Disconnect"
- "Send Line"
- "Send Buffer"

The Host and Port need to be set before connecting. 
The last two commands require a suitable parameter.