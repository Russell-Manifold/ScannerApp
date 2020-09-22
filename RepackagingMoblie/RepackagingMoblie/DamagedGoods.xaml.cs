using Data.KeyboardContol;
using Data.Message;
using Data.Model;
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
    public partial class DamagedGoods : ContentPage
    {
        private ExtendedEntry _currententry;
        IMessage message = DependencyService.Get<IMessage>();
        public DamagedGoods()
        {
            InitializeComponent();
            txfBarcode.Focused += Entry_Focused;
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            txfBarcode.Focus();
        }
        private async void BtnComplete_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
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
        private async void TxfBarcode_TextChanged(object sender, TextChangedEventArgs e)
        {
            await Task.Delay(200);
            if (txfBarcode.Text != "")
            {
                Loader.IsVisible = true;
                await ItemCheck();
                txfBarcode.Text = "";
                txfBarcode.Focus();
            }
        }
         void setQTY(string barcode)
        {
            int i = MainPage.docLines.FindAll(x => x.isRejected == true && x.ItemBarcode == barcode).Count;
            lblItemQTY.Text="You have scanned "+i+" items with this barcode to be rejected";
        }
        public async Task<bool> ItemCheck()
        {
            lblBarcode.Text = txfBarcode.Text;
            try
            {
                BOMItem bi = await GoodsRecieveingApp.App.Database.GetBOMItem(txfBarcode.Text);
                Loader.IsVisible = false;
                Vibration.Vibrate();
                message.DisplayMessage("You can't add a pack as a single item", true);
            }
            catch
            {
                try
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
                                if (MainPage.docLines.Where(x => x.ItemDesc == "1ItemFromMain").Select(x => x.ItemQty).FirstOrDefault() >= 1 + MainPage.docLines.Where(x => x.ItemDesc != "1ItemFromMain").Sum(x => x.ItemQty))
                                {
                                    lblItemDesc.Text = res.Content.Split('|')[3];
                                    MainPage.docLines.Add(new DocLine { ItemBarcode = txfBarcode.Text, ItemDesc = lblItemDesc.Text, isRejected = true,WarehouseID=(await GoodsRecieveingApp.App.Database.GetConfig()).DefaultRejWH, ItemQty = 1 });
                                    setQTY(txfBarcode.Text);
                                    Loader.IsVisible = false;
                                    return true;
                                }
                                else
                                {
                                    Vibration.Vibrate();
                                    message.DisplayMessage("Too many items have been scanned", true);
                                    Loader.IsVisible = false;
                                    return false;
                                } 
                            }
                            else
                            {
                                Loader.IsVisible = false;
                                Vibration.Vibrate();
                                message.DisplayMessage("This is not the right product", true);
                                lblItemDesc.Text = "";
                                lblItemQTY.Text = "";
                                return false;
                            }
                        }
                    }
                }
                catch
                {
                    lblItemDesc.Text = "No Item With This Code";
                    Loader.IsVisible = false;
                    Vibration.Vibrate();
                    message.DisplayMessage("This item code could not be found", true);
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
