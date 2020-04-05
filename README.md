# PrinterMonitor
打印机状态和打印任务状态监控


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
