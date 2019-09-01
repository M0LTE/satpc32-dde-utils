using System;
using System.Threading;

namespace satpc32_dde_utils
{
    public interface IRigController : IDisposable
    {
        event EventHandler<FreqEventArgs> FrequencyChanged;

        long GetFrequencyHz();
        bool SetFrequencyHz(long hz, CancellationToken cancellationToken);

        void StartFrequencyUpdates(CancellationToken cancellationToken);
    }

    public class FreqEventArgs : EventArgs
    {
        public long FrequencyHz { get; set; }
    }
}