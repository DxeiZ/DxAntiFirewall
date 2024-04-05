using NetFwTypeLib;
using System;

namespace DxAntiFirewall
{
    class Firewall
    {
        private static INetFwPolicy2 GetFirewallPolicy()
        {
            Type type = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            return (INetFwPolicy2)Activator.CreateInstance(type);
        }

        public static void BlockIP(string ipAddress)
        {
            INetFwPolicy2 firewallPolicy = GetFirewallPolicy();
            INetFwRule firewallRule = (INetFwRule)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));

            firewallRule.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
            firewallRule.Description = "Blocked IP Address: " + ipAddress;
            firewallRule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
            firewallRule.Enabled = true;
            firewallRule.InterfaceTypes = "All";
            firewallRule.Protocol = 256;
            firewallRule.RemoteAddresses = ipAddress;
            firewallRule.Name = "Block IP " + ipAddress;

            firewallPolicy.Rules.Add(firewallRule);
        }

        public static void UnblockIP(string ipAddress)
        {
            INetFwPolicy2 firewallPolicy = GetFirewallPolicy();

            foreach (INetFwRule rule in firewallPolicy.Rules)
            {
                if (rule.Name == "Block IP " + ipAddress)
                {
                    firewallPolicy.Rules.Remove(rule.Name);
                    break;
                }
            }
        }
    }
}
