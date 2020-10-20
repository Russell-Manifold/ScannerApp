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
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                await FetchWH();
            }
            else
            {
                Vibration.Vibrate();
                message.DisplayMessage("No Internet Connection", true);
            }
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
                swDelSOLines.IsToggled = config.DeleteSOLines;
                swDelGRVLines.IsToggled = config.DeletePOLines;
                try
                { txtReceiveUser.Text = config.ReceiveUser.ToString(); }
                catch { txtReceiveUser.Text = "0"; }

               try  {txtInvoiceUser.Text = config.InvoiceUser.ToString(); }
               catch{ txtInvoiceUser.Text = "0"; }

               try
                {txtWHTrfUser.Text = config.WhTrfUser.ToString();}
                catch { txtWHTrfUser.Text = "0"; }
                               
                try
                {
                    txfAPI.Text = config.DefaultAPI;
                }
                catch
                {

                }

            }
        }
        private async void btnSave_Clicked(object sender, EventArgs e)
        {
            try
            {
                config.DefaultAccWH = txfAccWH.SelectedItem.ToString();
                config.DefaultRejWH = txfRejWH.SelectedItem.ToString();
            }
            catch
            {

            }
            config.GRVActive = swGRV.IsToggled;
            config.RepackActive = swRepack.IsToggled;
            config.WhseTrfActive = swWTRF.IsToggled;
            config.CountActive = swInvCnt.IsToggled;
            config.InvoiceActive = swInvoice.IsToggled;
            config.DefaultAPI = txfAPI.Text;
            config.DeleteSOLines = swDelSOLines.IsToggled;
            config.DeletePOLines = swDelGRVLines.IsToggled;
            if (txtReceiveUser.Text.ToString().Length < 1)
            {
                config.ReceiveUser = "0";
            }
            else {
                config.ReceiveUser = txtReceiveUser.Text.ToString();
            }

            if (txtInvoiceUser.Text.ToString().Length < 1)
            {
                config.InvoiceUser = "0";
            }
            else
            {
                config.InvoiceUser = txtInvoiceUser.Text.ToString();
            }

            if (txtWHTrfUser.Text.ToString().Length < 1)
            {
                config.WhTrfUser = "0";
            }
            else
            {
                config.WhTrfUser = txtWHTrfUser.Text.ToString();
            }


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

        private void swInvoice_Toggled(object sender, ToggledEventArgs e)
        {
            if (swInvoice.IsToggled)
            {
                swDelSOLines.IsEnabled = true;
            }
            else
            {
                swDelSOLines.IsToggled = false;
                swDelSOLines.IsEnabled = false;
            }
        }

        private void swDelGRVLines_Toggled(object sender, ToggledEventArgs e)
        {
            if (swGRV.IsToggled)
            {
                swDelGRVLines.IsEnabled = true;
            }
            else
            {
                swDelGRVLines.IsToggled = false;
                swDelGRVLines.IsEnabled = false;
            }
        }
    }
}