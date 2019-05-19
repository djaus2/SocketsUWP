using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SocketsUWP
{
    public interface IRailRoads
    {
        StreamReader StreamReader { get; set; }
        Task<bool> Expect(char ch);
        Task<bool> Expect(char ch, CancellationToken cancellationToken);
        Task<bool> Expect(string msg);
        Task<bool> Expect(string msg, CancellationToken cancellationToken);
    }
}