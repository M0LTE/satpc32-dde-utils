# satpc32-dde-utils
DDE utils for SatPC. Connects SatPC32 to a TS-2000 or SDR Console (via VSPE), and emits MacDoppler-format UDP datagrams containing pass updates.

Windows-only - requires .NET Framework 4.7.2.

# Usage

First off, start SatPC32.

## Send tuning commands to SDR Console (or a TS-2000)

First establish a virtual serial port pair using VSPE or similar. For VSPE, use either device type Connector (same COM port at both ends) or Pair (different COM port at both ends).

Then: 

`satpc32-dde-utils.exe --rig-control --comport com1 --baud 57600`

## Emit MacDoppler-format UDP datagrams containing pass updates
`satpc32-dde-utils.exe --udp 192.168.1.0:2345`

Both of the above commands can be combined.

To just watch what data comes out of SatPC32, start SatPC32 then start this software with no command line arguments.
