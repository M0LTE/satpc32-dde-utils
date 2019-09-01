using CommandLine;
using NDde;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace satpc32_dde_utils
{
    class Program
    {
        private class Options
        {
            [Option(longName: "rig-control", HelpText = "Enable rig control - TS2000 only (supports SDR Console CAT via VSPE)")] public bool EnableRigControl { get; set; }
            [Option(longName: "comport", HelpText = "e.g. COM4")] public string RigControlPort { get; set; }
            [Option(longName: "baud", HelpText = "e.g. 57600")] public int RigControlBaud { get; set; }
            [Option(longName: "udp", HelpText = "Send MacDoppler UDP datagrams to hostname:port (e.g. 10.45.0.0:2345)")] public string UdpOutput { get; set; }
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Pass --help for instructions.");
            }

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(Run)
                .WithNotParsed(HandleParseErrors);
        }

        private static void HandleParseErrors(IEnumerable<Error> errors)
        {
            foreach (var error in errors)
            {
            }
        }

        static void Run(Options options)
        {
            IRigController sdrConsoleController = null;

            if (options.EnableRigControl)
            {
                if (string.IsNullOrWhiteSpace(options.RigControlPort))
                {
                    Console.WriteLine("Missing option: --comport COMn (e.g. --port COM4)");
                    return;
                }

                if (options.RigControlBaud == 0)
                {
                    Console.WriteLine("Missing option: --baud n (e.g. --baud 9600)");
                    return;
                }

                try
                {
                    sdrConsoleController = new Ts2000Controller(options.RigControlPort, options.RigControlBaud, TimeSpan.FromSeconds(1));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return;
                }
            }

            string udpHost;
            int udpPort;
            if (!String.IsNullOrWhiteSpace(options.UdpOutput))
            {
                var split = options.UdpOutput.Split(':');
                if (split.Length != 2)
                {
                    Console.WriteLine("Invalid value for option --udp, expected hostname:port");
                    return;
                }

                udpHost = split[0];
                if (!int.TryParse(split[1], out udpPort) || udpPort < 1 || udpPort > 65535)
                {
                    Console.WriteLine("Invalid port in option --udp, expected a value 1-65535");
                    return;
                }
            }
            else
            {
                udpHost = null;
                udpPort = 0;
            }

            using (var udpClient = new UdpClient())
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var satPC32Client = new SatPC32Client();
                satPC32Client.SatPC32DataReceived += ((object sender, SatPC32DataReceivedArgs e) =>
                {
                    Console.WriteLine($"{e.SatelliteName} az={e.Azimuth} el={e.Elevation} down={e.DownlinkFrequencyHz} {e.DownlinkMode} up={e.UplinkFrequencyHz} {e.UplinkMode}");

                    if (options.EnableRigControl)
                    {
                        SetRigFrequency(e, sdrConsoleController);
                    }

                    if (!string.IsNullOrWhiteSpace(udpHost))
                    {
                        EmitUdpDatagram(e, udpHost, udpPort, udpClient, cancellationTokenSource);
                    }
                });

                Console.WriteLine("Click a satellite in SatPC32. You'll see lines appearing here when the selected sat is over the horizon.");
                Console.WriteLine("Press CTRL-C to quit.");
                try
                {
                    satPC32Client.Run(cancellationTokenSource.Token);
                }
                catch (DdeException ex)
                {
                    Console.WriteLine("Could not connect to SatPC32 - is it running? Full error message follows.");
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private static void SetRigFrequency(SatPC32DataReceivedArgs e, IRigController sdrConsoleController)
        {
            // control an emulated TS-2000
            using (var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(0.9)))
            {
                if (!sdrConsoleController.SetFrequencyHz(e.DownlinkFrequencyHz, timeoutTokenSource.Token))
                {
                    Console.WriteLine("Warning, could not set rig frequency, continuing...");
                }
            }
        }

        private static void EmitUdpDatagram(SatPC32DataReceivedArgs e, string udpHost, int udpPort, UdpClient udpClient, CancellationTokenSource cancellationTokenSource)
        {
            // emit UDP broadcasts that look like this:
            // [Sat Radio Report:Down Mhz:435.18000, Down Mode:FM, Up MHz:145.98000, Up Mode:FM]

            string frame = $"[Sat Radio Report:Down Mhz:{e.DownlinkFrequencyHz / 1000000.0:5}, Down Mode:{e.DownlinkMode}, Up MHz:{e.UplinkFrequencyHz / 1000000.0:5}, Up Mode:{e.UplinkMode}]";
            byte[] datagram = Encoding.ASCII.GetBytes(frame);
            try
            {
                udpClient.Send(datagram, datagram.Length, udpHost, udpPort);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending UDP datagram: " + ex.Message);
                Console.WriteLine("Aborting.");
                cancellationTokenSource.Cancel();
            }
        }
    }
}