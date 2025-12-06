using System;
using System.Threading;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

// Utility to run EditMode tests from command line via:
// Unity -batchmode -projectPath <path> -executeMethod AutomatedTestRunner.RunEditModeTests -quit
public static class AutomatedTestRunner
{
    private class RunCallback : ICallbacks
    {
        private readonly ManualResetEventSlim _doneEvent;
        public int FailedCount { get; private set; }

        public RunCallback(ManualResetEventSlim doneEvent)
        {
            _doneEvent = doneEvent;
            FailedCount = 0;
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
            // no-op
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            if (result == null)
            {
                FailedCount = 1;
            }
            else
            {
                FailedCount = result.FailCount;
            }
            _doneEvent.Set();
        }

        public void TestStarted(ITestAdaptor test)
        {
            // no-op
        }

        public void TestFinished(ITestResultAdaptor result)
        {
            // no-op
        }
    }

    public static void RunEditModeTests()
    {
        EditorApplication.wantsToQuit += OnWantsToQuit;

        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        var done = new ManualResetEventSlim(false);
        var cb = new RunCallback(done);
        api.RegisterCallbacks(cb);

        var filter = new Filter()
        {
            testMode = TestMode.EditMode
        };

        api.Execute(new ExecutionSettings(filter));

        // Wait for completion (reasonable timeout to avoid hanging indefinitely)
        var completed = done.Wait(TimeSpan.FromMinutes(10));
        int exitCode = 2; // default unknown error
        if (!completed)
        {
            UnityEngine.Debug.LogError("AutomatedTestRunner: Timeout waiting for tests to finish.");
            exitCode = 2;
        }
        else
        {
            exitCode = cb.FailedCount == 0 ? 0 : 1;
        }

        api.UnregisterCallbacks(cb);
        // Ensure all logs are flushed
        Debug.Log($"AutomatedTestRunner: Tests finished. Exit code: {exitCode}");

        // Quit the editor with the appropriate exit code
        EditorApplication.Exit(exitCode);
    }

    private static bool OnWantsToQuit()
    {
        // Allow quit
        return true;
    }
}
