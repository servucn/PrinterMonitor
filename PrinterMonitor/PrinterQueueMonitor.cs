using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static UcAsp.Net.PrinterMonitor.PrintSpoolAPI;
using static UcAsp.Net.PrinterMonitor.PrinterAPI;
using System.Runtime.InteropServices;
using System.Management;

namespace UcAsp.Net.PrinterMonitor
{
    public class PrinterQueueMonitor
    {
        #region Events
        public event PrintJobStatusChanged OnJobStatusChange;
        /// <summary>
        /// 打印机状态监控
        /// </summary>
        public event PrinterStatusChanged OnPrinterStatusChange;
        #endregion
        #region private variables

        private List<string> spoolerName = new List<string>();

        private Dictionary<int, string> objJobDict = new Dictionary<int, string>();

        private List<PrinterModel> printerModels = new List<PrinterModel>();
        #endregion
        #region constructor

        public PrinterQueueMonitor(params string[] printerName)
        {

            spoolerName = printerName.ToList();
            foreach (string printer in spoolerName)
            {
                PrinterModel model = new PrinterModel { ChangeHandle = IntPtr.Zero, Error = false, Paused = false, PrinterHandle = IntPtr.Zero, PrinterName = printer, Spooler = null, WorkOffline = false };
                printerModels.Add(model);
            }


            Start();

        }

        public PrinterQueueMonitor()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Printer");
            foreach (var obj in searcher.Get())
            {

                string printer = obj["Name"].ToString();
                spoolerName.Add(printer);
                PrinterModel model = new PrinterModel { ChangeHandle = IntPtr.Zero, Error = false, Paused = false, PrinterHandle = IntPtr.Zero, PrinterName = printer, Spooler = null, WorkOffline = false };
                printerModels.Add(model);
            }
        }
        #endregion

        #region destructor
        ~PrinterQueueMonitor()
        {
            Stop();
        }
        #endregion
        #region StopMonitoring
        public void Stop()
        {
            foreach (PrinterModel printer in printerModels)
            {
                if (printer.PrinterHandle != IntPtr.Zero)
                {
                    ClosePrinter(printer.PrinterHandle);
                    printer.PrinterHandle = IntPtr.Zero;
                }
            }
        }
        #endregion


        private const int ERROR_INSUFFICIENT_BUFFER = 122;
        #region Callback Function
        public void PrinterNotifyWaitCallback(Object state, bool timedOut)
        {
            ManualResetEvent Mevent = (ManualResetEvent)state;
            PrinterModel printerModel = printerModels.FirstOrDefault(o => o.ChangeHandle == Mevent.Handle);

            #region read notification details
            printerModel.NotifyOptions.Count = 1;
            int pdwChange = 0;
            IntPtr pNotifyInfo = IntPtr.Zero;
            bool bResult = FindNextPrinterChangeNotification(printerModel.ChangeHandle, out pdwChange, printerModel.NotifyOptions, out pNotifyInfo);

            if ((bResult == false) || (((long)pNotifyInfo) == 0)) return;


            bool bJobRelatedChange = pdwChange > 0;

            if (!bJobRelatedChange) return;
            #endregion

            #region populate Notification Information

            PRINTER_NOTIFY_INFO info = (PRINTER_NOTIFY_INFO)Marshal.PtrToStructure(pNotifyInfo, typeof(PRINTER_NOTIFY_INFO));
            long pData = (long)pNotifyInfo + (long)Marshal.OffsetOf(typeof(PRINTER_NOTIFY_INFO), "aData");
            PRINTER_NOTIFY_INFO_DATA[] data = new PRINTER_NOTIFY_INFO_DATA[info.Count];
            for (uint i = 0; i < info.Count; i++)
            {
                data[i] = (PRINTER_NOTIFY_INFO_DATA)Marshal.PtrToStructure((IntPtr)pData, typeof(PRINTER_NOTIFY_INFO_DATA));
                pData += Marshal.SizeOf(typeof(PRINTER_NOTIFY_INFO_DATA));
            }
            #endregion

            #region iterate through all elements in the data array
            for (int i = 0; i < data.Count(); i++)
            {

                if ((data[i].Field == (ushort)PRINTERJOBNOTIFICATIONTYPES.JOB_NOTIFY_FIELD_STATUS) &&
                    (data[i].Type == (ushort)PRINTERNOTIFICATIONTYPES.JOB_NOTIFY_TYPE))
                {
                    JOBSTATUS jStatus = (JOBSTATUS)Enum.Parse(typeof(JOBSTATUS), data[i].NotifyData.Data.cbBuf.ToString());
                    int intJobID = (int)data[i].Id;
                    string strJobName = "";
                    PrintSystemJobInfo pji = null;
                    short shortJobCopies = 1;
                    uint uintJobTotalPages = 1;

                    try
                    {
                        printerModel.Spooler = new PrintQueue(new PrintServer(), printerModel.PrinterName);
                        pji = printerModel.Spooler.GetJob(intJobID);
                        if (!objJobDict.ContainsKey(intJobID))
                            objJobDict[intJobID] = pji.Name;
                        strJobName = pji.Name;


                        GetJob(printerModel.PrinterHandle, (uint)intJobID, 2, IntPtr.Zero, 0, out uint needed);
                        if (Marshal.GetLastWin32Error() == ERROR_INSUFFICIENT_BUFFER)
                        {
                            IntPtr buffer = Marshal.AllocHGlobal((int)needed);
                            GetJob(printerModel.PrinterHandle, (uint)intJobID, 2, buffer, needed, out needed);
                            JOB_INFO_2 jobInfo = (JOB_INFO_2)Marshal.PtrToStructure(buffer, typeof(JOB_INFO_2));
                            DEVMODE dMode = (DEVMODE)Marshal.PtrToStructure(jobInfo.pDevMode, typeof(DEVMODE));
                            shortJobCopies = dMode.dmCopies;
                            uintJobTotalPages = jobInfo.TotalPages;
                            Marshal.FreeHGlobal(buffer);
                        }
                    }
                    catch
                    {
                        pji = null;
                        objJobDict.TryGetValue(intJobID, out strJobName);
                        if (strJobName == null) strJobName = "";
                    }

                    OnJobStatusChange?.Invoke(this, new PrintJobChangeEventArgs(intJobID, strJobName, jStatus, pji, uintJobTotalPages, shortJobCopies));

                }
            }
            #endregion

            #region reset the Event and wait for the next event
            try
            {
                printerModel.MrEvent.Reset();
                printerModel.WaitHandle = ThreadPool.RegisterWaitForSingleObject(printerModel.MrEvent, new WaitOrTimerCallback(PrinterNotifyWaitCallback), printerModel.MrEvent, -1, true);
            }
            catch { }
            #endregion

        }
        #endregion

        private void WmiEventHandler(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject printer = (ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value;
            PrinterModel printerModel = printerModels.FirstOrDefault(o => o.PrinterName == printer["Name"].ToString());
            bool newPrinterStatus = bool.Parse(printer["WorkOffline"].ToString());
            int printerstatsu = int.Parse(printer["Printerstatus"].ToString());
            if (printerstatsu == 2 || printerstatsu == 0x400000 || printerstatsu == (int)PRINTERSTATUS.PRINTER_STATUS_OUT_OF_MEMORY)
            {
                printerModel.Error = true;
            }
            if (printerstatsu == (int)PRINTERSTATUS.PRINTER_STATUS_BUSY || printerstatsu == (int)PRINTERSTATUS.PRINTER_STATUS_PAUSED)
            {
                printerModel.Paused = true;
            }


            OnPrinterStatusChange?.Invoke(printer["Name"].ToString(), new PrinterChangeEventArgs(printer["Name"].ToString(), printerModel.WorkOffline, printerModel.Error, printerModel.Paused));
            printerModel.WorkOffline = newPrinterStatus;



        }
        public void Start()
        {
            foreach (string _spoolerName in spoolerName)
            {
                Console.WriteLine(_spoolerName);
                PrinterModel printerModel = printerModels.FirstOrDefault(o => o.PrinterName == _spoolerName);
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Printer");
                var unit = from ManagementObject x in searcher.Get()
                           where x.Properties["Name"].Value.ToString() == _spoolerName
                           select x;
                printerModel.WorkOffline = bool.Parse(unit.First()["WorkOffline"].ToString());
                int printerstatsu = int.Parse(unit.First()["Printerstatus"].ToString());
                if (printerstatsu == 2 || printerstatsu == 0x400000 || printerstatsu == (int)PRINTERSTATUS.PRINTER_STATUS_OUT_OF_MEMORY)
                {
                    printerModel.Error = true;
                }
                if (printerstatsu == (int)PRINTERSTATUS.PRINTER_STATUS_BUSY || printerstatsu == (int)PRINTERSTATUS.PRINTER_STATUS_PAUSED)
                {
                    printerModel.Paused = true;
                }
                string wmiQuery = "Select * From __InstanceModificationEvent Within 1 " +
                "Where TargetInstance ISA 'Win32_Printer' AND TargetInstance.Name ='" + _spoolerName + "'";
                ManagementEventWatcher watcher = new ManagementEventWatcher(new ManagementScope("\\root\\CIMV2"), new EventQuery(wmiQuery));
                watcher.EventArrived += new EventArrivedEventHandler(WmiEventHandler);
                watcher.Start();
                IntPtr _printerHandle = IntPtr.Zero;
                OpenPrinter(_spoolerName, out _printerHandle, 0);

                if (_printerHandle != IntPtr.Zero)
                {
                    Console.WriteLine(_spoolerName);
                    printerModel.PrinterHandle = _printerHandle;
                    //We got a valid Printer handle.  Let us register for change notification....
                    printerModel.ChangeHandle = FindFirstPrinterChangeNotification(printerModel.PrinterHandle, (int)PRINTER_CHANGES.PRINTER_CHANGE_JOB, 0, printerModel.NotifyOptions);
                    // We have successfully registered for change notification.  Let us capture the handle...
                    printerModel.MrEvent.SafeWaitHandle = new Microsoft.Win32.SafeHandles.SafeWaitHandle(printerModel.ChangeHandle, false);
                    //Now, let us wait for change notification from the printer queue....
                    printerModel.WaitHandle = ThreadPool.RegisterWaitForSingleObject(printerModel.MrEvent, new WaitOrTimerCallback(PrinterNotifyWaitCallback), printerModel.MrEvent, -1, true);
                }

                printerModel.Spooler = new PrintQueue(new PrintServer(), _spoolerName);
                foreach (PrintSystemJobInfo psi in printerModel.Spooler.GetPrintJobInfoCollection())
                {
                    objJobDict[psi.JobIdentifier] = psi.Name;
                }
            }
        }
    }
}
