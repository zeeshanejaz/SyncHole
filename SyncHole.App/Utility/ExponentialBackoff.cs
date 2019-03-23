using System;
using System.Threading.Tasks;

namespace SyncHole.App.Utility
{
    public class ExponentialBackoff
    {
        private const int MaxPow = 100000;
        private readonly int _delay, _maxDelay;
        private int _pow;

        public ExponentialBackoff(int delay, int maxDelay)
        {
            _delay = delay;
            _maxDelay = maxDelay;
            _pow = 1;
        }

        public Task DelayAsync(bool increment)
        {
            if (increment)
            {
                _pow = _pow << 1;
            }

            if (!increment || _pow > MaxPow)
            {
                _pow = 1;
            }

            var currentDelay = Math.Min(_delay * _pow, _maxDelay);
            return Task.Delay(currentDelay);
        }
    }
}
