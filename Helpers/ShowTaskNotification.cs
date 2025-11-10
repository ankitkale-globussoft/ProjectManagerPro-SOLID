using Microsoft.Toolkit.Uwp.Notifications;
using ProjectManagerPro_SOLID.Models;
using Windows.UI.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ProjectManagerPro_SOLID.Helpers
{
    public class ShowTaskNotification
    {
        public static void Show(TaskItem task)
    {
        if (task == null) return;

        try
        {
            // Build toast content with max 3 text lines (4 is invalid)
            var content = new ToastContentBuilder()
                .AddText("🎯 New Task Assigned!")
                .AddText($"{task.Title} (Priority: {task.Priority})")
                .AddAttributionText($"Due: {task.DueDate:dd MMM yyyy}")
                .GetToastContent();

            var toast = new ToastNotification(content.GetXml());

            ToastNotificationManagerCompat.CreateToastNotifier().Show(toast);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Toast show failed: {ex}");
        }
    }

    }
}
