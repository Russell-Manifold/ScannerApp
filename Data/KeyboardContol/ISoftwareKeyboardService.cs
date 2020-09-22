using System;
using System.Collections.Generic;
using System.Text;

namespace Data.KeyboardContol
{
    public interface ISoftwareKeyboardService
    {
        event EventHandler<SoftwareKeyboardEventArgs> KeyboardHeightChanged;
        bool IsKeyboardVisible { get; }
    }
}
