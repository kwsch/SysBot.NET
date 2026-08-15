using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SysBot.Pokemon.WinForms;

internal static class WinFormsUtil
{
    /// <summary>
    /// Gets the selected value of the input <see cref="cb"/>. If no value is selected, will return 0.
    /// </summary>
    /// <param name="cb">ComboBox to retrieve value for.</param>
    internal static int GetIndex(ListControl cb)
    {
        return (int)(cb.SelectedValue ?? 0);
    }

    public static IEnumerable<T> GetChildrenOfType<T>(this Control control) where T : class
    {
        foreach (var child in control.Controls.OfType<Control>())
        {
            if (child is T childOfT)
                yield return childOfT;

            if (!child.HasChildren) continue;
            foreach (var descendant in child.GetChildrenOfType<T>())
                yield return descendant;
        }
    }

    public static void ReformatDark(Control z)
    {
        if (z is TabControl tc)
        {
            foreach (TabPage tab in tc.TabPages)
                tab.UseVisualStyleBackColor = false;
        }
        else if (z is DataGridView dg)
        {
            dg.EnableHeadersVisualStyles = false;
            dg.BorderStyle = BorderStyle.None;
        }
        else if (z is ComboBox cb)
        {
            cb.FlatStyle = FlatStyle.Popup;
        }
        else if (z is ListBox lb)
        {
            lb.BorderStyle = BorderStyle.None;
        }
        else if (z is RichTextBox rtb)
        {
            rtb.BorderStyle = BorderStyle.None;
        }
        else if (z is TextBoxBase tb)
        {
            tb.BorderStyle = BorderStyle.FixedSingle;
        }
        else if (z is NumericUpDown nud)
        {
            nud.BorderStyle = BorderStyle.FixedSingle;
        }
        else if (z is GroupBox gb)
        {
            gb.FlatStyle = FlatStyle.Popup;
        }
        else if (z is ButtonBase b)
        {
            b.FlatStyle = FlatStyle.Popup;
        }
    }
}
