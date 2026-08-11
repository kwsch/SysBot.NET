using System;
using System.Media;
using System.Windows.Forms;

namespace SysBot.Pokemon.WinForms;

internal static class TaskDialogUtil
{
    /// <param name="owner">The window that owns the dialog.</param>
    extension(IWin32Window owner)
    {
        /// <summary>
        /// Displays a dialog showing the details of an error.
        /// </summary>
        /// <param name="lines">User-friendly message about the error.</param>
        /// <returns>The <see cref="DialogResult"/> associated with the dialog.</returns>
        public DialogResult Error(params ReadOnlySpan<string> lines)
        {
            SystemSounds.Hand.Play();

            var msg = string.Join(Environment.NewLine + Environment.NewLine, lines);
            var page = new TaskDialogPage
            {
                Caption = "Error",
                Text = msg,
                Icon = TaskDialogIcon.Error,
                Buttons = [TaskDialogButton.OK],
                AllowCancel = true, // Allows Esc key to close
                SizeToContent = true
            };

            var button = TaskDialog.ShowDialog(owner, page);
            return ToDialogResult(button);
        }

        public DialogResult Alert(params ReadOnlySpan<string> lines)
            => owner.Alert(true, lines);

        public DialogResult Alert(bool sound, params ReadOnlySpan<string> lines)
        {
            if (sound)
                SystemSounds.Asterisk.Play();

            var msg = string.Join(Environment.NewLine + Environment.NewLine, lines);
            var page = new TaskDialogPage
            {
                Caption = "Alert",
                Text = msg,
                Icon = sound ? TaskDialogIcon.Information : TaskDialogIcon.None,
                Buttons = [TaskDialogButton.OK],
                AllowCancel = true, // Allows Esc key to close
                SizeToContent = true
            };

            var button = TaskDialog.ShowDialog(owner, page);
            return ToDialogResult(button);
        }

        public DialogResult Prompt(MessageBoxButtons btn, params ReadOnlySpan<string> lines)
        {
            SystemSounds.Asterisk.Play();

            var msg = string.Join(Environment.NewLine + Environment.NewLine, lines);
            var page = new TaskDialogPage
            {
                Caption = "Prompt",
                Text = msg,
                Icon = TaskDialogIcon.Information,
                Buttons = GetTaskDialogButtons(btn),
                AllowCancel = true, // Allows Esc key to close
                SizeToContent = true
            };

            var button = TaskDialog.ShowDialog(owner, page);
            return ToDialogResult(button);
        }
    }

    private static TaskDialogButtonCollection GetTaskDialogButtons(MessageBoxButtons buttons) => buttons switch
    {
        MessageBoxButtons.OK => [TaskDialogButton.OK],
        MessageBoxButtons.OKCancel => [TaskDialogButton.OK, TaskDialogButton.Cancel],
        MessageBoxButtons.AbortRetryIgnore => [TaskDialogButton.Abort, TaskDialogButton.Retry, TaskDialogButton.Ignore],
        MessageBoxButtons.YesNo => [TaskDialogButton.Yes, TaskDialogButton.No],
        MessageBoxButtons.YesNoCancel => [TaskDialogButton.Yes, TaskDialogButton.No, TaskDialogButton.Cancel],
        MessageBoxButtons.RetryCancel => [TaskDialogButton.Retry, TaskDialogButton.Cancel],

        _ => throw new ArgumentOutOfRangeException(nameof(buttons), buttons, null),
    };

    private static DialogResult ToDialogResult(TaskDialogButton button)
    {
        if (button == TaskDialogButton.OK)
            return DialogResult.OK;
        if (button == TaskDialogButton.Cancel)
            return DialogResult.Cancel;
        if (button == TaskDialogButton.Abort)
            return DialogResult.Abort;
        if (button == TaskDialogButton.Retry)
            return DialogResult.Retry;
        if (button == TaskDialogButton.Ignore)
            return DialogResult.Ignore;
        if (button == TaskDialogButton.Yes)
            return DialogResult.Yes;
        if (button == TaskDialogButton.No)
            return DialogResult.No;
        return DialogResult.None;
    }
}
