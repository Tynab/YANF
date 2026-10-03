using Xunit;

// UI tests share process-wide state (Application.OpenForms, Control.CheckForIllegalCrossThreadCalls, the screen that
// placeholder checks capture, window focus), so the test classes run one after another
[assembly: CollectionBehavior(DisableTestParallelization = true)]
