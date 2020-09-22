using Data.KeyboardContol;
using Data.Message;
using Data.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace RepackagingMoblie
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        private ExtendedEntry _currententry;
        public static List<DocLine> docLines = new List<DocLine>();
        public static List<string> PackCodes = new List<string>();
        IMessage message = DependencyService.Get<IMessage>();
        public static string MainWH;
        public static string DMGWH;
        bool MustMakePack = false;
        public MainPage()
        {
            InitializeComponent();
            txfPackbarcode.Focused += Entry_Focused;
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
        protected async override void OnAppearing()
        {
            DeviceConfig dev =  await GoodsRecieveingApp.App.Database.GetConfig();
            if (dev == null || dev.DefaultRejWH == null || dev.DefaultAccWH == null)
            {
                Vibration.Vibrate();
                await DisplayAlert("Error!","Please make sure to set both warehouses in the configuration","OK");
                await Navigation.PopAsync();
            }
            dev.DefaultAccWH = MainWH;
            dev.DefaultRejWH = DMGWH;
            base.OnAppearing();
            txfPackbarcode.Text = "";
            lblQTY.Text = "";
            lblDesc.Text = "";
            MainLayout.IsVisible = false;
            OpenLayout.IsVisible = true;
        }
        private async void BtnGoToRepack_Clicked(object sender, EventArgs e)
        {
            if (docLines.Count>0)
            {
                await Navigation.PushAsync(new MenuPage(MustMakePack));
            }
            else
            {
                Vibration.Vibrate();
                message.DisplayMessage("Please make sure to scan a valid bardode", true);
            }

        }
        private async void TxfPackbarcode_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(txfPackbarcode.Text.Length>1){
                docLines.Clear();
                isLoading.IsVisible = true;
                if (MustMakePack)
                {
                    try
                    {
                        RestSharp.RestClient client = new RestSharp.RestClient();
                        string path = "FindDescAndCode";
                        client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                        {
                            string qry = $"ACCPRD|4|{txfPackbarcode.Text}";
                            string str = $"GET?qrystr={qry}";
                            var Request = new RestSharp.RestRequest(str, RestSharp.Method.GET);
                            var cancellationTokenSource = new CancellationTokenSource();
                            var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                            if (res.IsSuccessful && res.Content != null)
                            {
                                docLines.Add(new DocLine {ItemBarcode = txfPackbarcode.Text, ItemCode = res.Content.Split('|')[2], ItemDesc = res.Content.Split('|')[3],ItemQty=1});
                                lblDesc.Text = "Item: " + res.Content.Split('|')[3];
                                lblDesc.IsVisible = true;
                                btnGoToRepack.IsVisible = true;
                                return;
                            }
                        }
                    }
                    catch
                    {
                        Vibration.Vibrate();
                        btnGoToRepack.IsVisible = false;
                        message.DisplayMessage("No item code found", true);
                        txfPackbarcode.Text = "";
                        txfPackbarcode.Focus();
                    }
                }
                else
                {
                    try
                    {
                        BOMItem bi = await GoodsRecieveingApp.App.Database.GetBOMItem(txfPackbarcode.Text);
                        lblDesc.IsVisible = true;
                        lblQTY.IsVisible = false;
                        lblDesc.Text = "Item: " + bi.ItemDesc;
                        lblQTY.Text = "Qty: " + bi.Qty;
                        docLines.Add(new DocLine { ItemBarcode = bi.PackBarcode, ItemCode = bi.ItemCode, ItemDesc = "1ItemFromMain", ItemQty = bi.Qty });
                        btnGoToRepack.IsVisible = true;
                    }
                    catch
                    {
                        Vibration.Vibrate();
                        btnGoToRepack.IsVisible = false;
                        message.DisplayMessage("No pack code found", true);
                        txfPackbarcode.Text = "";
                        txfPackbarcode.Focus();
                    }
                }
                isLoading.IsVisible = false;
            }
        }
        private async void Button_Clicked_Home(object sender, EventArgs e)
        {
            await Navigation.PopAsync();  
        }
        private void btnUnpack_Clicked(object sender, EventArgs e)
        {
            lblQuestion.Text = "Scan PACKCODE of pack to be Un-packed:";
            imgUnpackMain.Source = "unpackicon.png";
            OpenLayout.IsVisible = false;
            MainLayout.IsVisible = true;
            MustMakePack = false;
            txfPackbarcode.Focus();
        }
        private void btnPack_Clicked(object sender, EventArgs e)
        {
            lblQuestion.Text = "Scan BARCODE of product to be Re-packed:";
            imgUnpackMain.Source = "repackboxicon.png";
            OpenLayout.IsVisible = false;
            MainLayout.IsVisible = true;
            MustMakePack = true;
            txfPackbarcode.Focus();
        }
        protected override bool OnBackButtonPressed()
        {
            if (!MainLayout.IsVisible)
            {
                Navigation.PopAsync();
            }
            else
            {
                OpenLayout.IsVisible = true;
                MainLayout.IsVisible = false;               
            }
            return false;
        }
    }
}
