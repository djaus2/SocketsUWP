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
        private bool Listening = false;

        /// <summary>
        /// - Create a DataReader object
        /// - Create an async task to read from the SerialDevice InputStream
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async Task Listen()
        {
            try
            {
                string response = "";
                int responseLength = 0;
                ReadCancellationTokenSource = new CancellationTokenSource();
                while (Listening) //!ReadCancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        //Messages are sent such that they are terminated by new line.
                        if (_ListeningMode == ListeningMode.chr)
                        {
                            await ReceiveCh(ReadCancellationTokenSource.Token);
                        }
                        else if (_ListeningMode == ListeningMode.bffr)
                        {
                            OnRecvdText?.Invoke("");
                            OnRecvdChar?.Invoke(' ');
                            ReadCancellationTokenSource = new CancellationTokenSource();
                            System.Diagnostics.Debug.WriteLine("Svr: Buffer Read Start");
                            response = await ReadAsync(_StreamReader, ReadCancellationTokenSource.Token);
                            responseLength = response.Length;
                            System.Diagnostics.Debug.WriteLine("Svr: Buffer Read End");
                            _ListeningMode = ListeningMode.ln;
                        }
                        else if (_ListeningMode == ListeningMode.ln)
                        {
                            response = await ReceiveLn(ReadCancellationTokenSource.Token);
                            responseLength = response.Length;
                        }
                        else if (_ListeningMode == ListeningMode.none)
                        {

                        }
                    }
                    catch (Exception ex)
                    {
                        StatusHandler.Set("Lost connection: " + ex.Message, SocketMode.Failed);
                        Listening = false;
                    }

                    if ((Listening) && (responseLength > 0))
                    {
                        switch (response[0])
                        {
                            case Common.Ack:
                                break;
                            case Common.SetBinaryModeRecvCharAck:
                                break;
                            case Common.DisconnectChar:
                                //Disconnect comamnd from the client
                                Listening = false;
                                _StreamWriter = null;
                                StatusHandler.SetStatus(SocketMode.Disconnected);
                                break;
                            case Common.SetBinaryModeRecvChar:
                                _ListeningMode = ListeningMode.bffr;
                                System.Diagnostics.Debug.WriteLine("Svr: Sending back ack");
                                await _StreamWriter.WriteLineAsync("" + Common.SetBinaryModeRecvCharAck);
                                await _StreamWriter.FlushAsync();
                                break;
                            default:
                                //Do app stuff here. For now just echo chars sent
                                if (responseLength == 1)
                                {
                                    //Single character messages could be commands
                                    if ((response[0] != Common.Ack) && (response[0] != Common.SetBinaryModeRecvCharAck))
                                    {
                                        OnRecvdChar?.Invoke(response[0]);
                                        try
                                        {
                                            await _StreamWriter.WriteLineAsync(Common.Ack);
                                            await _StreamWriter.FlushAsync();
                                        }
                                        catch (Exception ex)
                                        {
                                            StatusHandler.Set("Lost connection: " + ex.Message, SocketMode.Failed);
                                            Listening = false;
                                        }
                                    }
                                }
                                else if (responseLength > 0)
                                {
                                    //More complex commands from the client
                                    OnRecvdText?.Invoke(response);
                                    if (response != "OK")
                                    {
                                        try
                                        {
                                            await _StreamWriter.WriteLineAsync("OK");
                                            await _StreamWriter.FlushAsync();
                                        }
                                        catch (Exception ex)
                                        {
                                            StatusHandler.Set("Lost connection: " + ex.Message, SocketMode.Failed);
                                            Listening = false;
                                        }
                                    }
                                }
                                break;
                        }
                    }
                }
            } catch (Exception ex)
            {
                StatusHandler.Set("Lost connection: " + ex.Message, SocketMode.Failed);
                Listening = false;
            }
            StatusHandler.Set("Stopped Listening", SocketMode.Disconnected);
        }

        public async Task<char> ReceiveCh(CancellationToken cancellationToken)
        {
            if (_ListeningMode != ListeningMode.chr)
                return '\0';
            else if (StatusHandler.CurrentMode != SocketMode.Connected)
                return '\0';
            char ch = '\0';
            char[] chars = new char[2];
            try
            {
                int responseLength = await _StreamReader?.ReadAsync(chars, 0, 1);
                if (responseLength == 1)
                {
                    ch = chars[0];
                    OnRecvdChar?.Invoke(ch);
                }
            }
            catch (Exception ex)
            {
                Windows.Networking.Sockets.SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                StatusHandler.Set(webErrorStatus.ToString() != "Unknown" ? "Svr: " + webErrorStatus.ToString() : "Svr: " + ex.Message, SocketMode.Failed);
                return '\0';
            }
            StatusHandler.SetStatusMsg("Recv'd " + ch);
            return ch;
        }

        public async Task<string> ReceiveLn(CancellationToken cancellationToken)
        {
            if (_ListeningMode != ListeningMode.ln)
                return "";
            else if (StatusHandler.CurrentMode != SocketMode.Connected)
                return "";

            string line = "";
            char[] chars = new char[2];
            try
            {
                System.Diagnostics.Debug.WriteLine("Svr: Waiting for next msg");
                line = await _StreamReader.ReadLineAsync();
                System.Diagnostics.Debug.WriteLine(string.Format("Svr: Got next msg \"{0}\"", line));
                if (line != "")
                {
                    if (line.Length == 1)
                    {
                        OnRecvdChar?.Invoke(line[0]);
                        switch (line[0])
                        {
                            //Need to change mode here before next await Received is looped to.
                            case Common.SetBinaryModeRecvCharAck:
                                System.Diagnostics.Debug.WriteLine("Svr: Setting wait handle");
                                waitHandle?.Set();
                                break;
                        }

                    }
                    else
                        OnRecvdText?.Invoke(line);
                }
            }
            catch (TaskCanceledException te)
            {
                return "";
            }
            catch (Exception ex)
            {
                Windows.Networking.Sockets.SocketErrorStatus webErrorStatus = Windows.Networking.Sockets.SocketError.GetStatus(ex.GetBaseException().HResult);
                StatusHandler.Set(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message, SocketMode.Failed);
                return "";
            }
            return line;
        }

        public async Task<string> ReadAsync(StreamReader streamReader, CancellationToken cancellationToken)
        {
            if (_ListeningMode != ListeningMode.bffr)
                return null;
            else if (StatusHandler.CurrentMode != SocketMode.Connected)
                return null;
            else if (streamReader == null)
                return null;
            char[] buffer = new char[0];
            int responseLength = 0;
            do
            {
                char[] chars = new char[1024];
                responseLength = await streamReader.ReadAsync(chars, 0, 1024);
                char[] z = new char[buffer.Length + responseLength];
                int bs = buffer.Length;
                Array.Resize(ref buffer, bs + responseLength);
                Array.Resize(ref chars, responseLength);
                chars.CopyTo(buffer, bs);
            } while (streamReader.Peek() != -1);
            string response = new string(buffer);
            _ListeningMode = ListeningMode.ln;
            return response;
        }
    }
}
