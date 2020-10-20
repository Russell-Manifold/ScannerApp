using Data.KeyboardContol;
using Data.Message;
using Data.Model;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace InventoryCount
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AcceptScanPage : ContentPage
    {
        private ExtendedEntry _currententry;
        IMessage message = DependencyService.Get<IMessage>();
        InventoryItem i = new InventoryItem();
        int ActualQTY = 0;
        public AcceptScanPage(InventoryItem ite, int Actual)
        {
            InitializeComponent();
            txfUserCode.Focused += Entry_Focused;
            ActualQTY = Actual;
            i = ite;
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            lblInfo.Text = "Approve QTY for:\n" + i.ItemDesc + "\nFirst Count: " + i.FirstScanQty + "\nSecond Count: " + i.SecondScanQty + "\n";
            txfUserCode.Focus();
        }
        private async void txfUserCode_Completed(object sender, EventArgs e)
        {
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                LoadingIndicator.IsVisible = true;
                //xfUserCode.Text =GoodsRecieveingApp.MainPage.CalculateCheckDigit(txfUserCode.Text);
                try
                {
                    RestClient client = new RestClient();
                    string path = "GetUser";
                    //client.BaseUrl = new Uri("https://manifoldsa.co.za/FDBAPI/api/" + path);
                    client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                    {
                        string str = $"GET?UserName={txfUserCode.Text}";
                        var Request = new RestRequest(str, Method.GET);
                        var cancellationTokenSource = new CancellationTokenSource();
                        var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                        if (res.IsSuccessful && res.Content.Contains("UserName"))
                        {
                            bool inv = false;
                            int id = 0;
                            DataSet myds = new DataSet();
                            myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                            foreach (DataRow row in myds.Tables[0].Rows)
                            {
                                inv = Convert.ToBoolean(row["InvReCountAUTH"]);
                                id = Convert.ToInt32(row["Id"].ToString());
                            }
                            if (inv)
                            {
                                CountPage.items.Where(x => x.ItemCode == i.ItemCode).Select(x => { x.SecondScanAuth = id; x.Complete = true; return x; });
                                if (await SendToPastel())
                                {
                                    await Navigation.PopAsync();
                                }
                                else
                                {
                                    await DisplayAlert("Error could not adjust in system", "Please try again!", "OK");
                                }

                            }
                        }
                        else
                        {
                            LoadingIndicator.IsVisible = false;
                            message.DisplayMessage("Invalid user!", true);
                            Vibration.Vibrate();
                            txfUserCode.Text = "";
                            txfUserCode.Focus();
                            return;
                        }
                    }
                }
                catch
                {
                    LoadingIndicator.IsVisible = false;
                    message.DisplayMessage("Invalid user!", true);
                    Vibration.Vibrate();
                    txfUserCode.Text = "";
                    txfUserCode.Focus();
                    return;
                }
            }
            else
            {
                Vibration.Vibrate();
                message.DisplayMessage("No Internet Connection", true);
            }
        }
        private async Task<bool> SendToPastel()
        {
            string Store = InvLandingPage.WH;
            string JobCode = "Inventory count ADJ";
            string Ref = "Inventory count Stock ADJ";
            string JnlAcc = await GetGlCode(i.ItemCode, Store);
            int QTY = Convert.ToInt32(i.SecondScanQty, CultureInfo.InvariantCulture.NumberFormat) - ActualQTY;
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                RestClient client = new RestClient();
                string path = "InvStockADJ";
                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                {
                    string str = $"POST?itemCode={i.ItemCode}&JnlAcc={JnlAcc}&JnlDate={DateTime.Now.ToString("dd MMM yyyy")}&JobCode={JobCode}&Desc={i.ItemDesc}&Ref={Ref}&Qty={QTY}&Store={Store}";
                    var Request = new RestRequest(str, Method.POST);
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    if (res.IsSuccessful && res.Content.Contains("Complete"))
                    {
                        return true;
                    }
                    return false;
                }
            }
            return false;
        }
        async Task<string> GetGlCode(string itemCode, string WHCode)
        {
            RestClient client = new RestClient();
            string path = "GetField";
            client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
            {
                string str = $"GET?qrystr=ACCSTKST|0|{WHCode}{itemCode}|2";
                var Request = new RestRequest(str, Method.GET);
                var cancellationTokenSource = new CancellationTokenSource();
                var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                if (res.IsSuccessful && res.Content.Split('|')[0].Contains("0"))
                {
                    str = $"GET?qrystr=ACCGRP|0|{res.Content.Replace('"', ' ').Replace('\\', ' ').Trim().Split('|')[1]}|8";
                    Request = new RestRequest(str, Method.GET);
                    cancellationTokenSource = new CancellationTokenSource();
                    res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    if (res.IsSuccessful && res.Content.Contains("0"))
                    {
                        return res.Content.Replace('"', ' ').Replace('\\', ' ').Trim().Split('|')[1];
                    }
                }
            }
            return "";
        }
        private async void Entry_Focused(object sender, FocusEventArgs e)
        {
            await Task.Delay(110);
            _currententry = sender as ExtendedEntry;
            if (_currententry != null)
            {
                try
                {
                    _currententry.HideKeyboard();
                }
                catch
                {

                }

            }
        }
    }
}