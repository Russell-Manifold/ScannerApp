using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Data.DataAccess;
using Data.Model;
using System.IO;
using System.Threading;
using Data.KeyboardContol;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;

namespace ScannerFDB
{
    public partial class App : Xamarin.Forms.Application
    {
        static AccessLayer database;
        public App()
        {
            InitializeComponent();
            LoginPage mp = new LoginPage();
            var nav = new NavigationPage(mp);
            nav.BarBackgroundColor = Color.Blue;
            nav.BarTextColor = Color.White;
            MainPage = nav;
        }
        public static AccessLayer Database
        {
            get
            {
                if (database == null)
                {
                    database = new AccessLayer(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SqliteDB.db3"));
                }
                return database;
            }
        }
        protected override void OnStart()
        {
            App.Current.On<Android>().UseWindowSoftInputModeAdjust(WindowSoftInputModeAdjust.Resize);
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
