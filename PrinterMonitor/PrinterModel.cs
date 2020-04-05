using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static UcAsp.Net.PrinterMonitor.PrintSpoolAPI;

namespace UcAsp.Net.PrinterMonitor
{
    internal class PrinterModel
    {
        public string PrinterName { get; set; }
        public IntPtr PrinterHandle { get; set; }

        public IntPtr ChangeHandle { get; set; }

        public PrintQueue Spooler { get; set; }

        public bool WorkOffline { get; set; }

        public bool Error { get; set; }

        public bool Paused { get; set; }

        public ManualResetEvent MrEvent = new ManualResetEvent(false);

        public RegisteredWaitHandle WaitHandle { get; set; }

        public PRINTER_NOTIFY_OPTIONS NotifyOptions = new PRINTER_NOTIFY_OPTIONS();
    }
}
