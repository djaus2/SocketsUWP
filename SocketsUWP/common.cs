using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketsUWP
{
    public delegate void ActionReceivedText(string recvTxt);
    public delegate void ActionReceivedLine(string recvLine);
    public delegate void ActionReceivedChar(char ch);



    public static class Common
    {
        // Client sends this to server to action disconnection
        public const char DisconnectChar = '^';
        public const char SetBinaryModeRecvChar = '~';
        public const char SetBinaryModeRecvCharAck = '`';
        public const char Ack = '!';

        //TCPIP Server Port
        public static string Port { get; private set; } = "1234";
        public static string SvrHost { get; private set; } = "192.168.0.4";


        //The following are not used
        const int PauseBtwSentCharsmS = 1000;
        public const string EOStringStr = "~";
        public const char EOStringChar = '~';
        public const byte EOStringByte = 126;
        private const int cFineStructure = 137; //ASCII Per mille sign

        // Client and server handshake upon connection by sending and expecting these chars:
        public static int HandshakeSvrStartIndex { get; set; } = 0;
        public static int HandshakeClientStartIndex { get; set; } = 0;
        public static string Handshake = "@01234"; ///Should be an even number of characters
        // Server sends every second char from @ and expects every second char from 0  (in a do Send() while(Expect) loop (V2 only)).
        // Client expects every second char from @ and sends every second char from 0 in response to each (in a while(Expect) Send() loop (V2 only)).
        // In version 1 only:  The first char is consumed by the server's first expectation, ie isn't sent by the client.
    }

}

