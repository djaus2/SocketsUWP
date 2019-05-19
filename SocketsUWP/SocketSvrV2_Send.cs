using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketsUWP
{
    public partial class SocketServerV2
    {
        private AutoResetEvent waitHandle = new AutoResetEvent(false);

        public async Task SendCh(char ch)
        {
            if (StatusHandler.CurrentMode != SocketMode.Connected)
                return;
            if (_StreamWriter == null)
                return;
            char[] chars = new char[2];
            chars[0] = ch;
            try
            {
                await _StreamWriter.WriteAsync(ch);
                await _StreamWriter.FlushAsync();
            }
            catch (Exception ex)
            {
                Windows.Networking.Sockets.SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                StatusHandler.Set(webErrorStatus.ToString() != "Unknown" ? "Svr: " + webErrorStatus.ToString() : "Svr: " + ex.Message, SocketMode.Failed);
            }
        }

        /// <summary>
        /// Send a string as a new terminated string to the client
        /// </summary>
        /// <param name="msg"></param>
        internal async Task SendasLine(string msg)
        {
            if (msg != "")
            {
                if (StatusHandler.CurrentMode == SocketMode.Connected)
                {

                    if (_StreamWriter != null)
                    {

                        try
                        {
                            System.Diagnostics.Debug.WriteLine(string.Format("Svr: sending \"{0}\"", msg));
                            //Messages terminated with a new line.
                            await _StreamWriter.WriteLineAsync(msg);
                            await _StreamWriter.FlushAsync();
                        }
                        catch (Exception ex)
                        {
                            Windows.Networking.Sockets.SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                            StatusHandler.Set(webErrorStatus.ToString() != "Unknown" ? "Svr: " + webErrorStatus.ToString() : "Svr: " + ex.Message, SocketMode.Failed);
                            Listening = false;
                        }
                    }
                }
            }
        }

        public async Task SendBuffer(char[] request)
        {
            if (StatusHandler.CurrentMode != SocketMode.Connected)
                return;
            if (_StreamWriter == null)
                return;
            System.Diagnostics.Debug.WriteLine("Svr: Resetting wait  handle");
            waitHandle = new AutoResetEvent(false);
            System.Diagnostics.Debug.WriteLine("Svr: Sending buffer send signa1 to svr");
            await SendasLine("" + Common.SetBinaryModeRecvChar);
            System.Diagnostics.Debug.WriteLine("Svr: Waiting for wait handle");
            waitHandle.WaitOne();
            System.Diagnostics.Debug.WriteLine("Svr: Got wait handle");
            try
            {
                //Message are terminated by new line.
                await _StreamWriter.WriteAsync(request, 0, request.Length);
                await _StreamWriter.FlushAsync();
                System.Diagnostics.Debug.WriteLine("Svr: Sent buffer");
            }
            catch (Exception ex)
            {
                Windows.Networking.Sockets.SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                StatusHandler.Set(webErrorStatus.ToString() != "Unknown" ? "Svr: " + webErrorStatus.ToString() : "Svr: " + ex.Message, SocketMode.Failed);
            }
        }
    }
}
