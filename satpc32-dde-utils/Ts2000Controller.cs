using System;
using System.Linq;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Threading;

namespace satpc32_dde_utils
{
    /// <summary>
    /// Implement a client for the TS-2000, or radios that emulate it, or SDR Console, which emulates it, via VSPE
    /// </summary>
    public class Ts2000Controller : IRigController
    {
        public event EventHandler<FreqEventArgs> FrequencyChanged;
        public event EventHandler<ModeEventArgs> ModeChanged;
        private readonly SerialPort serialPort;
        private readonly TimeSpan rigPollInterval;
        private readonly object lockObj = new object();
        private long freqHz;
        private Mode mode;

        public Ts2000Controller(string comPort, int baudRate, TimeSpan rigPollInterval)
        {
            this.rigPollInterval = rigPollInterval;

            serialPort = new SerialPort(comPort, baudRate);
            serialPort.ReadTimeout = 500;
            serialPort.Open();
        }

        public void StartPolling(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(()=>PollRig(cancellationToken), TaskCreationOptions.LongRunning);
        }

        private void PollRig(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    long hz = ReadFrequencyFromRig(cancellationToken);

                    if (hz == 0)
                        continue;

                    if (freqHz != hz)
                    {
                        if (freqHz != 0)
                        {
                            FrequencyChanged?.Invoke(null, new FreqEventArgs { FrequencyHz = hz });
                        }
                        freqHz = hz;
                    }

                    Mode m = ReadModeFromRig(cancellationToken);
                    if (m == Mode.Undefined)
                        continue;

                    if (m != mode)
                    {
                        ModeChanged?.Invoke(null, new ModeEventArgs { Mode = m });
                        mode = m;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.GetType().Name} in {nameof(PollRig)}: {ex.Message}");
                }
                finally
                {
                    Thread.Sleep(rigPollInterval);
                }
            }
        }

        public long GetFrequencyHz()
        {
            return freqHz;
        }

        private long ReadFrequencyFromRig(CancellationToken cancellationToken)
        {
            string response = null;

            lock (lockObj)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    serialPort.Write("FA;");

                    try
                    {
                        response = ReadResponse();
                        break;
                    }
                    catch (TimeoutException)
                    {
                    }
                }
            }

            if (response == null)
            {
                return 0;
            }

            if (!response.StartsWith("FA") || response.Length != 14 || !response.EndsWith(";"))
            {
                return 0;
            }

            if (!long.TryParse(new String(response.Skip(2).Take(11).ToArray()), out long hz))
            {
                return 0;
            }

            return hz;
        }

        private string ReadResponse()
        {
            var chars = new List<char>();
            while (true)
            {
                int b = serialPort.ReadByte();
                chars.Add((char)b);
                if (b == ';')
                    break;
            }

            string response = new string(chars.ToArray());

            return response;
        }

        public bool SetFrequencyHz(long hz, CancellationToken cancellationToken)
        {
            lock (lockObj)
            {
                serialPort.Write($"FA{hz:D11};");
                freqHz = hz;
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (ReadFrequencyFromRig(cancellationToken) == hz)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        Dictionary<Mode, int> modeMap = new Dictionary<Mode, int>
        {
            { Mode.LSB, 1 },
            { Mode.USB, 2 },
            { Mode.CW, 3 },
            { Mode.FM, 4 },
            { Mode.AM, 5 },
            { Mode.FSK, 6 },
            //{ Mode.ReverseSidebandCW, 7 },
            //{ Mode.ReverseSidebandFSK, 9 },
        };

        public bool SetMode(Mode mode, CancellationToken cancellationToken)
        {
            lock (lockObj)
            {
                serialPort.Write($"MD{modeMap[mode]};");
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (ReadModeFromRig(cancellationToken) == mode)
                    {
                        this.mode = mode;
                        return true;
                    }
                }
            }

            return false;
        }

        private Mode ReadModeFromRig(CancellationToken cancellationToken)
        {
            string response = null;

            lock (lockObj)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    serialPort.Write("MD;");

                    try
                    {
                        response = ReadResponse();
                        break;
                    }
                    catch (TimeoutException)
                    {
                    }
                }
            }

            if (response == null)
            {
                return 0;
            }

            if (!response.StartsWith("MD") || response.Length != 4 || !response.EndsWith(";"))
            {
                return 0;
            }

            if (int.TryParse(new string(response.Skip(2).Take(1).ToArray()), out int modeVal))
            {
                if (modeMap.ContainsValue(modeVal))
                {
                    var kvp = modeMap.Single(k => k.Value == modeVal);
                    return kvp.Key;
                }
            }

            return Mode.Undefined;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    serialPort.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}