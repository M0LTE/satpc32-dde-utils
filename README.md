# satpc32-dde-utils
DDE utils for SatPC. Connects SatPC32 to a TS-2000 or SDR Console (via VSPE), and emits MacDopper-format UDP datagrams containing pass updates.

# Usage

## Send tuning commands to SDR Console (or a TS-2000):
`satpc32-dde-utils.exe --rig-control --comport com1 --baud 57600`

## Emit MacDopper-format UDP datagrams containing pass updates
`satpc32-dde-utils.exe --udp 192.168.1.0:2345`

Both of the above commands can be combined.
