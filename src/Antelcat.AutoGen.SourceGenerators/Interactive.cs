using System;
using System.IO.MemoryMappedFiles;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Antelcat.AutoGen.SourceGenerators;

public static class Interactive
{
    private static TcpListener Listener;
    static Interactive()
    {
        var port = 1119;
        Listener = TcpListener.Create(port);
        var source = new CancellationTokenSource();
        Task.Run(async () =>
        {
            Listener.Start();
            while (!source.IsCancellationRequested)
            {
                var socket = await Listener.AcceptSocketAsync();
                if (socket is null) continue;
                Task.Run(async () =>
                {
                    while (!source.IsCancellationRequested && socket.Connected)
                    {
                        var buffer = new byte[1024];
                        await socket.ReceiveAsync(new ArraySegment<byte>(buffer, 0, 1024), SocketFlags.None);
                    }
                });
            }
        });
    }
}
