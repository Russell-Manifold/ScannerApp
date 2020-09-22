using Data.KeyboardContol;
using Data.Message;
using Data.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Globalization;

namespace WHTransfer
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class OutItems : ContentPage
    {
        private ExtendedEntry _currententry;
        IMessage message = DependencyService.Get<IMessage>();
        public static List<IBTItem> items=new List<IBTItem>();
        public OutItems()
        {
            InitializeComponent();
            items = new List<IBTItem>();
            txfScannedItem.Focused += Entry_Focused;
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();           
            txfScannedItem.Focus();
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
        private async Task<bool> CheckItem()
        {
            Loading.IsVisible = true;
            try
            {
                BOMItem bi = await GoodsRecieveingApp.App.Database.GetBOMItem(txfScannedItem.Text);
                if (!await CheckQTY(bi.ItemCode, bi.Qty))
                    return false;
                items.Add(new IBTItem {ScanBarcode=txfScannedItem.Text,ItemBarcode=bi.PackBarcode, ItemDesc=bi.ItemDesc,ItemCode=bi.ItemCode,ItemQtyOut=bi.Qty,ItemQtyIn=0,PickDateTime=DateTime.Now});
                return true;
            }
            catch
            {
                try
                {
                    RestSharp.RestClient client = new RestSharp.RestClient();
                    string path = "FindDescAndCode";
                    client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                    {
                        string qry = $"ACCPRD|4|{txfScannedItem.Text}";
                        string str = $"GET?qrystr={qry}";
                        var Request = new RestSharp.RestRequest(str,RestSharp.Method.GET);
                        var cancellationTokenSource = new CancellationTokenSource();
                        var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                        if (res.IsSuccessful && res.Content != null)
                        {
                            if (!await CheckQTY(res.Content.Split('|')[2],1))
                                return false;                              
                            items.Add(new IBTItem { ScanBarcode = txfScannedItem.Text, ItemBarcode = res.Content.Split('|')[4], ItemCode = res.Content.Split('|')[2], ItemDesc= res.Content.Split('|')[3], ItemQtyOut = 1, ItemQtyIn = 0,PickDateTime = DateTime.Now });
                            return true;
                        }
                    }
                }
                catch
                {
                    Loading.IsVisible = false;
                    message.DisplayMessage("There was a error in getting this item", true);
                    return false;
                }
            }
            Loading.IsVisible = false;
            message.DisplayMessage("Invalid Item", false);
            return false;
        }
        private async Task<bool> CheckQTY(string iCode,int qty)
        {
            RestSharp.RestClient client = new RestSharp.RestClient();
            string path = "ItemQOH";
            client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
            {
                string str = $"GET?WH={OutPage.FromWH}&ItemCode={iCode}";
                var Request = new RestSharp.RestRequest(str, RestSharp.Method.GET);
                var cancellationTokenSource = new CancellationTokenSource();
                var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                if (res.IsSuccessful && res.Content != null)
                {
                    if (res.Content=="0")
                    {
                        Loading.IsVisible = false;
                        message.DisplayMessage("There isnt enough stock in this warehouse", true);
                        return false;
                    }
                    else
                    {
                        int q = 0;

                        try
						{
                             q= Convert.ToInt32(Double.Parse(res.Content, CultureInfo.InvariantCulture.NumberFormat),CultureInfo.InvariantCulture.NumberFormat);
                        }
						catch(Exception ex)
						{

						}                        
                        if (q>=(qty+items.Where(x=>x.ItemCode==iCode).Sum(X=>X.ItemQtyOut)))
                        {
                            return true;
                        }
                        else
                        {
                            Loading.IsVisible = false;
                            message.DisplayMessage("There arent enough of this item in this warehouse",true);
                            return false;
                        }
                    }
                }
            }
            Loading.IsVisible = false;
            message.DisplayMessage("Please check your internet connection!", true);
            return false;
        }
        private void BtnComplete_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new AuthOut());
        }
        private async void ListViewItems_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            IBTItem ite = e.SelectedItem as IBTItem;
            string output = await DisplayActionSheet("Remove this item?", "YES", "NO");
            switch (output)
            {
                case "NO":
                    break;
                case "YES":
                    items.Remove(ite);
                    ListViewItems.ItemsSource = null;
                    ListViewItems.ItemsSource = items;
                    if (items.Count==0)
                    {
                        btnComplete.IsVisible = false;
                    }
                    txfScannedItem.Focus();
                    break;
            }
        }
        private async void txfScannedItem_Completed(object sender, EventArgs e)
        {          
            if (txfScannedItem.Text.Length > 1)
            {
               // txfScannedItem.Text = GoodsRecieveingApp.MainPage.CalculateCheckDigit(txfScannedItem.Text);
                if (await CheckItem())
                {
                    ListViewItems.ItemsSource = null;
                    await Task.Delay(100);
                    lblLastItem.Text = "Previous Barcode: " + txfScannedItem.Text;
                    ListViewItems.ItemsSource = items;
                    Loading.IsVisible = false;
                    btnComplete.IsVisible = true;
                    txfScannedItem.Text = "";
                    txfScannedItem.Focus();
                }
                else
                {
                    Vibration.Vibrate();
                    txfScannedItem.Text = "";
                    txfScannedItem.Focus();
                }
            }
        }
    }
}