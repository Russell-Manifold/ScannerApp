using Data.DataAccess;
using System;
using System.IO;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace GoodsRecieveingApp
{
    public partial class App : Application
    {
        static AccessLayer database;
        public App()
        {
            InitializeComponent();

            MainPage = new MainPage();
        }
        public static AccessLayer Database
        {
            get
            {
                if (database == null)
                {
                    database = new AccessLayer(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SqliteDBGoodsRec.db3"));
                }
                return database;
            }
        }
        protected override void OnStart()
        {
            // Handle when your app starts
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
