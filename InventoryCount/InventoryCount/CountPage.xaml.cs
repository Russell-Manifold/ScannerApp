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
    public partial class CountPage : ContentPage
    {
        public static List<InventoryItem> items = new List<InventoryItem>();
        IMessage message = DependencyService.Get<IMessage>();
        private ExtendedEntry _currententry;
        private InventoryItem currentItem = new InventoryItem();
        private int countID = 0;
        private int CurrentQTYCounted = 0;
        Button btn1 = new Button();
        public CountPage(int i)
        {
            InitializeComponent();
            countID = i;
            txfItemCode.Focused += Entry_Focused;
        }
        protected async override void OnAppearing()
        {
            base.OnAppearing();           
            if (countID==0)
            {
                Vibration.Vibrate();
                message.DisplayMessage("Please select a valid Count",true);
                await Navigation.PopAsync();
            }
            if(!await GetItems())
            {
                Vibration.Vibrate();
                message.DisplayMessage("Error in fetching data", true);
                await Navigation.PopAsync();
            }
            if(!RefreshList())
            {
                Vibration.Vibrate();
                message.DisplayMessage("Could not display items", true);
                await Navigation.PopAsync();
            }
            if (InvLandingPage.CustQty)
            {
                LineT.IsVisible = true;
                lblMain.Text = "Scan SINGLE item codes";
            }
            txfItemCode.Focus();
        }
        async Task<bool> GetItems()
        {
            if (Connectivity.NetworkAccess==NetworkAccess.Internet)
            {
                RestClient client = new RestClient();
                string path = "Inventory";
                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                {
                    string str = $"GET?CountIDNum={countID}";
                    var Request = new RestRequest(str, Method.GET);
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    if (res.IsSuccessful && res.Content.Contains("CountID"))
                    {
                        items.Clear();
                        DataSet myds = new DataSet();
                        myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                        foreach (DataRow row in myds.Tables[0].Rows)
                        {
                            InventoryItem i1 = new InventoryItem();
                            i1.CountID = countID;
                            i1.ItemDesc = row["ItemDesc"].ToString();
                            i1.BarCode = row["BarCode"].ToString();
                            i1.ItemCode = row["ItemCode"].ToString();
                            i1.Bin = row["Bin"].ToString();
                            i1.isFirst = Convert.ToBoolean(row["isFirst"].ToString());
                            i1.Complete = Convert.ToBoolean(row["Complete"].ToString());
                            try
                            {
                                i1.FirstScanQty = Convert.ToInt32(row["FirstScanQty"].ToString());
                            }
                            catch
                            {
                                i1.FirstScanQty = 0;
                            }
                            try
                            {
                                i1.SecondScanQty = Convert.ToInt32(row["SecondScanQty"].ToString());
                            }
                            catch
                            {
                                i1.SecondScanQty = 0;
                            }
                            try
                            {
                                i1.CountUser = Convert.ToInt32(row["CountUser"].ToString()); ;
                            }
                            catch
                            {
                                i1.CountUser =0;
                            }
                            try
                            {
                                i1.SecondScanAuth = Convert.ToInt32(row["SecondScanAuth"].ToString()); ;
                            }
                            catch
                            {
                                i1.SecondScanAuth = 0;
                            }
                            items.Add(i1);
                        }
                        return true;
                    }
                }
            }
            else
            {
                Vibration.Vibrate();
                message.DisplayMessage("Please connect to the internet",true);
                return false;
            }
            return false;
        }
        bool RefreshList()
        {
            lstItems.ItemsSource = null;
            foreach (InventoryItem ite in items)
            {
                if (ite.Complete)
                {
                    ite.Status = "2";
                }
                else if (!ite.isFirst)
                {
                    ite.Status = "0";
                }else
                {
                    ite.Status = "1";
                }
            }
            lstItems.ItemsSource = items.OrderBy(x=>x.Status).ThenBy(x=>x.Bin);
            _ = CheckIfComplete();
            return true;
        }
        Task CheckIfComplete()
        {          
            foreach (InventoryItem i in items)
            {
                if (i.Complete == false)
                {
                    btnComplete.IsVisible = false;
                    return null;
                }
            }
            btnComplete.IsVisible = true;
            return null;
        }
        void Setlbl(string desc)
        {           
            lblLayout.IsVisible = true;
            currentItem = items.Where(x => x.ItemDesc == desc).First();
            lblCurrentItem.Text = currentItem .ItemDesc+ " : \nBin: "+currentItem.Bin;
            if (items.Where(x => x.ItemDesc == desc).First().isFirst)
            {
                lblLayout.BackgroundColor = Color.FromHex("#3F51B5");
                lblCurrentQty.Text = ""+ CurrentQTYCounted;
            }
            else
            {
                lblLayout.BackgroundColor = Color.Orange;
                lblCurrentQty.Text = "" + CurrentQTYCounted;
            }
        }
        void SameItemCheck(string itemCode)
        {
            if(currentItem.ItemCode != null)
            {
                if (itemCode != currentItem.ItemCode)
                {
                    CurrentQTYCounted = 0;
                }
            }
        }
        private async void txfItemCode_Completed(object sender, EventArgs e)
        {
            LoadingIndicator.IsVisible = true;
            if (txfItemCode.Text.Length>10)
            {
                if (items.Where(x => x.BarCode == txfItemCode.Text&&x.Complete==false).FirstOrDefault() != null)
                {
                    int CUSTQTY = 1;
                    if (InvLandingPage.CustQty)
                    {
                        string result = await DisplayPromptAsync("Custom QTY", "Enter QTY of SINGLE units","OK","Cancel",keyboard:Keyboard.Numeric);
                        switch (result)
                        {                              
                        case "Cancel":
                                Vibration.Vibrate();
                                message.DisplayMessage("You have to enter a QTY", true);
                                txfItemCode.Text = "";
                                LoadingIndicator.IsVisible = false;
                                txfItemCode.Focus();
                                return;
                            default:
                                try
                                {
                                    CUSTQTY = Convert.ToInt32(result);
                                }
                                catch
                                {
                                    Vibration.Vibrate();
                                    message.DisplayMessage("Please enter a valid QTY", true);
                                    txfItemCode.Text = "";
                                    LoadingIndicator.IsVisible = false;
                                    txfItemCode.Focus();
                                    return;
                                }
                                break;
                        }
                    }
                    SameItemCheck(items.Where(x => x.BarCode == txfItemCode.Text).First().ItemCode);
                    CurrentQTYCounted += CUSTQTY;
                    Setlbl(items.Where(x => x.BarCode == txfItemCode.Text).First().ItemDesc);
                        if (!RefreshList())
                        {
                            Vibration.Vibrate();
                            message.DisplayMessage("Could Not Refresh The List", true);
                            txfItemCode.Text = "";
                            LoadingIndicator.IsVisible = false;
                            txfItemCode.Focus();
                            return;
                        }            
                }
                else
                {
                    Vibration.Vibrate();
                    message.DisplayMessage("No item found or scanning complete for this item", true);
                    txfItemCode.Text = "";
                    LoadingIndicator.IsVisible = false;
                    txfItemCode.Focus();
                    return;
                }
            }
            else if(txfItemCode.Text.Length>7&&!InvLandingPage.CustQty)
            {
                BOMItem bi = new BOMItem();
                try
                {
                    bi = await GoodsRecieveingApp.App.Database.GetBOMItem(txfItemCode.Text);
                }
                catch
                {
                    Vibration.Vibrate();
                    message.DisplayMessage("Error! No Item with this code", true);
                    txfItemCode.Text = "";
                    LoadingIndicator.IsVisible = false;
                    txfItemCode.Focus();
                    return;
                }
                if (items.Where(x=>x.ItemCode==bi.ItemCode && x.Complete == false).FirstOrDefault() != null)
                {
                    SameItemCheck(bi.ItemCode);
                    CurrentQTYCounted += bi.Qty;
                    Setlbl(items.Where(x => x.ItemCode == bi.ItemCode).First().ItemDesc);
                        if (!RefreshList())
                        {
                            Vibration.Vibrate();
                            message.DisplayMessage("Could Not Refresh The List", true);
                            txfItemCode.Text = "";
                            LoadingIndicator.IsVisible = false;
                            txfItemCode.Focus();
                            return;
                        }                         
                }
                else
                {
                    Vibration.Vibrate();
                    message.DisplayMessage("No item found or scanning complete for this item", true);
                    txfItemCode.Text = "";
                    LoadingIndicator.IsVisible = false;
                    txfItemCode.Focus();
                    return;
                }
            }
            else if(InvLandingPage.CustQty&&(txfItemCode.Text.Length==8|| txfItemCode.Text.Length == 9))
            {
                Vibration.Vibrate();
                message.DisplayMessage("You cannot add a pack to a custom Qty scan", true);
            }
            txfItemCode.Text = "";
            LoadingIndicator.IsVisible = false;
            txfItemCode.Focus();
        }       
        private async void lstItems_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            InventoryItem dl = e.SelectedItem as InventoryItem;
            string output = await DisplayActionSheet("Reset Qty to zero for this item?", "YES", "NO");
            switch (output)
            {
                case "NO":
                    break;
                case "YES":
                   
                    if(!await ResetInDB(dl.ItemDesc, dl.isFirst))
                    {
                        Vibration.Vibrate();
                        message.DisplayMessage("Could not reset in Database", true);
                        txfItemCode.Text = "";
                        LoadingIndicator.IsVisible = false;
                        txfItemCode.Focus();
                        return;
                    }
                    else
                    {
                        if (dl.isFirst)
                        {
                            items.Where(x => x.ItemDesc == dl.ItemDesc).FirstOrDefault().FirstScanQty = 0;                           
                        }
                        else
                        {
                            items.Where(x => x.ItemDesc == dl.ItemDesc).FirstOrDefault().SecondScanQty = 0;
                        }
                        items.Where(x => x.ItemDesc == dl.ItemDesc).FirstOrDefault().SecondScanAuth = 0;
                        items.Where(x => x.ItemDesc == dl.ItemDesc).FirstOrDefault().Complete = false;
                    }
                    break;
            }
            if (!RefreshList())
            {
                Vibration.Vibrate();
                message.DisplayMessage("Could not refresh list", true);
            }
            txfItemCode.Text = "";
            LoadingIndicator.IsVisible = false;
            txfItemCode.Focus();
        }
        private async Task<bool> ResetInDB(string desc, bool isfirst)
        {
            string scanQty = "";
            if (isfirst)
            {
                scanQty = "FirstScanQty";
            }
            else
            {
                scanQty = "SecondScanQty";
            }
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                RestClient client = new RestClient();
                string path = "DocumentSQLConnection";
                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                {
                    string str = $"POST?qry=UPDATE InventoryLines SET {scanQty}=0,Complete='false',SecondScanAuth=0 WHERE CountID={countID} AND ItemDesc='{desc}'";
                    var Request = new RestRequest(str, Method.POST);
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);

                    if (res.IsSuccessful && res.Content.Contains("Complete"))
                    {
                        return true;
                    }
                }
            }
            else
            {
                return false;
            }
            return false;
        }
        private async void btnComplete_Clicked(object sender, EventArgs e)
        {
            message.DisplayMessage("Complete!",true);
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
        private async void btnSave_Clicked(object sender, EventArgs e)
        {
            message.DisplayMessage("Checking Values....", false);
            if (!await CheckWithSQL())
            {
                Vibration.Vibrate();
                message.DisplayMessage("Could not connect to DB", false);
            }
        }
        private void SetQty()
        {
            if (currentItem.isFirst)
            {
                items.Where(x => x.ItemCode == currentItem.ItemCode).FirstOrDefault().FirstScanQty = CurrentQTYCounted;
            }
            else
            {
                items.Where(x => x.ItemCode == currentItem.ItemCode).FirstOrDefault().SecondScanQty = CurrentQTYCounted;
            }
        }
        private async Task<bool> CheckWithSQL()
        {
            int QTY = 0;
            string itemcode = currentItem.ItemCode;           
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                RestClient client = new RestClient();
                string path = "ItemQOH";
                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                {
                    string stri = InvLandingPage.WH;
                    string str = $"GET?WH={InvLandingPage.WH}&ItemCode={itemcode}";
                    var Request = new RestRequest(str, Method.GET);
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    if (!res.IsSuccessful||res.Content==null)
                    {
                        Vibration.Vibrate();
                        message.DisplayMessage("Item not found", true);
                        return true;
                    }
                    else
                    {
                        QTY = Convert.ToInt32(Double.Parse(res.Content, CultureInfo.InvariantCulture.NumberFormat), CultureInfo.InvariantCulture.NumberFormat);                       
                    }
                }
            }
            else
            {
                Vibration.Vibrate();
                message.DisplayMessage("Please recconect to the internet", true);
                return true;
            }
            SetQty();
            if (currentItem.isFirst)
            {
                if (currentItem.FirstScanQty==QTY)
                {
                    items.Where(x => x.BarCode == currentItem.BarCode).First().Complete = true;
                    items.Where(x => x.BarCode == currentItem.BarCode).First().FinalQTY = items.Where(x=>x.BarCode==currentItem.BarCode).First().FirstScanQty;
                    await DisplayAlert("Complete!","QTY DID MATCH!","OK");
                }
                else
                {
                    items.Where(x => x.BarCode == currentItem.BarCode).First().FinalQTY = items.Where(x => x.BarCode == currentItem.BarCode).First().FirstScanQty;
                    items.Where(x => x.BarCode == currentItem.BarCode).First().isFirst = false;
                    Vibration.Vibrate();
                    await DisplayAlert("Error", "QTY DID NOT MATCH\nPlease recount now!", "OK");
                }
            }
            else
            {
                if (currentItem.SecondScanQty == QTY)
                {
                    items.Where(x => x.BarCode == currentItem.BarCode).First().Complete = true;
                    items.Where(x => x.BarCode == currentItem.BarCode).First().FinalQTY = items.Where(x => x.BarCode == currentItem.BarCode).First().SecondScanQty;
                    await DisplayAlert("Complete!", "QTY DID MATCH!", "OK");
                }
                else
                {
                    items.Where(x => x.BarCode == currentItem.BarCode).First().Complete = true;
                    items.Where(x => x.BarCode == currentItem.BarCode).First().FinalQTY = items.Where(x => x.BarCode == currentItem.BarCode).First().SecondScanQty;
                    //await Navigation.PushAsync(new AcceptScanPage(currentItem,QTY));
                }
            }
            if (!await SendData())
            {
                Vibration.Vibrate();
                message.DisplayMessage("Could not update the database",true);
            }
            Navigation.InsertPageBefore(new CountPage(countID), Navigation.NavigationStack[3]);
            await Navigation.PopAsync();
            return true;
        }
        private async Task<bool> SendData()
        {
            var ds = new DataSet();
            try
            {
                var t1 = new DataTable();
                DataRow row = null;
                t1.Columns.Add("CountID");
                t1.Columns.Add("BarCode");
                t1.Columns.Add("ItemDesc");
                t1.Columns.Add("FirstScanQty");
                t1.Columns.Add("SecondScanQty");
                t1.Columns.Add("SecondScanAuth");
                t1.Columns.Add("CountUser");
                t1.Columns.Add("isFirst");
                t1.Columns.Add("Complete");
                t1.Columns.Add("Bin");
                t1.Columns.Add("FinalScanQty");
                InventoryItem iut = currentItem;
                row = t1.NewRow();
                row["CountID"] = countID;
                row["BarCode"] = iut.BarCode;
                row["FirstScanQty"] = iut.FirstScanQty;
                row["SecondScanQty"] = iut.SecondScanQty;
                row["SecondScanAuth"] = iut.SecondScanAuth;
                row["ItemDesc"] = iut.ItemDesc;
                row["CountUser"] = iut.CountUser;
                row["isFirst"] = iut.isFirst;
                row["Complete"] = iut.Complete;
                row["Bin"] = iut.Bin;
                row["FinalScanQty"] = iut.FinalQTY;
                t1.Rows.Add(row);
                ds.Tables.Add(t1);
            }
            catch (Exception)
            {
                return false;
            }
            string myds = Newtonsoft.Json.JsonConvert.SerializeObject(ds);
            RestSharp.RestClient client = new RestSharp.RestClient();
            client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath);
            {
                var Request = new RestSharp.RestRequest("Inventory", RestSharp.Method.POST);
                Request.RequestFormat = RestSharp.DataFormat.Json;
                Request.AddJsonBody(myds);
                var cancellationTokenSource = new CancellationTokenSource();
                var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                if (res.IsSuccessful && res.Content.Contains("Complete"))
                {
                    return true;
                }
            }
            return false;
        }    
    }
}