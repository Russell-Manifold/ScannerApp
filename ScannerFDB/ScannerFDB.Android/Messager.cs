using Xamarin.Forms;
using Data.Message;
using Android.Widget;
using ScannerFDB.Droid;

[assembly: Dependency(typeof(Messager))]
namespace ScannerFDB.Droid
{   
    public class Messager : IMessage
    {
        public void DisplayMessage(string msg, bool isLong)
        {
            if (isLong)
            {
                Toast.MakeText(Android.App.Application.Context, msg, ToastLength.Long).Show();
            }
            else
            {
                Toast.MakeText(Android.App.Application.Context, msg, ToastLength.Long).Show();
            }
        }
    }
}