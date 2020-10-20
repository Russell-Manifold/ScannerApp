using Data.KeyboardContol;
using Data.Message;
using Data.Model;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace RepackagingMoblie
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Custom : ContentPage
    {
        private ExtendedEntry _currententry;
        private string ItemBarcode, ItemCode;
        IMessage message = DependencyService.Get<IMessage>();
        public Custom()
        {
            InitializeComponent();
            txfBarcode.Focused += Entry_Focused;
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            txfBarcode.Focus();
        }
        private async void TxfBarcode_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txfBarcode.Text != "")
            {
                Loader.IsVisible = true;
                try
                {
                    BOMItem bi = await GoodsRecieveingApp.App.Database.GetBOMItem(txfBarcode.Text);
                    Loader.IsVisible = false;
                    Vibration.Vibrate();
                    message.DisplayMessage("You can only add single items", true);
                }
                catch
                {
                    try
                    {
                        if (Connectivity.NetworkAccess == NetworkAccess.Internet)
                        {
                            RestSharp.RestClient client = new RestSharp.RestClient();
                            string path = "FindDescAndCode";
                            client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                            {
                                string str = $"GET?qrystr=ACCPRD|4|" + txfBarcode.Text;
                                var Request = new RestSharp.RestRequest();
                                Request.Resource = str;
                                Request.Method = RestSharp.Method.GET;
                                var cancellationTokenSource = new CancellationTokenSource();
                                var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                                if (res.IsSuccessful && res.Content.Split('|')[0].Contains("0"))
                                {
                                    if (res.Content.Split('|')[2] == MainPage.docLines.Find(x => x.ItemDesc == "1ItemFromMain").ItemCode)
                                    {
                                        lblItemDesc.Text = res.Content.Split('|')[3];
                                        ItemBarcode = res.Content.Split('|')[4];
                                        ItemCode = res.Content.Split('|')[2];
                                        Loader.IsVisible = false;
                                    }
                                    else
                                    {
                                        Loader.IsVisible = false;
                                        Vibration.Vibrate();
                                        message.DisplayMessage("This is not the same product", true);
                                        txfBarcode.Text = "";
                                        txfBarcode.Focus();
                                        return;
                                    }
                                }
                            }
                        }
                        else
                        {
                            Vibration.Vibrate();
                            message.DisplayMessage("No Internet Connection", true);
                        }
                    }
                    catch
                    {
                        lblItemDesc.Text = "No Item With This Code";
                        Loader.IsVisible = false;
                        Vibration.Vibrate();
                        message.DisplayMessage("We could not find this item code", true);
                        txfBarcode.Text = "";
                        txfBarcode.Focus();
                        return;
                    }
                }
                txfQTY.Focus();
            }
        }
        private async void BtnAdd_Clicked(object sender, EventArgs e)
        {
            if (lblItemDesc.Text != null && lblItemDesc.Text != "" && lblItemDesc.Text != "No Item With This Code" && txfQTY.Text != null && txfQTY.Text != "" && Convert.ToInt32(txfQTY.Text) >= 1 && Convert.ToInt32(txfQTY.Text) < 100)
            {
                if (MainPage.docLines.Where(x => x.ItemDesc == "1ItemFromMain").Select(x => x.ItemQty).FirstOrDefault() >= Convert.ToInt32(txfQTY.Text) + MainPage.docLines.Where(x => x.ItemDesc != "1ItemFromMain").Sum(x => x.ItemQty))
                {
                    if (!await CheckExists(txfQTY.Text))
                    {
                        string code = "";
                        if (Convert.ToInt32(txfQTY.Text) > 9)
                        {
                            code = "F" + txfQTY.Text + ItemBarcode.Substring(ItemBarcode.Length - 6, 5);
                        }
                        else
                        {
                            code = "F0" + txfQTY.Text + ItemBarcode.Substring(ItemBarcode.Length - 6, 5);
                        }
                        if (Connectivity.NetworkAccess == NetworkAccess.Internet)
                        {
                            await linkCodesInPastel(ItemCode, code);
                        }
                        else
                        {
                            Vibration.Vibrate();
                            message.DisplayMessage("No Internet Connection", true);
                        }
                        MainPage.PackCodes.Add(code);
                    }
                    MainPage.docLines.Add(new DocLine { ItemBarcode = txfBarcode.Text, ItemDesc = lblItemDesc.Text, isRejected = false, ItemQty = Convert.ToInt32(txfQTY.Text) });

                    message.DisplayMessage("Complete!", true);
                    await Navigation.PopAsync();
                }
                else
                {
                    Vibration.Vibrate();
                    message.DisplayMessage("Too many items have been scanned", true);
                }
            }
            else
            {
                Vibration.Vibrate();
                message.DisplayMessage("Make sure all fields are filled in", true);
            }
        }
        private async Task<string> linkCodesInPastel(string itemCode, string barcode)
        {
            try
            {
                string BOMHead = "" + barcode + "|BOM HEADER|12.1|12.2|12.3|12.4|12.5|12.6|12.7|12.8|12.9|12|10|20|30|100|200|300|" + itemCode + "|Y|Y|N||N|N|" + MainPage.MainWH;
                string line = "" + barcode + "|" + itemCode + "|" + txfQTY.Text.Trim() + "|" + MainPage.MainWH + "#";
                RestSharp.RestClient client = new RestSharp.RestClient();
                string path = "POSTBOM";
                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                {
                    string str = $"POST?BOMHead={BOMHead}&line={line}";
                    var Request = new RestSharp.RestRequest();
                    Request.Resource = str;
                    Request.Method = RestSharp.Method.POST;
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = client.Execute(Request);
                    if (res.StatusCode.ToString().Contains("OK"))
                    {
                        return res.Content.Substring(1, res.Content.Length - 2);
                    }
                }
            }
            catch (Exception)
            {

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
        async Task<bool> CheckExists(string qty)
        {
            RestClient client = new RestClient();
            string path = "CheckBOMExists";
            client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
            {
                string str = $"GET?qrystr=ACCBOML|2|{MainPage.docLines.Where(x => x.ItemDesc == "1ItemFromMain").FirstOrDefault().ItemCode}|{qty}";
                var Request = new RestSharp.RestRequest(str, Method.GET);
                CancellationTokenSource ct = new CancellationTokenSource();
                var res = await client.ExecuteAsync(Request, ct.Token);
                if (res.IsSuccessful)
                {
                    string returnVal = res.Content.Substring(1, res.Content.Length - 2);
                    if (returnVal.Split('|')[0] == "0")
                    {
                        if (returnVal.Split('|')[1].Length > 3)
                        {
                            MainPage.PackCodes.Add(returnVal.Split('|')[1]);
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        private void btnHome_Clicked(object sender, EventArgs e)
        {
            Navigation.PopAsync();
        }
    }
}