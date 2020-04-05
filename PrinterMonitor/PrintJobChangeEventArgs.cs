using System;
using System.Collections.Generic;
using System.Text;
using static UcAsp.Net.PrinterMonitor.PrintSpoolAPI;
using System.Printing;
namespace UcAsp.Net.PrinterMonitor
{
    public class PrintJobChangeEventArgs : EventArgs
    {
        public int JobID { get; } = 0;
        public string JobName { get; } = "";
        public JOBSTATUS JobStatus { get; } = new JOBSTATUS();
        public PrintSystemJobInfo JobInfo { get; } = null;
        public uint JobTotalPages { get; set; } = 0;
        public short JobCopies { get; set; } = 0;
        public PrintJobChangeEventArgs(int intJobID, string strJobName, JOBSTATUS jStatus, PrintSystemJobInfo objJobInfo, uint uintJobTotalPages, short shortJobCopies)
            : base()
        {
            JobID = intJobID;
            JobName = strJobName;
            JobStatus = jStatus;
            JobInfo = objJobInfo;
            JobTotalPages = uintJobTotalPages;
            JobCopies = shortJobCopies;
        }
    }

    public delegate void PrintJobStatusChanged(object sender, PrintJobChangeEventArgs e);
    public class PrinterChangeEventArgs : EventArgs
    {
        public string PrinterName { get; } = "";
        public bool WorkOffLine { get; } = false;

        public bool Error { get; } = false;

        public bool Paused { get; } = false;
        public PrinterChangeEventArgs(string printerName, bool workOffline, bool error, bool paused)
        {
            PrinterName = printerName;
            WorkOffLine = workOffline;
            Error = error;
            Paused = paused;

        }
    }
    public delegate void PrinterStatusChanged(object sender, PrinterChangeEventArgs e);

}
