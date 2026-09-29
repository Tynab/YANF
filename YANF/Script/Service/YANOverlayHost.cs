using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using YANF.Screen;
using static System.Environment;
using static System.Threading.ApartmentState;
using static System.Threading.Monitor;

namespace YANF.Script.Service
{
    /// <summary>
    /// Runs one overlay screen on a dedicated background STA thread and marshals every access to it through BeginInvoke.
    /// <see cref="Start"/> blocks until the screen has loaded (its handle exists), failed or timed out.
    /// <see cref="Publish"/> and <see cref="Close"/> are safe from any thread, in any order and any number of times,
    /// and do nothing once the overlay is closed. The screen never gets an owner on another thread.
    /// </summary>
    internal sealed class YANOverlayHost<T> where T : AnonScreen
    {
        #region Fields
        private const int READY_TIMEOUT = 5000;
        private static readonly object _systemEventsSync = new();
        private static bool _is_SystemEventsReady;
        private readonly object _sync = new();
        private readonly Func<T> _create;
        private Mailbox _mailbox;
        private T _scr;
        private Action<T> _pendingUpdate;
        private Exception _startError;
        private bool _is_Started;
        private bool _is_Loaded;
        private bool _is_Finished;
        private bool _is_CloseRequested;
        private bool _is_UpdateQueued;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a host; <paramref name="create"/> runs on the overlay thread.
        /// </summary>
        public YANOverlayHost(Func<T> create) => _create = create;
        #endregion

        #region Methods
        /// <summary>
        /// Starts the overlay thread and waits until the screen has loaded. A failure before that point is rethrown here.
        /// If the screen does not load in time, the wait ends and the screen closes itself as soon as it loads.
        /// </summary>
        public void Start()
        {
            lock (_sync)
            {
                if (_is_Started)
                {
                    return;
                }
                _is_Started = true;
                if (_is_CloseRequested)
                {
                    // Closed before it was ever started: nothing to show
                    _is_Finished = true;
                    return;
                }
            }
            EnsureSystemEvents();
            var thread = new Thread(Run)
            {
                IsBackground = true,
                Name = $"YANF {typeof(T).Name}"
            };
            thread.SetApartmentState(STA);
            thread.Start();
            Exception error;
            lock (_sync)
            {
                var sw = Stopwatch.StartNew();
                while (!_is_Loaded && !_is_Finished)
                {
                    var left = READY_TIMEOUT - (int)sw.ElapsedMilliseconds;
                    if (left <= 0 || !Wait(_sync, left))
                    {
                        break;
                    }
                }
                if (!_is_Loaded && !_is_Finished)
                {
                    // Stop waiting but never orphan the window: the late Load sees the request and closes it
                    _is_CloseRequested = true;
                    _pendingUpdate = null;
                }
                error = _startError;
                _startError = null;
            }
            if (error != null)
            {
                ExceptionDispatchInfo.Capture(error).Throw();
            }
        }

        /// <summary>
        /// Queues an update of the screen's controls. Only the latest pending update runs, on the overlay thread.
        /// An update published before the screen has loaded is applied when it loads.
        /// </summary>
        public void Publish(Action<T> update)
        {
            lock (_sync)
            {
                if (_is_CloseRequested || _is_Finished)
                {
                    return;
                }
                _pendingUpdate = update;
                if (_is_Loaded && !_is_UpdateQueued)
                {
                    _is_UpdateQueued = TryPost(ApplyUpdate);
                }
            }
        }

        /// <summary>
        /// Closes the overlay (Frm_Close on its own thread) exactly once, whether or not it has loaded yet.
        /// </summary>
        public void Close()
        {
            lock (_sync)
            {
                if (_is_CloseRequested)
                {
                    return;
                }
                // Keep _pendingUpdate: an ApplyUpdate already posted runs before CloseScreen, so the last value shows while fading out
                _is_CloseRequested = true;
                if (_is_Loaded && !_is_Finished)
                {
                    _ = TryPost(CloseScreen);
                }
            }
        }

        // SystemEvents created from an STA thread shares that thread and dies with it: let an MTA thread create it first (once per overlay type), so it gets its own thread as with the old MTA overlay thread
        private static void EnsureSystemEvents()
        {
            if (!UserInteractive)
            {
                // SystemEvents then lives on whichever thread creates it, whatever its apartment: nothing to change
                return;
            }
            lock (_systemEventsSync)
            {
                if (_is_SystemEventsReady)
                {
                    return;
                }
                _is_SystemEventsReady = true;
                var thread = new Thread(TouchSystemEvents)
                {
                    IsBackground = true,
                    Name = "YANF SystemEvents init"
                };
                thread.SetApartmentState(MTA);
                thread.Start();
                thread.Join();
            }
        }

        // MTA thread: one subscription initialises SystemEvents on its own thread (nothing happens when it already exists)
        private static void TouchSystemEvents()
        {
            try
            {
                UserPreferenceChangedEventHandler handler = (_, _) => { };
                SystemEvents.UserPreferenceChanged += handler;
                SystemEvents.UserPreferenceChanged -= handler;
            }
            catch (Exception ex) when (ex is InvalidOperationException or ExternalException)
            {
                // SystemEvents unavailable here: the overlay runs as before
            }
        }

        // Overlay thread: create the mailbox and the screen, run the modal loop, always dispose both
        private void Run()
        {
            Mailbox mailbox = null;
            T scr = null;
            try
            {
                mailbox = new Mailbox();
                _ = mailbox.Handle;
                lock (_sync)
                {
                    _mailbox = mailbox;
                }
                scr = _create();
                scr.Load += Scr_Load;
                _ = scr.ShowDialog(NoOwner.Instance);
            }
            catch (Exception ex) when (TrySetStartError(ex))
            {
                // Rethrown by Start on the caller's thread
            }
            finally
            {
                // Mark finished before any window goes away, so no caller posts to a dying handle
                lock (_sync)
                {
                    _is_Finished = true;
                    _mailbox = null;
                    _scr = null;
                    _pendingUpdate = null;
                    PulseAll(_sync);
                }
                scr?.Dispose();
                mailbox?.Dispose();
            }
        }

        // Keep a failure that happened before the screen loaded for Start (later failures stay unhandled, as before)
        private bool TrySetStartError(Exception ex)
        {
            lock (_sync)
            {
                if (_is_Loaded)
                {
                    return false;
                }
                _startError = ex;
                return true;
            }
        }

        // Overlay thread: the handle exists now, release Start and replay what arrived before it
        private void Scr_Load(object sender, EventArgs e)
        {
            lock (_sync)
            {
                _scr = (T)sender;
                _is_Loaded = true;
                if (_is_CloseRequested)
                {
                    _ = TryPost(CloseScreen);
                }
                else if (_pendingUpdate != null)
                {
                    _is_UpdateQueued = TryPost(ApplyUpdate);
                }
                PulseAll(_sync);
            }
        }

        // Overlay thread: apply the latest pending update
        private void ApplyUpdate()
        {
            T scr;
            Action<T> update;
            lock (_sync)
            {
                _is_UpdateQueued = false;
                update = _pendingUpdate;
                _pendingUpdate = null;
                scr = _scr;
            }
            if (update != null && scr != null && !scr.IsDisposed)
            {
                update(scr);
            }
        }

        // Overlay thread: run the screen's own close (fade out, dispose) unless it is already gone
        private void CloseScreen()
        {
            T scr;
            lock (_sync)
            {
                scr = _scr;
            }
            if (scr != null && !scr.IsDisposed)
            {
                scr.Frm_Close();
            }
        }

        // Post to the overlay thread (caller holds _sync, so the mailbox cannot be destroyed meanwhile); false when it is gone
        private bool TryPost(MethodInvoker method)
        {
            try
            {
                _ = _mailbox.BeginInvoke(method);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
        #endregion

        #region Nested types
        /// <summary>
        /// Hidden top-level window of the overlay thread that receives the marshalled calls.
        /// Unlike the screen, nothing destroys it but the host, after it is marked finished.
        /// </summary>
        private sealed class Mailbox : System.Windows.Forms.Control
        {
            public Mailbox()
            {
                // Hide first: SetTopLevel creates a visible control right away, which would be a stray window with a taskbar button
                Visible = false;
                SetTopLevel(true);
            }
        }

        /// <summary>
        /// Explicit "no owner" for ShowDialog. The parameterless overload adopts the active window as owner,
        /// which must never be a window of another thread (that would also attach the two input queues).
        /// </summary>
        private sealed class NoOwner : IWin32Window
        {
            public static readonly NoOwner Instance = new();

            public IntPtr Handle => IntPtr.Zero;
        }
        #endregion
    }
}
