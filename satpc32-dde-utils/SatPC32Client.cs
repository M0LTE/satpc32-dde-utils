using NDde.Client;
using System;
using System.Diagnostics;
using System.Threading;

namespace satpc32_dde_utils
{
    public class SatPC32Client
    {
        public event EventHandler<SatPC32DataReceivedArgs> SatPC32DataReceived;

        public void Run(CancellationToken cancellationToken)
        {
            using (var client = new DdeClient("SatPC32", "SatPcDdeConv"))
            {
                client.Connect();
                client.StartAdvise("SatPcDdeItem", 1, true, 60000);

                while (true)
                {
                    var timer = Stopwatch.StartNew();

                    try
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            client.Disconnect();
                            client.Dispose();
                            break;
                        }

                        string satdata;

                        try
                        {
                            satdata = client.Request("SatPcDdeItem", 60000);
                        }
                        catch (InvalidOperationException)
                        {
                            // satpc32 has gone away
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(satdata))
                        {
                            continue;
                        }

                        if (satdata.Contains("** NO SATELLITE **"))
                        {
                            continue;
                        }

                        // SNFO-29 AZ56.7 EL6.8 UP145951275 UMLSB DN435853062 DMUSB MA52.6

                        string[] words = satdata.Split(' ');

                        string satname = "";
                        long downlink_freq = 0;
                        long uplink_freq = 0;
                        string downlink_mode = "";
                        string uplink_mode = "";
                        double az = 0, el = 0;

                        foreach (string word in words)
                        {
                            // Get Satellite Name
                            if (word.StartsWith("SN"))
                            {
                                satname = word.Remove(0, 2);
                            }
                            else if (word.StartsWith("AZ"))
                            {
                                double.TryParse(word.Remove(0, 2), out az);
                            }
                            else if (word.StartsWith("EL"))
                            {
                                double.TryParse(word.Remove(0, 2), out el);
                            }
                            else if (word.StartsWith("UP"))
                            {
                                long.TryParse(word.Remove(0, 2), out uplink_freq);
                            }
                            else if (word.StartsWith("UM"))
                            {
                                uplink_mode = word.Remove(0, 2);
                            }
                            else if (word.StartsWith("DN"))
                            {
                                long.TryParse(word.Remove(0, 2), out downlink_freq);
                            }
                            else if (word.StartsWith("DM"))
                            {
                                downlink_mode = word.Remove(0, 2);
                            }
                        }

                        if (az != 0 && el != 0)
                        {
                            SatPC32DataReceived?.Invoke(this, new SatPC32DataReceivedArgs(satname, uplink_freq, uplink_mode, downlink_freq, downlink_mode, az, el));
                        }
                    }
                    finally
                    {
                        if (timer.Elapsed < TimeSpan.FromSeconds(1))
                        {
                            Thread.Sleep(TimeSpan.FromSeconds(1) - timer.Elapsed);
                        }
                    }
                }
            }
        }
    }
}