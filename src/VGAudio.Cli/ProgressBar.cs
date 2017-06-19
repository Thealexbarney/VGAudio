// Adapted from https://gist.github.com/0ab6a96899cc5377bf54

using System;
using System.Text;
using System.Threading;

namespace VGAudio.Cli
{
    public class ProgressBar : IDisposable, IProgressReport
    {
        private const int BlockCount = 20;
        private int _progress;
        private int _total;
        private readonly Timer _timer;

        private readonly TimeSpan _animationInterval = TimeSpan.FromSeconds(1.0 / 30);
        private static readonly string Animation = @"|/-\";

        private string _currentText = string.Empty;
        private bool _disposed;
        private int _animationIndex;

        public ProgressBar()
        {
            var timerCallBack = new TimerCallback(TimerHandler);
            _timer = new Timer(timerCallBack, 0, 0, 0);
        }

        public void Report(int value)
        {
            Interlocked.Exchange(ref _progress, value);
        }

        public void ReportAdd(int value)
        {
            Interlocked.Add(ref _progress, value);
        }

        public void ReportMessage(string message)
        {
            lock (_timer)
            {
                Console.WriteLine($"\r{message}");
                _currentText = string.Empty;
                TimerHandler(null);
            }
        }

        public void ReportTotal(int value)
        {
            Interlocked.Exchange(ref _total, value);
        }

        private void TimerHandler(object state)
        {
            lock (_timer)
            {
                if (_disposed) return;

                double progress = _total == 0 ? 0 : (double)_progress / _total;
                int progressBlockCount = (int)(progress * BlockCount);
                string text = $"[{new string('#', progressBlockCount)}{new string('-', BlockCount - progressBlockCount)}] {_progress}/{_total} {progress:P1} {Animation[_animationIndex++ % Animation.Length]}";
                UpdateText(text);

                ResetTimer();
            }
        }

        private void UpdateText(string text)
        {
            // Get length of common portion
            int commonPrefixLength = 0;
            int commonLength = Math.Min(_currentText.Length, text.Length);
            while (commonPrefixLength < commonLength && text[commonPrefixLength] == _currentText[commonPrefixLength])
            {
                commonPrefixLength++;
            }

            // Backtrack to the first differing character
            StringBuilder outputBuilder = new StringBuilder();
            outputBuilder.Append('\b', _currentText.Length - commonPrefixLength);

            // Output new suffix
            outputBuilder.Append(text.Substring(commonPrefixLength));

            // If the new text is shorter than the old one: delete overlapping characters
            int overlapCount = _currentText.Length - text.Length;
            if (overlapCount > 0)
            {
                outputBuilder.Append(' ', overlapCount);
                outputBuilder.Append('\b', overlapCount);
            }

            Console.Write(outputBuilder);
            _currentText = text;
        }

        private void ResetTimer()
        {
            _timer.Change(_animationInterval, TimeSpan.FromMilliseconds(-1));
        }

        public void Dispose()
        {
            lock (_timer)
            {
                _disposed = true;
                UpdateText(string.Empty);
            }
        }
    }
}
