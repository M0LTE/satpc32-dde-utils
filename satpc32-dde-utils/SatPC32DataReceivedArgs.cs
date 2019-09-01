using System;

namespace satpc32_dde_utils
{
    public class SatPC32DataReceivedArgs : EventArgs
    {
        public string SatelliteName { get; set; }
        public long UplinkFrequencyHz { get; set; }
        public string UplinkMode { get; set; }
        public long DownlinkFrequencyHz { get; set; }
        public string DownlinkMode { get; set; }
        public double Azimuth { get; set; }
        public double Elevation { get; set; }

        public SatPC32DataReceivedArgs(string satname, long uplink_freq, string uplink_mode, long downlink_freq, string downlink_mode, double az, double el)
        {
            this.SatelliteName = satname;
            this.UplinkFrequencyHz = uplink_freq;
            this.UplinkMode = uplink_mode;
            this.DownlinkFrequencyHz = downlink_freq;
            this.DownlinkMode = downlink_mode;
            this.Azimuth = az;
            this.Elevation = el;
        }
    }
}
