using System;
using System.Linq;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Threading;

namespace satpc32_dde_utils
{
    /// <summary>
    /// Implement a client for the TS-2000, or radios that emulate it, or SDR Console
    /// </summary>
    public class Ts2000Controller : IRigController
    {
        public event EventHandler<FreqEventArgs> FrequencyChanged;
        private readonly SerialPort serialPort;
        private readonly TimeSpan rigPollInterval;
        private readonly object lockObj = new object();
        private long freqHz;

        public Ts2000Controller(string comPort, int baudRate, TimeSpan rigPollInterval)
        {
            this.rigPollInterval = rigPollInterval;

            serialPort = new SerialPort(comPort, baudRate);
            serialPort.ReadTimeout = 500;
            serialPort.Open();
        }

        public void StartFrequencyUpdates(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(()=>PollRig(cancellationToken), TaskCreationOptions.LongRunning);
        }

        private void PollRig(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                long hz = ReadFrequencyFromRig(cancellationToken);

                if (hz == 0)
                    return;

                if (freqHz != hz)
                {
                    if (freqHz != 0)
                    {
                        FrequencyChanged?.Invoke(null, new FreqEventArgs { FrequencyHz = hz });
                    }
                    freqHz = hz;
                }

                Thread.Sleep(rigPollInterval);
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