using System.Net;
using Spi;

namespace UDPProbe;
class Opts
    {
        public required List<int> Ports { get; init; }
        public required List<IPAddress> IPs { get; init; }
        //public required bool Listen { get; set; }
        public required bool SendForever { get; init; }
    }

class Options {

    public static Opts? GetOpts(string[] args)
    {
        string? ports = null;
        bool help = false;
        bool sendForever = false;

        var optDefinitions = new Spi.BeeOptsBuilder()
            .Add('p', "ports", Spi.OPTTYPE.VALUE, "ports (comma separated)", arg => ports = arg)
            .Add('s', "send",  Spi.OPTTYPE.BOOL,  "send forever", arg => sendForever = true)
            .Add('h', "help",  Spi.OPTTYPE.BOOL,  "show usage", arg => help = true )
            .GetOpts();

        var arguments = Spi.BeeOpts.Parse(args, optDefinitions, (string unknowOption) => Console.Error.WriteLine($"unknown option [{unknowOption}]") );

        if ( help )
        {
            Console.WriteLine("usage: UDPTest [options] [IPaddress...]");
            BeeOpts.PrintOptions(optDefinitions);
            return null;
        }

        var opts = new Opts()
        {
            Ports = ParsePorts(ports),
            IPs   = ParseIPs(arguments),
            SendForever = sendForever
        };

        return opts;
    }
    static List<int> ParsePorts(string? CsvPorts)
    {
        if ( String.IsNullOrEmpty(CsvPorts) ) 
        {
            throw new Exception("specify at least one port");
        }
        else
        {
            return 
                CsvPorts.Split(',').Select( strPort => Convert.ToInt32(strPort)).ToList();
        }
    }
    static List<IPAddress> ParseIPs(IEnumerable<string> strIPs)
    {
        List<IPAddress> validIPs = new List<IPAddress>();

        foreach( string strIP in strIPs )
        {
            if ( ! IPAddress.TryParse(strIP, out IPAddress? ipaddress) )
            {
                Console.Error.WriteLine($"E: could not parse IPAddress [{strIP}]");
            }
            else
            {
                validIPs.Add(ipaddress);
            }
        }

        return validIPs;
    }
}