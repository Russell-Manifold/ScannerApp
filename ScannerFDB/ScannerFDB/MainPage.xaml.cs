using Data.KeyboardContol;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace ScannerFDB
{    
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
       
        public MainPage()
        {
            InitializeComponent();            
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            this.Title = "Hi "+GoodsRecieveingApp.MainPage.UserName;
            
            if (GoodsRecieveingApp.MainPage.fReceive == true)
            {btnReceive.IsVisible = true;}
            else{btnReceive.IsVisible = false;}

            if (GoodsRecieveingApp.MainPage.fRepack == true)
            { btnRepack.IsVisible = true; }
            else { btnRepack.IsVisible = false; }

            if (GoodsRecieveingApp.MainPage.fWhTrf == true)
            { btnWhTrf.IsVisible = true; }
            else { btnWhTrf.IsVisible = false; }

            if (GoodsRecieveingApp.MainPage.fInvCount == true)
            { BtnInvCount.IsVisible = true; }
            else { BtnInvCount.IsVisible = false; }

            if (GoodsRecieveingApp.MainPage.fPickPack == true)
            { btnPickPack.IsVisible = true; }
            else { btnPickPack.IsVisible = false; }

            if (GoodsRecieveingApp.MainPage.SystAdmin == true)
            { btnAdmin.IsEnabled = true; }
            else { btnAdmin.IsEnabled = false; }
        }
        private void Button_Clicked_Goods_Receving(object sender, EventArgs e)
        {
            Navigation.PushAsync(new GoodsRecieveingApp.MainPage());
        }

        private async void Button_Clicked_Admin(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AdminPage());
        }

        private async void Button_Clicked_Repacking(object sender, EventArgs e)
        {
           await Navigation.PushAsync(new RepackagingMoblie.MainPage());
        }
        private async void Button_Clicked_WareHouseTransfer(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new WHTransfer.MainPage());
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PickAndPack.MainPage());
        }

        private async void BtnInvCount_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new InventoryCount.InvLandingPage());
        }
    }
}
