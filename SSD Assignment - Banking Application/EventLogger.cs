using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Principal;

namespace Banking_Application
{
    // ######################
    // Very small helper to write to the Windows Event Log in a simple way.
    // I keep the strings easy to read because I am new to this.
    // ######################
    public static class EventLogger
    {
        private const string SourceName = "SSD Banking Application";
        private const string LogName = "Application";
        private static bool _sourceReady = false;

        // ######################
        // Make sure the Event Log source exists. If I do not have admin rights
        // the try/catch stops the whole app from crashing.
        // ######################
        private static void EnsureSource()
        {
            if (_sourceReady)
                return;

            try
            {
                if (!EventLog.SourceExists(SourceName))
                {
                    EventLog.CreateEventSource(SourceName, LogName);
                }
                _sourceReady = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Event Log source creation failed: " + ex.Message);
                _sourceReady = false;
            }
        }

        // ######################
        // Log any banking transaction (add/close/lodge/withdraw/view)
        // WHO: teller + account holder, WHAT: transaction type, WHERE: machine info,
        // WHEN: UTC timestamp, WHY: optional reason for >€10k, HOW: app metadata.
        // ######################
        public static void LogTransaction(string teller, string accountNo, string accountHolder, string transactionType, double amount, string status, string reasonOptional = "", string extra = "")
        {
            EnsureSource();

            string machineInfo = $"Machine={Environment.MachineName}; IP={GetLocalIp()}; SID={GetSid()}";
            string appMeta = "App=SSD Banking Application; Version=1.0";
            string why = string.IsNullOrWhiteSpace(reasonOptional) ? "Reason=N/A" : $"Reason={reasonOptional}";
            string entry = $"WHO_TELLER={teller}; WHO_ACCOUNT={accountHolder}({accountNo}); WHAT={transactionType}; AMOUNT={amount}; STATUS={status}; WHERE={machineInfo}; WHEN={DateTime.UtcNow:o}; {why}; HOW={appMeta}; EXTRA={extra}";

            try
            {
                var type = status.Equals("FAIL", StringComparison.OrdinalIgnoreCase) ? EventLogEntryType.Warning : EventLogEntryType.Information;
                EventLog.WriteEntry(SourceName, entry, type);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Event Log write failed: " + ex.Message);
            }
        }

        // ######################
        // Log authentication attempts (will be used later when AD auth is added).
        // ######################
        public static void LogAuth(string username, string outcome, string machineInfo = "")
        {
            EnsureSource();

            string where = string.IsNullOrWhiteSpace(machineInfo) ? $"Machine={Environment.MachineName}; SID={GetSid()}" : machineInfo;
            string entry = $"AUTH_USER={username}; OUTCOME={outcome}; WHERE={where}; WHEN={DateTime.UtcNow:o}; APP=SSD Banking Application";

            try
            {
                var type = outcome.Equals("FAIL", StringComparison.OrdinalIgnoreCase) ? EventLogEntryType.FailureAudit : EventLogEntryType.SuccessAudit;
                EventLog.WriteEntry(SourceName, entry, type);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Event Log write failed: " + ex.Message);
            }
        }

        private static string GetSid()
        {
            try
            {
                return WindowsIdentity.GetCurrent().User?.Value ?? "unknown";
            }
            catch
            {
                return "unknown";
            }
        }

        // ######################
        // Get a simple IPv4 address for WHERE info.
        // I keep this loop easy so I can understand it.
        // ######################
        private static string GetLocalIp()
        {
            try
            {
                foreach (var addr in Dns.GetHostAddresses(Dns.GetHostName()))
                {
                    if (addr.AddressFamily == AddressFamily.InterNetwork)
                        return addr.ToString();
                }
                return "unknown";
            }
            catch
            {
                return "unknown";
            }
        }
    }
}
