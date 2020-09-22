using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace InventoryCount
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new InvLandingPage();
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
