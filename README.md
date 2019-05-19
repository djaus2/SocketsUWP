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

The server starts in a listening mode. The client intiates. There is some handshaking where both end send some characters and the other end expects and receives certain characters. 
The server only supports one connection at a time, as doe sthe client.


# Target Platforms
Windows 10 Desktop x64,x86, IoT-Core x64,x86 and ARM32, Windows Phone 10.

##### *NB:*
*The Target Windows version is Creators Update (15063). That way the app can be deployed to Windows Phones.
There is plenty of lattitude though as to what target build you use. The librray an app build need to match of course.*

## ToDos
- Enable Cancellation of Socket IO.
- Some refactoring of code, especially of common functionality between the client and server classes.