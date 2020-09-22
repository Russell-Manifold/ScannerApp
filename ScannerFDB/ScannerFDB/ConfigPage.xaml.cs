using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoodsRecieveingApp;
using Data.Model;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Data.Message;
using System.Threading;
using System.Data;
using Xamarin.Essentials;

namespace ScannerFDB
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ConfigPage : ContentPage
    {
        bool isNew = false;
        List<string> WHIDs = new List<string>();
        DeviceConfig config = new DeviceConfig();
        IMessage message = DependencyService.Get<IMessage>();
        public ConfigPage()
        {
            InitializeComponent();
        }
        protected async override void OnAppearing()
        {
            base.OnAppearing();
            await FetchWH();
            try
            {
                config = await GoodsRecieveingApp.App.Database.GetConfig();
            }
            catch(Exception es)
            {

            }          
            if (config == null)
            {
                isNew = true;
                config = new DeviceConfig();
            }
            else
            {
                txfAccWH.SelectedItem = config.DefaultAccWH;
                txfRejWH.SelectedItem = config.DefaultRejWH;
                swGRV.IsToggled = config.GRVActive;
                swRepack.IsToggled = config.RepackActive;
                swWTRF.IsToggled = config.WhseTrfActive;
                swInvCnt.IsToggled = config.CountActive;
                swInvoice.IsToggled = config.InvoiceActive;
            }           
        }
        private async void btnSave_Clicked(object sender, EventArgs e)
        {
            config.DefaultAccWH = txfAccWH.SelectedItem.ToString();
            config.DefaultRejWH = txfRejWH.SelectedItem.ToString();
            config.GRVActive = swGRV.IsToggled;
            config.RepackActive = swRepack.IsToggled;
            config.WhseTrfActive = swWTRF.IsToggled;
            config.CountActive = swInvCnt.IsToggled;
            config.InvoiceActive = swInvoice.IsToggled;
            if (isNew)
            {
                await GoodsRecieveingApp.App.Database.Insert(config);
            }
            else
            {
                await GoodsRecieveingApp.App.Database.Update(config);
            }
            message.DisplayMessage("Saved!", true);
            await Navigation.PopAsync();
        }

        private async Task<bool> FetchWH()
        {
            RestSharp.RestClient client = new RestSharp.RestClient();
            string path = "GetWarehouses";
            client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
            {
                string str = $"GET?";
                var Request = new RestSharp.RestRequest();
                Request.Resource = str;
                Request.Method = RestSharp.Method.GET;
                var cancellationTokenSource = new CancellationTokenSource();
                var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                if (res.Content.ToString().Contains("WHID"))
                {
                    DataSet myds = new DataSet();
                    myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                    foreach (DataRow row in myds.Tables[0].Rows)
                    {
                        WHIDs.Add(row["WHID"].ToString());                        
                    }
                    txfAccWH.ItemsSource = WHIDs;
                    txfRejWH.ItemsSource = WHIDs;
                    return true;
                }
            }
            return false;
        }
     
        private void txfAccWH_SelectedIndexChanged(object sender, EventArgs e)
        {        
            if (txfAccWH.SelectedIndex!=-1&&txfRejWH.SelectedIndex!=-1)
            {
                if (txfAccWH.SelectedItem.ToString()==txfRejWH.SelectedItem.ToString())
                {
                    message.DisplayMessage("You cannot have the same warehouse for both",true);
                    Vibration.Vibrate();
                    txfAccWH.SelectedIndex = -1;
                }
            }
        }
        private void txfRejWH_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (txfRejWH.SelectedIndex != -1 && txfAccWH.SelectedIndex != -1)
            {
                if (txfRejWH.SelectedItem.ToString() == txfAccWH.SelectedItem.ToString())
                {
                    message.DisplayMessage("You cannot have the same warehouse for both", true);
                    Vibration.Vibrate();
                    txfRejWH.SelectedIndex = -1;
                }
            }
        }
    }
}