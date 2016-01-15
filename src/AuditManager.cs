using System;
using System.Configuration;
using System.Threading;
using System.Timers;
using FileAuditManager.Hashing;
using log4net;
using Timer = System.Timers.Timer;

namespace FileAuditManager
{
    class AuditManager
    {
        private static ILog Log = LogManager.GetLogger(typeof(AuditManager));
        private static readonly object LockObject = new object();
        private readonly Timer timer;
        private readonly IApplicationHashingManager hashingManager;

        public AuditManager(IApplicationHashingManager hashingManager)
        {
            //todo: DI this.
            this.hashingManager = hashingManager ?? new ApplicationHashingManager(null, null, null);

            var milliseconds = long.Parse(ConfigurationManager.AppSettings["AuditTimerInMilliseconds"]);
            timer = new Timer(milliseconds);
            timer.Elapsed += Timer_Elapsed;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Monitor.TryEnter(LockObject))
            {
                try
                {
                    hashingManager.HashAllActiveApplications();
                }
                catch (Exception ex)
                {
                    Log.Error("Error in AuditManager timer handler:", ex);
                }
                Monitor.Exit(LockObject);
            }
        }

        public void Start()
        {
            timer.Start();
        }

        public void Stop()
        {
            timer.Stop();
        }

        public void Pause()
        {
            timer.Stop();
        }

        public void Continue()
        {
            timer.Start();
        }
    }
}
