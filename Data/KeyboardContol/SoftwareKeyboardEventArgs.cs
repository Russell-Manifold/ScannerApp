using System;
using System.Collections.Generic;
using System.Text;

namespace Data.KeyboardContol
{
    public class SoftwareKeyboardEventArgs : EventArgs
    {
        public SoftwareKeyboardEventArgs(int keyboardheight)
        {
            KeyboardHeight = keyboardheight;
        }

        public int KeyboardHeight { get; private set; }
    }
}
