using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketsUWP
{

    public class RailRoads : IRailRoads
    {
        private StreamReader _streamReader = null;

        public StreamReader StreamReader { get => _streamReader; set => _streamReader = value; }
        public async Task<bool> Expect(char ch)
        {
            try
            {
                char[] chars = new char[] { '\0' };
                int responseLength = await _streamReader?.ReadAsync(chars, 0, 1);
                if (responseLength != 1)
                {
                    System.Diagnostics.Debug.WriteLine("Expect('{0}') .streamReader probably not assigned", ch);
                    return false;
                }
                else if (chars[0] == '\0')
                {
                    System.Diagnostics.Debug.WriteLine("Expect('{0}') .streamReader not assigned", ch);
                    return false;
                }
                else
                {
                    if (ch != chars[0])
                    {
                        System.Diagnostics.Debug.WriteLine("Expect('{0}') got '{1}'", ch, chars[0]);
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            } catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Expect('{0}') Error: {1}", ch,ex.Message);
                return false;
            }
        }

        public async Task<bool> Expect(char ch, CancellationToken cancellationToken)
        {
            try
            {
                return await Expect(ch);
            }
            catch (TaskCanceledException e)
            {
                System.Diagnostics.Debug.WriteLine("Expect('{0}') cancelled.", ch);
                return false;
            }
        }

        public async Task<bool> Expect(string msg)
        {
            string response = "";
            response = await StreamReader?.ReadLineAsync();

            if (response == "")
            {
                System.Diagnostics.Debug.WriteLine("Expect('{0}') .streamReader not assigned", msg);
                return false;
            }
            else
            {
                if (msg != response)
                {
                    System.Diagnostics.Debug.WriteLine("Expect('{0}') got '{1}'", msg, response);
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public async Task<bool> Expect(string msg, CancellationToken cancellationToken)
        {
            try
            {
                return await Expect(msg);
            }
            catch (TaskCanceledException e)
            {
                System.Diagnostics.Debug.WriteLine("Expect(\"{0}\") cancelled.", msg);
                return false;
            }
        }
    }
}
