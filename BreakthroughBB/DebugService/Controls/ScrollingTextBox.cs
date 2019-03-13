using System.Windows.Controls;

namespace DebugService.Controls
{
    /// <summary>
    /// Text box that automatically scroll down after text appending
    /// </summary>
    public class ScrollingTextBox : TextBox
    {
        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);
            CaretIndex = Text.Length;
            ScrollToEnd();
        }
    }
}
