using System;
using System.Threading;

namespace satpc32_dde_utils
{
    public interface IRigController : IDisposable
    {
        event EventHandler<FreqEventArgs> FrequencyChanged;
        event EventHandler<ModeEventArgs> ModeChanged;

        long GetFrequencyHz();
        bool SetFrequencyHz(long hz, CancellationToken cancellationToken);
        bool SetMode(Mode mode, CancellationToken cancellationToken);

        void StartPolling(CancellationToken cancellationToken);
    }

    public class FreqEventArgs : EventArgs
    {
        public long FrequencyHz { get; set; }
    }

    public class ModeEventArgs : EventArgs
    {
        public Mode Mode { get; set; }
    }
}