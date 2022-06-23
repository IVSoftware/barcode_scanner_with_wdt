using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace barcode_scanner_with_wdt
{
    class WatchdogTimer
    {
        public WatchdogTimer(TimeSpan timeSpan)
        {
            TimeSpan = timeSpan;
        }
        public void StartOrRestart(int eventCount)
        {
            if(_currentTask != null)
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _currentTask?.Dispose();
            }
            if (_stopwatch.IsRunning)
            {
                _eventCount += eventCount;
            }
            else
            {
                _eventCount = eventCount;
                _stopwatch.Restart();
            }
            _elapsed = _stopwatch.Elapsed;
            _cts = new CancellationTokenSource();
            _currentTask = Task.Delay((int)TimeSpan.TotalMilliseconds, _cts.Token);
            _currentTask.ContinueWith((task) =>
            {
                if(task.Status.Equals(TaskStatus.RanToCompletion))
                {
                    _stopwatch.Stop();
                    Completed?.Invoke(this, new CompletedEventArgs(_eventCount, _elapsed));
                }
                task.Dispose();
            });
        }
        public event CompletedEventHandler Completed;
        private Task _currentTask = null;
        private CancellationTokenSource _cts = null;
        private int _eventCount = 0;
        private TimeSpan _elapsed = TimeSpan.Zero;
        private Stopwatch _stopwatch = new Stopwatch();
        public TimeSpan TimeSpan { get; }
        public bool IsIdle => !_stopwatch.IsRunning;
    }

    delegate void CompletedEventHandler(Object sender, CompletedEventArgs e);
    class CompletedEventArgs : EventArgs
    {
        public CompletedEventArgs(int eventCount, TimeSpan elapsed)
        {
            EventCount = eventCount;
            TotalElapsed = elapsed;
        }

        public TimeSpan TotalElapsed { get; }
        public int EventCount { get; }
    }
}
