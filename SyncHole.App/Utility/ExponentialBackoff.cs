using System;
using System.Threading.Tasks;

namespace SyncHole.App.Utility
{
    public class ExponentialBackoff
    {
        private readonly int _delay, _maxDelay;
        private int _pow;

        public ExponentialBackoff(int delay, int maxDelay)
        {
            _delay = delay;
            _maxDelay = maxDelay;
            _pow = 1;
        }

        public Task DelayAsync(bool expand)
        {
            if (expand)
            {
                _pow = _pow << 1;
            }
            else
            {
                _pow = 1;
            }

            var currentDelay = Math.Min(_delay * _pow / 2, _maxDelay);
            return Task.Delay(currentDelay);
        }
    }
}
