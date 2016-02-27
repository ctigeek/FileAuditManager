using System;
using System.ServiceProcess;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;

namespace FileAuditManager
{
    static class Program
    {
        static ILog log = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            XmlConfigurator.Configure();
            if (args.Length > 0)
            {
                if (args[0] == "debug")
                {
                    RunCommandLine();
                }
                else
                {
                    Console.WriteLine("Error. Use `debug` to run in command line.");
                    Console.ReadLine();
                }
            }
            else
            {
                ServiceBase.Run(new ServiceManager());
            }
        }

        private static void RunCommandLine()
        {
            AddCommandLineLogger();
            log.Info("Running in command-line mode.");
            try
            {
                var actionManager = new WebAppManager();
                actionManager.Start();

                var auditManager = new AuditManager(null, null, null, null, null);
                auditManager.Start();

                Console.WriteLine("Press enter to stop....");
                Console.ReadLine();

                actionManager.Stop();
                auditManager.Stop();
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
                Console.WriteLine("Press enter to stop....");
                Console.ReadLine();
            }
        }

        private static void AddCommandLineLogger()
        {
            //remove application logger...
            var root = ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root;
            var attachable = (IAppenderAttachable)root;
            attachable.RemoveAllAppenders();

            //add console logger...
            var layout = new PatternLayout("%message%newline");
            layout.ActivateOptions();
            var appender = new ConsoleAppender { Layout = layout, Threshold = Level.Debug };
            BasicConfigurator.Configure(appender);
            // set level to Debug
            ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root.Level = Level.Debug;
            ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).RaiseConfigurationChanged(EventArgs.Empty);
        }
    }
}
