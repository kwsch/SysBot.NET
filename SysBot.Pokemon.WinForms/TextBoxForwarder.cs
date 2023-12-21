using System;
using System.Windows.Forms;
using SysBot.Base;

namespace SysBot.Pokemon.WinForms;

/// <summary>
/// Forward logs to a TextBox.
/// </summary>
public sealed class TextBoxForwarder(TextBoxBase Box) : ILogForwarder
{
    /// <summary>
    /// Synchronize access to the TextBox. Only the GUI thread should be writing to it.
    /// </summary>
    private readonly object _logLock = new();

    public void Forward(string message, string identity)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] - {identity}: {message}{Environment.NewLine}";

        lock (_logLock)
        {
            if (Box.InvokeRequired)
                Box.BeginInvoke((MethodInvoker)(() => UpdateLog(line)));
            else
                UpdateLog(line);
        }
    }

    private void UpdateLog(string line)
    {
        // ghetto truncate
        var text = Box.Text;
        var max = Box.MaxLength;
        if (text.Length + line.Length + 2 >= max)
            Box.Text = text[(max / 4)..];

        Box.AppendText(line);
        Box.ScrollToCaret();
    }
}
