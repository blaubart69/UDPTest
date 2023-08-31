using System.Net;

namespace UDPProbe;
    
class Program
{
    static int Main(string[] args)
    {
        var opts = Options.GetOpts(args);
        if ( opts == null)
        {
            return 4;
        }
        var tasks = Listen(opts.Ports, 
                          echoReply:  opts.IPs.Count == 0
                        , runForever: opts.IPs.Count == 0 || opts.SendForever);

        if ( opts.IPs.Count > 0)
        {
            Send(opts.IPs, opts.Ports, opts.SendForever);
        }
        
        Task.WaitAll(tasks);

        return 0;
    }
    public static Task[] Listen(List<int> ports, bool echoReply, bool runForever)
    {
        return 
        ports.Select( port =>
        {
            return 
            Task.Run( async () =>
            {
                using var socket = new System.Net.Sockets.UdpClient(port);
                Console.WriteLine($"listening: {socket.Client.LocalEndPoint}");
                do
                {
                    var receiveResult = await socket.ReceiveAsync();
                    string recvMsg = $"received from {receiveResult.RemoteEndPoint}";

                    if ( echoReply )
                    {
                        var returnAddress = new IPEndPoint( receiveResult.RemoteEndPoint.Address, port );
                        int bytesSent = socket.Send(receiveResult.Buffer, receiveResult.Buffer.Length, returnAddress);
                        Console.WriteLine($"{recvMsg}. sent back to {returnAddress}");
                    }
                    else
                    {
                        Console.WriteLine(recvMsg);
                    }
                }
                while (runForever);
            });
        }).ToArray();
    }
    public static void Send(List<IPAddress> IPs, List<int> ports, bool sendForever)
    {
        var buffer = new byte[] { 0, 1, 2 };

        using var socket = new System.Net.Sockets.UdpClient();
        do
        {
            foreach ( var IP in IPs )
            {
                foreach ( var port in ports )
                {
                    var remote = new IPEndPoint(IP,port);
                    int sentBytes = socket.Send(buffer, buffer.Length, remote );
                    Console.WriteLine($"sent {sentBytes} bytes to {remote}");
                }
            }
            if ( sendForever )
            {
                Thread.Sleep(millisecondsTimeout: 2000 );
            }
        }
        while (sendForever);
    }
}