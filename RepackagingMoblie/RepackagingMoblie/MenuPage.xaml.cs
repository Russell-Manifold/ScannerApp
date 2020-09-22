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
    public partial class MenuPage : ContentPage
    {
        private ExtendedEntry _currententry;
        IMessage message = DependencyService.Get<IMessage>();
        string PackCode="";
        bool Unpack = false;
        public MenuPage(bool isUnPacking)
        {
            InitializeComponent();
            Unpack = isUnPacking;
            txfScanPack.Focused += Entry_Focused;
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
        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (Unpack)
            {
                Mainlayout.IsVisible = false;
                IntoPackLayout.IsVisible = true;
                RepackingImg.Source = "repackboxicon.png";
                txfScanPack.Focus();
            }
            else
            {
                try
                {
                    lblBOMInfo.Text = "You have repacked " + MainPage.docLines.FindAll(x => x.ItemDesc != "1ItemFromMain").Sum(x => x.ItemQty) + " / " + +MainPage.docLines.Find(x => x.ItemDesc == "1ItemFromMain").ItemQty + " items";
                }
                catch
                {
                    lblBOMInfo.Text = "0/" + MainPage.docLines.Find(x => x.ItemDesc == "1ItemFromMain").ItemQty + " Repacked";
                }
            }
        }
        private async void BtnSingles_Clicked(object sender, EventArgs e)
        {           
            await Navigation.PushAsync(new Singles());
        }
        private async void BtnCustom_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new Custom());
            
        } 
        private async void BtnDamaged_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new DamagedGoods());
        }
        private async void BtnComplete_Clicked(object sender, EventArgs e)
        {
            if (MainPage.docLines.Find(x=>x.ItemDesc=="1ItemFromMain").ItemQty==MainPage.docLines.FindAll(x=>x.ItemDesc!= "1ItemFromMain").Sum(x=>x.ItemQty))
            {
                if (MainPage.PackCodes.Count > 0)
                {
                   if(!await SendPrinterCodes())
                    {
                        return;
                    }
                }                
                lblBOMInfo.TextColor = System.Drawing.Color.Gray;
                message.DisplayMessage("Repacking Complete!!", true);
                Navigation.RemovePage(Navigation.NavigationStack[2]);
                await Navigation.PopAsync();
            }
            else
            {
                Vibration.Vibrate();
                message.DisplayMessage("Not All items have benn packed", true);
            }
        }
        async Task<bool> SendPrinterCodes()
        {
            RestClient client = new RestClient();
            string path = "DocumentSQLConnection";
            client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
            {
                foreach (string s in MainPage.PackCodes)
                {
                    string str = $"POST?qry=INSERT INTO tblPrintQue (Barcode,Qty)VALUES('{s}',1)";
                    var Request = new RestSharp.RestRequest(str,Method.POST);
                    CancellationTokenSource ct = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request,ct.Token);
                     if (!res.IsSuccessful)
                    {
                        Vibration.Vibrate();
                        message.DisplayMessage("Could not send the codes",false);
                        return false;
                    }
                }
                return true;
            }
        }
        async Task<bool> CheckExists(string qty)
        {
            RestClient client = new RestClient();
            string path = "CheckBOMExists";
            client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
            {
                string str = $"GET?qrystr=ACCBOML|2|{MainPage.docLines.FirstOrDefault().ItemCode}|{qty}";
                var Request = new RestSharp.RestRequest(str, Method.GET);
                CancellationTokenSource ct = new CancellationTokenSource();
                var res = await client.ExecuteAsync(Request, ct.Token);
                if (res.IsSuccessful)
                {
                    string returnVal = res.Content.Substring(1, res.Content.Length - 2);
                    if (returnVal.Split('|')[0] == "0")
                    {
                        if (returnVal.Split('|')[1].Length>3)
                        {
                            PackCode = returnVal.Split('|')[1];
                            return true;
                        }                       
                    }
                }
            }
            return false;
        }
        private async void Button_Clicked_Home(object sender, EventArgs e)
        {
            string output = await DisplayActionSheet("Exit before the repacking is complete", "YES", "NO");
            switch (output)
            {
                case "NO":
                    break;
                case "YES":
                    Navigation.RemovePage(Navigation.NavigationStack[2]);
                    await Navigation.PopAsync();
                    break;
            }           
        }
        private async void Clear_Clicked(object sender, EventArgs e)
        {
            string output = await DisplayActionSheet("Confirm:- Clear repacking quantities scanned", "YES", "NO");
            switch (output)
            {
                case "NO":
                    break;
                case "YES":
                    MainPage.docLines.Clear();
                    await Navigation.PopAsync();
                    break;
            }
        }
        private async void txfScanPack_Completed(object sender, EventArgs e)
        {
            if (txfScanPack.Text.Length > 1)
            {
                try
                {
                    BOMItem bi = await GoodsRecieveingApp.App.Database.GetBOMItem(txfScanPack.Text);
                    string result=await DisplayActionSheet($"Pack {bi.Qty} : {bi.ItemDesc} into one pack","YES","NO");
                    if (result=="YES")
                    {
                        // sending pack code to print queue
                        RestClient client = new RestClient();
                        string path = "DocumentSQLConnection";
                        client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                        {
                            string str = $"POST?qry=INSERT INTO tblPrintQue (Barcode,Qty)VALUES('" + txfScanPack.Text + "',1)";
                            var Request = new RestSharp.RestRequest(str, Method.POST);
                            CancellationTokenSource ct = new CancellationTokenSource();
                            var res = await client.ExecuteAsync(Request, ct.Token);
                            if (!res.IsSuccessful)
                            {
                                Vibration.Vibrate();
                                message.DisplayMessage("Could not send the codes", false); 
                            }
                        }                       
                        message.DisplayMessage("COMPLETE!", false);
                        await Navigation.PopAsync();
                    }
                    else
                    {
                        txfScanPack.Text = "";
                        txfScanPack.Focus();
                    }
                }
                catch
                {
                    Vibration.Vibrate();
                    message.DisplayMessage("No pack code found", true);
                    txfScanPack.Text = "";
                    txfScanPack.Focus();
                }
            }
        }
        private async void txfNumberOfItem_Completed(object sender, EventArgs e)
        {
            if (txfNumberOfItem.Text.Length > 0)
            {
                try
                {
                    int i = Convert.ToInt32(txfNumberOfItem.Text);
                    if (!await CheckExists(i+""))
                    {
                        PackCode = "F";
                        if (i > 9)
                        {
                            PackCode += txfNumberOfItem.Text;
                        }
                        else
                        {
                            PackCode += "0" + txfNumberOfItem.Text;
                        }
                        PackCode += "" + MainPage.docLines.First().ItemBarcode.Substring(7, 5);
                    }                  
                    try
                    {
                        BOMItem boi = await GoodsRecieveingApp.App.Database.GetBOMItem(PackCode);
                        if (PackCode != null)
                        {
                            // sending pack code to print queue
                            RestClient client = new RestClient();
                            string path = "DocumentSQLConnection";
                            client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                            {
                                string str = $"POST?qry=INSERT INTO tblPrintQue (Barcode,Qty)VALUES('" + PackCode + "',1)";
                                var Request = new RestSharp.RestRequest(str, Method.POST);
                                CancellationTokenSource ct = new CancellationTokenSource();
                                var res = await client.ExecuteAsync(Request, ct.Token);
                                if (!res.IsSuccessful)
                                {
                                    Vibration.Vibrate();
                                    message.DisplayMessage("Could not send codes to printing", false);
                                }
                            }
                        }
                        message.DisplayMessage("Complete!", true);
                        await Navigation.PopAsync();
                    }
                    catch
                    {
                        RestClient client;
                        string path;
                        string result = await DisplayActionSheet("No Pack Code found would you like to create a new packcode?", "YES", "NO");
                        if (result == "YES")
                        {
                            message.DisplayMessage("Linking Codes .....", false);
                            if (PackCode != null)
                            {
                                // sending pack code to print queue
                                client = new RestClient();
                                path = "DocumentSQLConnection";
                                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                                {
                                    string str = $"POST?qry=INSERT INTO tblPrintQue (Barcode,Qty)VALUES('" + PackCode + "',1)";
                                    var Request = new RestSharp.RestRequest(str, Method.POST);
                                    CancellationTokenSource ct = new CancellationTokenSource();
                                    var res = await client.ExecuteAsync(Request, ct.Token);
                                    if (!res.IsSuccessful)
                                    {
                                        Vibration.Vibrate();
                                        message.DisplayMessage("Could not send the codes to printing", false);
                                    }
                                }
                            }
                            linkCodesInPastel(MainPage.docLines.FirstOrDefault().ItemCode,PackCode);
                            BOMItem itemB = new BOMItem();
                            itemB.PackBarcode = PackCode;
                            itemB.ItemCode = MainPage.docLines.FirstOrDefault().ItemCode;
                            itemB.Qty = Convert.ToInt16(txfNumberOfItem.Text);
                            itemB.ItemDesc = MainPage.docLines.FirstOrDefault().ItemDesc;
                            await GoodsRecieveingApp.App.Database.Insert(itemB);
                            message.DisplayMessage("Complete!", true);
                            await Navigation.PopAsync();
                        }
                    }
                }
                catch
                {
                    message.DisplayMessage("No BOM created!", true);
                }
            }
        }
        private string linkCodesInPastel(string itemCode, string barcode)
        {
            try
            {
                string BOMHead = $"{barcode}|BOM HEADER|12.1|12.2|12.3|12.4|12.5|12.6|12.7|12.8|12.9|12|10|20|30|100|200|300|{itemCode}|Y|Y|N||N|N|{MainPage.MainWH}";
                string line = $"{barcode}|{itemCode}|{txfNumberOfItem.Text.Trim()}|{MainPage.MainWH}#";
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
    }
}