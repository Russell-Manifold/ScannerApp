using Data.KeyboardContol;
using Data.Message;
using Data.Model;
using GoodsRecieveingApp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace PickAndPack
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent(); 
        }

        private async void btnSingleOrder_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SingleOrder());
        }

        private async void btnSinglePallet_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SinglePallet());
        }

        private async void btnPickingSlips_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PickSlips());
        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();
            DeviceConfig d = await GoodsRecieveingApp.App.Database.GetConfig();
            if (d == null || d.DefaultAccWH == null || d.DefaultRejWH == null)
            {
                Vibration.Vibrate();
                await DisplayAlert("Error!", "Please select options in device config in device configuration", "OK");
                await Navigation.PopAsync();
                return;
            }
            //if (!d.PaperPickSlips)
            //{
            //    btnPickingSlips.IsVisible = false;
            //    btnHOPickingSlips.IsVisible = false;
            //}
            if (GoodsRecieveingApp.MainPage.PSCollect == true&&!d.PaperPickSlips)
            {
                btnPickingSlips.IsVisible = true;
                btnHOPickingSlips.IsVisible = true;
            }
            else {
                btnPickingSlips.IsVisible = false;
                btnHOPickingSlips.IsVisible = true;
            }
        }

        private async void btnHOPickingSlips_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new HandOverPage());
        }

        private async void btnReleaseOrd_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AuthOut());
        }
    }
}