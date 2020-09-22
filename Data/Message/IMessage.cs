using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Message
{
    public interface IMessage
    {
        void DisplayMessage(string msg, bool isLong);
    }
}
