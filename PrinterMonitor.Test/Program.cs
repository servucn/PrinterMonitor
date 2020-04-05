using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UcAsp.Net.PrinterMonitor;
namespace PrinterMonitor.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            PrinterQueueMonitor printerMonitor = new PrinterQueueMonitor();

            printerMonitor.OnPrinterStatusChange += (o, e) => {
                Console.WriteLine(o);
                Console.WriteLine(e.WorkOffLine);
            };
            printerMonitor.OnJobStatusChange += (o, e) =>
            {
                Console.WriteLine(e.JobID + "." + e.JobName + "." + e.JobStatus + "." + e.JobTotalPages);
            };
            printerMonitor.Start();
            Console.ReadKey();
        }


    }
}
