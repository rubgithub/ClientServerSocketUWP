using System;
using System.Diagnostics;
using Windows.ApplicationModel.Background;

namespace SocketClientUWP
{
    public static class BgTaskConfig
    {
        public const string TaskName = "ServerTask";
        public const string TaskEntryPoint = "BgSocketServer.ServerTask";
        public static string TaskRegStatus = "";
        public static string ServerReturnMessage = "";
        public static string ServerStatus = "";
        public static string ServerReceivedMessage = "";
        public static bool BgTaskRegistered;

        public static BackgroundTaskRegistration RegisterBackgroundTask(string taskEntryPoint, string name,
                IBackgroundTrigger trigger)
        {
            var builder = new BackgroundTaskBuilder
            {
                Name = name,
                TaskEntryPoint = taskEntryPoint
            };
            builder.SetTrigger(trigger);

            try
            {
                var task = builder.Register();
                TaskRegStatus = "Registered";
                BgTaskRegistered = true;
                return task;
            }
            catch (Exception e)
            {
                TaskRegStatus = "Fail to register, trying again...";
                Debug.WriteLine($"Fail to register {e.Message}");
                return null;
            }
        }

        //Unregister background tasks with specified name.
        public static void UnregisterBackgroundTasks(string name)
        {
            // Loop through all background tasks and unregister any with SampleBackgroundTaskName or
            // SampleBackgroundTaskWithConditionName.
            foreach (var cur in BackgroundTaskRegistration.AllTasks)
            {
                if (cur.Value.Name != name) continue;
                cur.Value.Unregister(true);
                Debug.WriteLine($"** Unregister: {name} **");
            }
            TaskRegStatus = "Unregistered";
        }
    }
}
