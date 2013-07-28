﻿namespace NPerf.Lab
{
    using System;
    using System.Reactive.Disposables;
    using System.Reactive.Subjects;
    using System.Threading;
    using System.Threading.Tasks;
    using NPerf.Core.Communication;
    using NPerf.Core.PerfTestResults;

    internal class SingleExperimentListener : IObservable<PerfTestResult>, IDisposable
    {
        private readonly ExperimentProcess experiment;

        private readonly ProcessMailBox mailBox;

        private static EventWaitHandle wh = new AutoResetEvent(true);

        private bool parallel;

        private readonly Action<ExperimentProcess> startProcess;

        public SingleExperimentListener(ExperimentProcess experiment, Action<ExperimentProcess> startProcess, bool parallel = false)
        {
            this.startProcess = startProcess;
            this.parallel = parallel;                   
            this.experiment = experiment;
            this.mailBox = new ProcessMailBox(this.experiment.ChannelName, TimeSpan.FromMilliseconds(-1));
        }

        protected void Run(IObserver<PerfTestResult> observer)
        {
            var ok = true;
            try
            {
                this.startProcess(this.experiment);

                object message;
                Thread.CurrentThread.Name = this.mailBox.ChannelName;

                var buff = new ReplaySubject<PerfTestResult>();
                buff.Subscribe(observer);
                do
                {
                    message = this.mailBox.Content as PerfTestResult;
                    
                    if (message != null)
                    {
                        buff.OnNext((PerfTestResult)message);
                        //observer.OnNext((PerfTestResult)message);
                    }
                }
                while (!(message is ExperimentError && ((PerfTestResult)message).Descriptor == -1)
                       && !(message is ExperimentCompleted) && !this.experiment.HasExited);            
            }
            catch (ThreadAbortException)
            {
            }
            catch (Exception ex)
            {
                ok = false;
                observer.OnError(ex);
            }

            if (ok)
            {
                observer.OnCompleted();
            }
        }

        public IDisposable Subscribe(IObserver<PerfTestResult> observer)
        {
            if (!this.parallel)
            {
                wh.WaitOne();
            }

            var task = Task.Factory.StartNew(() => this.Run(observer));           

            return Disposable.Create(
                () =>
                    {
                        if (!this.parallel)
                        {
                            wh.Set();
                        }

                        task.Wait(TimeSpan.FromMilliseconds(10));
                      //  task.Dispose();
                    });
        }

        public void Dispose()
        {
            this.mailBox.Dispose();
        }
    }
}
