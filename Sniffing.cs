using SharpPcap.LibPcap;
using SharpPcap;
using PacketDotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using NetFwTypeLib;

namespace DxAntiFirewall
{
    public partial class Siniffer : Form
    {
        private int indexSel;
        private int pkgNumber = 1;
        private string strTime = "",
            ipSource = "",
            ipDestination = "",
            protocolType = "",
            pkgLength = "";
        private bool againStartCapturing = false;
        private string captureFile = Path.Combine(Environment.CurrentDirectory, "dxcap.pcap");

        private Thread Sniffing;
        private List<LibPcapLiveDevice> libPcapLiveDevices = new List<LibPcapLiveDevice>();
        private LibPcapLiveDevice ipDevice;
        private CaptureFileWriterDevice captureFileWriterDevice;
        //private CaptureFileReaderDevice captureFileReaderDevice;
        private Dictionary<int, Packet> keyValuePairs = new Dictionary<int, Packet>();

        public Siniffer(List<LibPcapLiveDevice> liveDevice, int indexSelx)
        {
            InitializeComponent();

            this.libPcapLiveDevices = liveDevice;
            indexSel = indexSelx;
            ipDevice = libPcapLiveDevices[indexSel];

            LoadFirewallRules();
        }

        private void Siniffer_FormClosed(object sender, FormClosedEventArgs e) => Application.Exit();

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (!againStartCapturing)
            {
                File.Delete(captureFile);

                ipDevice.OnPacketArrival += new PacketArrivalEventHandler(Device_OnPacketArrival);
                Sniffing = new Thread(new ThreadStart(proccessSniffing));
                Sniffing.Start();

                toolStripButton1.Enabled = false;
                toolStripButton2.Enabled = true;
                siticoneTextBox1.Enabled = false;

                againStartCapturing = true;
            }
            else
            {
                if (MessageBox.Show("Paketleriniz bir dosyada yakalanır. Yeni bir yakalama başlatmak mevcut olanları geçersiz kılacaktır.", "Onayla", MessageBoxButtons.OK, MessageBoxIcon.Warning) == DialogResult.OK)
                {
                    File.Delete(captureFile);

                    listView1.Items.Clear();
                    keyValuePairs.Clear();
                    pkgNumber = 1;
                    listView2.Clear();

                    ipDevice.OnPacketArrival += new PacketArrivalEventHandler(Device_OnPacketArrival);
                    Sniffing = new Thread(new ThreadStart(proccessSniffing));
                    Sniffing.Start();

                    toolStripButton1.Enabled = false;
                    toolStripButton2.Enabled = true;
                    siticoneTextBox1.Enabled = false;

                    againStartCapturing = true;
                }
            }
        }

        private void stopProccess()
        {
            try
            {
                Sniffing.Abort();
                ipDevice.StopCapture();
                ipDevice.Close();

                if (captureFileWriterDevice != null)
                {
                    captureFileWriterDevice.Close();
                    captureFileWriterDevice = null;
                }

                toolStripButton1.Enabled = true;
                siticoneTextBox1.Enabled = true;
                toolStripButton2.Enabled = false;
            } catch {}
        }

        private void chooseInterfacelStripMenuItem_Click(object sender, EventArgs e)
        {
            Interface ınterface = new Interface();
            Hide();
            ınterface.Show();
        }

        private void saveFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PCAP File | *.pcap";
            saveFileDialog.Title = "DxAntiFirewall | Save File";
            saveFileDialog.FileName = "dxcap.pcap";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    ipDevice.Open(DeviceMode.Promiscuous, 1000);

                    if (ipDevice.Opened)
                    {
                        captureFileWriterDevice = new CaptureFileWriterDevice(ipDevice, saveFileDialog.FileName);
                    }
                } catch { }
            }
        }

        private void LoadFirewallRules()
        {
            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

            listView3.Items.Clear();

            foreach (INetFwRule rule in firewallPolicy.Rules)
            {
                if (rule.Name.StartsWith("Block IP"))
                {
                    ListViewItem item = new ListViewItem(rule.Name);
                    item.SubItems.Add(rule.Action == NET_FW_ACTION_.NET_FW_ACTION_ALLOW ? "Allow" : "Block");
                    item.SubItems.Add(rule.RemoteAddresses);
                    listView3.Items.Add(item.Text.Replace("Block IP ", ""));
                }
            }
        }

        private void blockStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                string ipToBlock = listView1.SelectedItems[0].SubItems[2].Text;
                
                Firewall.BlockIP(ipToBlock);
                MessageBox.Show($"{ipToBlock} IP adresi engellendi.");
                LoadFirewallRules();
            }
        }

        private void unblockStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                string ipToBlock = listView1.SelectedItems[0].SubItems[2].Text;

                Firewall.UnblockIP(ipToBlock);
                MessageBox.Show($"{ipToBlock} IP adresin engeli kaldırıldı.");
                LoadFirewallRules();
            }
        }

        private void contextMenuStrip1_Opened(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                contextMenuStrip1.Items[0].Text = listView1.SelectedItems[0].SubItems[2].Text;
            } else
            {
                contextMenuStrip1.Items[0].Text = "Hiç bir IP seçilmemiş";
            }
        }

        /*private void ReadPcapFile(string filePath)
        {
            int packetsRead = 0;
            var startTime = DateTime.Now;
            RawCapture rawCapture = null;
            while (packetsRead < 100)
            {
                var captureDevice = new CaptureFileReaderDevice(filePath);
                captureDevice.Open();

                do
                {
                    rawCapture = captureDevice.GetNextPacket();
                    packetsRead++;
                }
                while (rawCapture != null);

                captureDevice.Close();
            }

            Console.WriteLine(rawCapture);
        }

        private void openFIleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "PCAP Files (*.pcap)|*.pcap";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                ReadPcapFile(openFileDialog.FileName);
            }
        }*/

        private void toolStripButton2_Click(object sender, EventArgs e) => stopProccess();
        private void listView1_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            string protocol = e.Item.SubItems[4].Text;
            int key = Int32.Parse(e.Item.SubItems[0].Text);
            Packet packet;
            bool getPacket = keyValuePairs.TryGetValue(key, out packet);

            switch (protocol)
            {
                case "TCP":
                    if (getPacket)
                    {
                        var pkgTCP = (TcpPacket)packet.Extract(typeof(TcpPacket));

                        if (pkgTCP != null)
                        {
                            int portSrc = pkgTCP.SourcePort;
                            int portDst = pkgTCP.SourcePort;
                            int checksum = pkgTCP.Checksum;

                            listView2.Clear();

                            string[] columnHeaders = {
                                "Packet Number",
                                "Type",
                                "Source Port",
                                "Destination Port",
                                "TCP Header Size",
                                "Window Size",
                                "Checksum",
                                "TCP Checksum",
                                "Sequence Number",
                                "Acknowledgment Number",
                                "Urgent Pointer",
                                "ACK Flag",
                                "PSH Flag",
                                "RST Flag",
                                "SYN Flag",
                                "FIN Flag",
                                "ECN Flag",
                                "CWR Flag",
                                "NS Flag"
                            };

                            listView2.Columns.Add("Attributes").Width = 125;
                            listView2.Columns.Add("Values").Width = 120;

                            for (int i = 0; i < columnHeaders.Length; i++)
                            {
                                ListViewItem item = new ListViewItem();

                                item.Text = columnHeaders[i];
                                item.SubItems.Add("");

                                listView2.Items.Add(item);
                            }

                            listView2.Items[0].SubItems[1].Text = key.ToString();
                            listView2.Items[1].SubItems[1].Text = "TCP";
                            listView2.Items[2].SubItems[1].Text = portSrc.ToString();
                            listView2.Items[3].SubItems[1].Text = portDst.ToString();
                            listView2.Items[4].SubItems[1].Text = pkgTCP.DataOffset.ToString();
                            listView2.Items[5].SubItems[1].Text = pkgTCP.WindowSize.ToString();
                            listView2.Items[6].SubItems[1].Text = (checksum.ToString() ?? "none") + (pkgTCP.ValidChecksum ? ", valid" : ", invalid");
                            listView2.Items[7].SubItems[1].Text = pkgTCP.ValidChecksum ? "valid" : "invalid";
                            listView2.Items[8].SubItems[1].Text = pkgTCP.SequenceNumber.ToString();
                            listView2.Items[9].SubItems[1].Text = (pkgTCP.AcknowledgmentNumber.ToString() ?? "none") + (pkgTCP.Ack ? ", valid" : ", invalid");
                            listView2.Items[10].SubItems[1].Text = pkgTCP.Urg ? "valid" : "invalid";
                            listView2.Items[11].SubItems[1].Text = pkgTCP.Ack ? "1" : "0";
                            listView2.Items[12].SubItems[1].Text = pkgTCP.Psh ? "1" : "0";
                            listView2.Items[13].SubItems[1].Text = pkgTCP.Rst ? "1" : "0";
                            listView2.Items[14].SubItems[1].Text = pkgTCP.Syn ? "1" : "0";
                            listView2.Items[15].SubItems[1].Text = pkgTCP.Fin ? "1" : "0";
                            listView2.Items[16].SubItems[1].Text = pkgTCP.ECN ? "1" : "0";
                            listView2.Items[17].SubItems[1].Text = pkgTCP.CWR ? "1" : "0";
                            listView2.Items[18].SubItems[1].Text = pkgTCP.NS ? "1" : "0";
                        }
                    }
                    break;

                case "UDP":
                    if (getPacket)
                    {
                        var pkgUDP = (UdpPacket)packet.Extract(typeof(UdpPacket));
                        if (pkgUDP != null)
                        {
                            int portSrc = pkgUDP.SourcePort;
                            int portDst = pkgUDP.SourcePort;
                            int checksum = pkgUDP.Checksum;

                            listView2.Clear();

                            string[] columnHeaders = {
                                "Packet Number",
                                "Type",
                                "Source Port",
                                "Destination Port",
                                "Checksum",
                                "Valid UDP Checksum"
                            };

                            listView2.Columns.Add("Attributes").Width = 125;
                            listView2.Columns.Add("Values").Width = 120;

                            for (int i = 0; i < columnHeaders.Length; i++)
                            {
                                ListViewItem item = new ListViewItem();

                                item.Text = columnHeaders[i];
                                item.SubItems.Add("");

                                listView2.Items.Add(item);
                            }

                            listView2.Items[0].SubItems[1].Text = key.ToString();
                            listView2.Items[1].SubItems[1].Text = "UDP";
                            listView2.Items[2].SubItems[1].Text = portSrc.ToString();
                            listView2.Items[3].SubItems[1].Text = portDst.ToString();
                            listView2.Items[4].SubItems[1].Text = checksum.ToString();
                            listView2.Items[5].SubItems[1].Text = pkgUDP.ValidUDPChecksum.ToString();
                        }
                    }
                    break;

                case "ARP":
                    if (getPacket)
                    {
                        var pkgARP = (ARPPacket)packet.Extract(typeof(ARPPacket));
                        if (pkgARP != null)
                        {
                            IPAddress ipAddress = pkgARP.SenderProtocolAddress;
                            IPAddress targetAddress = pkgARP.TargetProtocolAddress;

                            System.Net.NetworkInformation.PhysicalAddress ipHardwareAddress = pkgARP.SenderHardwareAddress;
                            System.Net.NetworkInformation.PhysicalAddress targetHardwareAddress = pkgARP.TargetHardwareAddress;

                            listView2.Clear();

                            string[] columnHeaders = {
                                "Packet Number",
                                "Type",
                                "Hardware Address Length",
                                "Protocol Address Length",
                                "Operation",
                                "Sender Protocol Address",
                                "Target Protocol Address",
                                "Sender Hardware Address",
                                "Target Hardware Address",
                            };

                            listView2.Columns.Add("Attributes").Width = 125;
                            listView2.Columns.Add("Values").Width = 120;

                            for (int i = 0; i < columnHeaders.Length; i++)
                            {
                                ListViewItem item = new ListViewItem();

                                item.Text = columnHeaders[i];
                                item.SubItems.Add("");

                                listView2.Items.Add(item);

                                listView2.Items[0].SubItems[1].Text = key.ToString();
                                listView2.Items[1].SubItems[1].Text = "ARP";
                                listView2.Items[2].SubItems[1].Text = pkgARP.HardwareAddressLength.ToString();
                                listView2.Items[3].SubItems[1].Text = pkgARP.ProtocolAddressLength.ToString();
                                listView2.Items[4].SubItems[1].Text = pkgARP.Operation.ToString();
                                listView2.Items[5].SubItems[1].Text = ipAddress.ToString();
                                listView2.Items[6].SubItems[1].Text = targetAddress.ToString();
                                listView2.Items[7].SubItems[1].Text = ipHardwareAddress.ToString();
                                listView2.Items[8].SubItems[1].Text = targetHardwareAddress.ToString();
                            }
                        }
                    }
                    break;

                case "ICMP":
                    if (getPacket)
                    {
                        var pkgICMP = (ICMPv4Packet)packet.Extract(typeof(ICMPv4Packet));
                        if (pkgICMP != null)
                        {
                            listView2.Clear();

                            string[] columnHeaders = {
                                "Packet Number",
                                "Type",
                                "Type Code",
                                "Checksum",
                                "ID",
                                "Sequence Number",
                            };

                            listView2.Columns.Add("Attributes").Width = 125;
                            listView2.Columns.Add("Values").Width = 120;

                            for (int i = 0; i < columnHeaders.Length; i++)
                            {
                                ListViewItem item = new ListViewItem();

                                item.Text = columnHeaders[i];
                                item.SubItems.Add("");

                                listView2.Items.Add(item);

                                listView2.Items[0].SubItems[1].Text = key.ToString();
                                listView2.Items[1].SubItems[1].Text = "ICMP v4";
                                listView2.Items[2].SubItems[1].Text = "0x" + pkgICMP.TypeCode.ToString("x");
                                listView2.Items[3].SubItems[1].Text = pkgICMP.Checksum.ToString("x");
                                listView2.Items[4].SubItems[1].Text = pkgICMP.ID.ToString("x");
                                listView2.Items[5].SubItems[1].Text = pkgICMP.Sequence.ToString("x");
                            }
                        }
                    }
                    break;

                case "IGMP":
                    if (getPacket)
                    {
                        var pkgIGMP = (IGMPv2Packet)packet.Extract(typeof(IGMPv2Packet));
                        if (pkgIGMP != null)
                        {
                            listView2.Clear();

                            string[] columnHeaders = {
                                "Packet Number",
                                "Type",
                                "TypeV2",
                                "Group Address",
                                "Max Response Time",
                            };

                            listView2.Columns.Add("Attributes").Width = 125;
                            listView2.Columns.Add("Values").Width = 120;

                            for (int i = 0; i < columnHeaders.Length; i++)
                            {
                                ListViewItem item = new ListViewItem();

                                item.Text = columnHeaders[i];
                                item.SubItems.Add("");

                                listView2.Items.Add(item);

                                listView2.Items[0].SubItems[1].Text = key.ToString();
                                listView2.Items[1].SubItems[1].Text = "IGMP v2";
                                listView2.Items[2].SubItems[1].Text = pkgIGMP.Type.ToString();
                                listView2.Items[3].SubItems[1].Text = pkgIGMP.GroupAddress.ToString();
                                listView2.Items[4].SubItems[1].Text = pkgIGMP.MaxResponseTime.ToString();
                            }
                        }
                    }
                    break;

                default:
                    listView2.Clear();
                    break;
            }
        }

        private void Device_OnPacketArrival(object sender, CaptureEventArgs e)
        {
            DateTime dateTime = e.Packet.Timeval.Date;
            strTime = (dateTime.Hour + 1) + ":" + dateTime.Minute + ":" + dateTime.Second;
            pkgLength = e.Packet.Data.Length.ToString();
            var packet = Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);

            try
            {
                if (!keyValuePairs.ContainsKey(pkgNumber))
                {
                    keyValuePairs.Add(pkgNumber, packet);
                }
            } catch { }

            var ipPkg = (IpPacket)packet.Extract(typeof(IpPacket));
            if (ipPkg != null)
            {
                IPAddress ipSrc = ipPkg.SourceAddress;
                IPAddress ipDst = ipPkg.DestinationAddress;
                protocolType = ipPkg.Protocol.ToString();
                ipSource = ipSrc.ToString();
                ipDestination = ipDst.ToString();

                ListViewItem listViewItem = new ListViewItem(pkgNumber.ToString());
                listViewItem.SubItems.Add(strTime);
                listViewItem.SubItems.Add(ipSource);
                listViewItem.SubItems.Add(ipDestination);
                listViewItem.SubItems.Add(protocolType);
                listViewItem.SubItems.Add(pkgLength);

                Action action = () => listView1.Items.Add(listViewItem);
                listView1.Invoke(action);
                ++pkgNumber;
            }
        }

        private void proccessSniffing()
        {
            try
            {
                ipDevice.Open(DeviceMode.Promiscuous, 1000);

                if (ipDevice.Opened)
                {
                    if (!string.IsNullOrEmpty(siticoneTextBox1.Text)) ipDevice.Filter = siticoneTextBox1.Text.ToLower();
                    ipDevice.Capture();
                    ipDevice.Filter = siticoneTextBox1.Text;
                }
            } catch {}
        }
    }
}
