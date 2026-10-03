using System;
using System.Diagnostics;
using System.Threading;

namespace YANF.Tests
{
    /// <summary>
    /// Bounded waits for conditions set by other threads (never a fixed sleep that assumes a timing).
    /// </summary>
    internal static class Poll
    {
        #region Fields
        /// <summary>
        /// Default upper bound of a wait: generous, because it only costs time when the test is failing anyway.
        /// </summary>
        public const int TIMEOUT_MS = 15000;
        #endregion

        #region Methods
        /// <summary>
        /// Polls <paramref name="condition"/> until it holds or the timeout elapses; returns its final value.
        /// </summary>
        public static bool Until(Func<bool> condition, int timeoutMs = TIMEOUT_MS)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (condition())
                {
                    return true;
                }
                Thread.Sleep(20);
            }
            return condition();
        }
        #endregion
    }
}
