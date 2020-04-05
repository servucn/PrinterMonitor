using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using static UcAsp.Net.PrinterMonitor.PrintSpoolAPI;

namespace UcAsp.Net.PrinterMonitor
{
    internal class PrinterAPI
    {
        #region DLL Import Functions
        [DllImport("winspool.drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool OpenPrinter(String pPrinterName,
        out IntPtr phPrinter,
        Int32 pDefault);

        [DllImport("winspool.drv",
        EntryPoint = "ClosePrinter",
        SetLastError = true,
        ExactSpelling = true,
        CallingConvention = CallingConvention.StdCall)]
        public static extern bool ClosePrinter
        (IntPtr hPrinter);

        [DllImport("winspool.drv",
        EntryPoint = "FindFirstPrinterChangeNotification",
        SetLastError = true, CharSet = CharSet.Ansi,
        ExactSpelling = true,
        CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr FindFirstPrinterChangeNotification
                            ([InAttribute()] IntPtr hPrinter,
                            [InAttribute()] Int32 fwFlags,
                            [InAttribute()] Int32 fwOptions,
                            [InAttribute(), MarshalAs(UnmanagedType.LPStruct)] PRINTER_NOTIFY_OPTIONS pPrinterNotifyOptions);

        [DllImport("winspool.drv", EntryPoint = "FindNextPrinterChangeNotification",
        SetLastError = true, CharSet = CharSet.Ansi,
        ExactSpelling = false,
        CallingConvention = CallingConvention.StdCall)]
        public static extern bool FindNextPrinterChangeNotification
                            ([InAttribute()] IntPtr hChangeObject,
                             [OutAttribute()] out Int32 pdwChange,
                             [InAttribute(), MarshalAs(UnmanagedType.LPStruct)] PRINTER_NOTIFY_OPTIONS pPrinterNotifyOptions,
                            [OutAttribute()] out IntPtr lppPrinterNotifyInfo
                                 );

        [DllImport(
        "winspool.drv",
        EntryPoint = "GetJob",
        SetLastError = true,
        ExactSpelling = false,
        CallingConvention = CallingConvention.StdCall)]
        public static extern bool GetJob
        ([InAttribute()] IntPtr hPrinter,
        [InAttribute()] UInt32 JobId,
        [InAttribute()] UInt32 Level,
        [OutAttribute()] IntPtr pJob,
        [InAttribute()] UInt32 cbBuf,
        [OutAttribute()] out UInt32 pcbNeeded);
        #endregion


    }
}
