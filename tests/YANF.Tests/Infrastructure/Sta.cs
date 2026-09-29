using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using static System.Threading.ApartmentState;

namespace YANF.Tests
{
    /// <summary>
    /// Runs WinForms test code on a dedicated STA thread. xunit runs tests on MTA thread-pool threads, while WinForms
    /// windows belong to the thread that creates them and need an STA thread. Every window of a test lives and dies on
    /// its own thread, so tests cannot leak handles or thread state into each other.
    /// </summary>
    internal static class Sta
    {
        #region Fields
        private static readonly TimeSpan TIMEOUT = TimeSpan.FromMinutes(3);
        #endregion

        #region Methods
        /// <summary>
        /// Runs <paramref name="body"/> on a new STA thread with a fresh <see cref="Ui"/> scope and rethrows its failure on the
        /// calling thread. Exceptions raised inside window procedures (which WinForms reports through Application.ThreadException
        /// instead of throwing them at the caller) fail the test too.
        /// </summary>
        public static void Run(Action<Ui> body)
        {
            Exception error = null;
            var thread = new Thread(() =>
            {
                try
                {
                    Ui.Run(body);
                }
                catch (Exception ex)
                {
                    error = ex;
                }
            })
            {
                IsBackground = true,
                Name = "YANF.Tests STA"
            };
            thread.SetApartmentState(STA);
            thread.Start();
            if (!thread.Join(TIMEOUT))
            {
                throw new TimeoutException($"The STA test body did not finish within {TIMEOUT.TotalSeconds} s.");
            }
            if (error != null)
            {
                ExceptionDispatchInfo.Capture(error).Throw();
            }
        }

        /// <summary>
        /// Runs <paramref name="body"/> on a new STA thread (see <see cref="Run(Action{Ui})"/>).
        /// </summary>
        public static void Run(Action body) => Run(_ => body());
        #endregion
    }
}
