using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using TrakHound.MTConnectSniffer;
using System.Xml;
using System.Net;

namespace TrakHound.DataClient.DeviceFinder
{
    public class DeviceFinder
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        private Sniffer sniffer;

        [XmlElement("Ports")]
        public PortRange Ports { get; set; }

        [XmlElement("Addresses")]
        public AddressRange Addresses { get; set; }

        [XmlAttribute("scanInterval")]
        public int ScanInterval { get; set; }

        public event Sniffer.DeviceHandler DeviceFound;
        public event Sniffer.RequestStatusHandler SearchCompleted;

        public void Start()
        {
            sniffer = new Sniffer();
            sniffer.RequestsCompleted += Sniffer_RequestsCompleted;
            sniffer.DeviceFound += Sniffer_DeviceFound;

            var ports = GetPortRange();
            if (ports != null) sniffer.PortRange = ports;

            var ips = GetAddressRange();
            if (ips != null) sniffer.AddressRange = ips;

            sniffer.Start();
        }

        private void Sniffer_DeviceFound(MTConnectDevice device)
        {
            DeviceFound?.Invoke(device);
        }

        private void Sniffer_RequestsCompleted(long milliseconds)
        {
            SearchCompleted?.Invoke(milliseconds);
        }

        private int[] GetPortRange()
        {
            if (Ports != null)
            {
                var l = new List<int>();

                // Add Allowed Ports
                if (Ports.AllowedPorts != null) l.AddRange(Ports.AllowedPorts);

                for (int i = Ports.Minimum; i <= Ports.Maximum; i++)
                {
                    bool allow = true;

                    // Check if in Denied list
                    if (Ports.DeniedPorts != null) allow = !Ports.DeniedPorts.ToList().Exists(o => o == i);

                    // Check if already added to list
                    allow = allow && !l.Exists(o => o == i);

                    if (allow) l.Add(i);
                }

                return l.ToArray();
            }

            return null;
        }

        private IPAddress[] GetAddressRange()
        {
            if (Addresses != null)
            {
                var l = new List<IPAddress>();

                // Add Allowed Ports
                if (Addresses.AllowedAddresses!= null) l.AddRange(GetIpAddressFromString(Addresses.AllowedAddresses));

                IPAddress min;
                IPAddress max;

                IPAddress.TryParse(Addresses.Minimum, out min);
                IPAddress.TryParse(Addresses.Maximum, out max);

                if (min != null && max != null)
                {
                    var minBytes = min.GetAddressBytes();
                    var maxBytes = max.GetAddressBytes();

                    var b = minBytes[3];
                    var e = maxBytes[3];

                    bool allow = true;

                    for (int i = b; i <= e; i++)
                    {
                        byte x = (byte)i;
                        var ip = new IPAddress(new byte[] { minBytes[0], minBytes[1], minBytes[2], x });

                        // Check if in Denied list
                        if (Addresses.DeniedAddresses != null) allow = !Addresses.DeniedAddresses.ToList().Exists(o => o.ToString() == ip.ToString());

                        // Check if already added to list
                        allow = allow && !l.Exists(o => o.ToString() == i.ToString());

                        if (allow) l.Add(ip);
                    }

                }

                return l.ToArray();
            }

            return null;
        }

        private IPAddress[] GetIpAddressFromString(string[] strings)
        {
            var l = new List<IPAddress>();

            foreach (var s in strings)
            {
                IPAddress ip;
                if (IPAddress.TryParse(s, out ip)) l.Add(ip);
            }

            return l.ToArray();
        }

    }
}
