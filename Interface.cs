using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using SharpPcap.LibPcap;

namespace DxAntiFirewall
{
    public partial class Interface : Form
    {
        List<LibPcapLiveDevice> libPcapLiveDevices = new List<LibPcapLiveDevice>();

        public Interface()
        {
            InitializeComponent();
            StartWriting();
        }

        //      /**\    Start Write Device List    /**\        //
        private void StartWriting()
        {
            LibPcapLiveDeviceList liveDevices = LibPcapLiveDeviceList.Instance;

            foreach (LibPcapLiveDevice liveDevice in liveDevices)
            {
                if (!liveDevice.Interface.Addresses.Exists(before => before != null
                && before.Addr != null
                && before.Addr.ipAddress != null))
                    continue;

                var deviceInterface = liveDevice.Interface;
                var friendlyName = liveDevice.Interface?.FriendlyName ?? "Yok";
                var description = liveDevice.Interface?.Description ?? "Yok";
                var macAddress = liveDevice.Interface?.MacAddress?.ToString() ?? "Yok";
                var ipAddress = liveDevice.Interface?.Addresses[0].Addr?.ToString() ?? "Yok";

                libPcapLiveDevices.Add(liveDevice);

                ListViewItem items = new ListViewItem();
                items.Text = friendlyName;
                items.SubItems.Add(description);
                items.SubItems.Add(FormatMacAddress(macAddress));
                items.SubItems.Add(ipAddress);
                listView1.Items.Add(items);
            }
        }

        //      /**\    Format for Mac Address Interface    /**\        //
        public static string FormatMacAddress(string macAddress)
        {
            if (macAddress.Length != 12)
            {
                throw new ArgumentException("ERROR");
            }

            var sb = new StringBuilder();
            for (int i = 0; i < macAddress.Length; i += 2)
            {
                sb.Append($"{macAddress[i]}{macAddress[i + 1]}:");
            }

            sb.Remove(sb.Length - 1, 1);

            return sb.ToString();
        }

        //      /**\    Get Send Selected Index Sniffing Form    /**\        //
        private void siticoneButton1_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.SelectedItems)
            {
                Siniffer siniffer = new Siniffer(libPcapLiveDevices, item.Index);
                Hide();
                siniffer.Show();
            }
        }
    }
}
