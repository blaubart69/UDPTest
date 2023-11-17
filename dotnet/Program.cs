using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

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

        bool runForever = opts.IPs.Count == 0 || opts.SendForever;
        int receivesPerPort;
        if (runForever)
        {
            receivesPerPort = 3;
        }
        else
        {
            receivesPerPort = 1;
        }

        int receives = opts.Ports.Count * receivesPerPort;
        CountdownEvent allReceivesListening = new CountdownEvent(receives);
        var replies = new ConcurrentDictionary<IPAddress, List<int>>();

        CancellationTokenSource cts = new CancellationTokenSource();

        /*
        Console.CancelKeyPress += (sender, args) =>
        {
            args.Cancel = true;
            cts.Cancel();
        };
        */

        Task.Run(() => HandleQuit(cts));

        var receiveTasks = SetupReceives(
              cts.Token
            , allReceivesListening
            , opts.Ports
            , echoReply:  opts.IPs.Count == 0
            , runForever
            , receivesPerPort
            , replies);

        if ( ! allReceivesListening.Wait(millisecondsTimeout: 3000) )
        {
            Console.Error.WriteLine("could not setup listening on all ports");
            return 8;
        }
        else
        {
            Console.WriteLine("listening on all given ports (press q to quit)");
        }

        if ( opts.IPs.Count > 0)
        {
            Send(opts.IPs, opts.Ports, opts.SendForever);
        }
        
        try
        {
            Task.WaitAll(receiveTasks, cts.Token);
        }
        catch (OperationCanceledException)
        {
        }

        PrintReplies(replies, opts.Ports);

        return 0;
    }
    public static Task[] SetupReceives(CancellationToken ct, CountdownEvent cde, List<int> ports, bool echoReply, bool runForever, int receivesPerPort, ConcurrentDictionary<IPAddress, List<int>> replies)
    {
        return 
            ports.SelectMany( port =>
                Enumerable
                    .Repeat(port, receivesPerPort)
                    .Select(p => Receive(ct, cde, p, echoReply, runForever, replies))
            ).ToArray();
    }
    public static async Task Receive(CancellationToken cancelToken, CountdownEvent cde, int port, bool echoReply, bool runForever, ConcurrentDictionary<IPAddress, List<int>> replies)
    {
        try
        {
            using var udpClient = new System.Net.Sockets.UdpClient();
            //
            // ReuseAddress AND Bind() are important to have multiple Receives() in flight
            //
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.Client.Bind(new IPEndPoint( IPAddress.Any, port));
            var receiveTask =  udpClient.ReceiveAsync(cancelToken).ConfigureAwait(false);
            //Console.WriteLine($"listening on port {port}");
            cde.Signal();

            for (;;)
            {
                var receiveResult = await receiveTask;
                string recvMsg = $"received from {receiveResult.RemoteEndPoint}";
                replies.AddOrUpdate(
                      key: receiveResult.RemoteEndPoint.Address
                    , addValue: new List<int>() { port }
                    , updateValueFactory: (ip, ports) => { ports.Add(port); return ports; } );

                if (echoReply)
                {
                    var returnAddress = new IPEndPoint(receiveResult.RemoteEndPoint.Address, port);
                    int bytesSent = udpClient.Send(receiveResult.Buffer, receiveResult.Buffer.Length, returnAddress);
                    Console.WriteLine($"{recvMsg}. sent back to {returnAddress}");
                }
                else
                {
                    Console.WriteLine(recvMsg);
                }

                if (runForever)
                {
                    receiveTask = udpClient.ReceiveAsync(cancelToken).ConfigureAwait(false);
                }
                else
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"X: listen on port {port} got exception {ex.Message}");
            return;
        }
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
                    int sentBytes = socket.Send(buffer, buffer.Length, remote);
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
    static void PrintReplies(IReadOnlyDictionary<IPAddress, List<int>> replies, List<int> ports)
    {
        if (replies.Count == 0)
        {
            Console.WriteLine("no replies received");
        }
        else
        {
            foreach ( var ip in replies )
            {
                bool receivedOnAllPorts = ports.All( ip.Value.Contains );

                string result;
                if ( receivedOnAllPorts)
                {
                    result = "ALL ports ok";
                }
                else
                {
                    string missingPorts = String.Join(',', ports.Where(p => !ip.Value.Contains(p)));
                    result = $"missing ports: {missingPorts}";
                }
                Console.WriteLine($"{ip.Key}\t{result}");
            }
        }
    }
    static void HandleQuit(CancellationTokenSource cancelSource)
        {
            while (!cancelSource.IsCancellationRequested)
            {
                var key = Console.ReadKey(intercept: true);
                switch (key.KeyChar)
                {
                    case 'q': 
                        cancelSource.Cancel(); 
                        break;
                }
            }
        }
}