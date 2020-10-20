using Data.KeyboardContol;
using Data.Message;
using Data.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PickAndPack
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SingleOrder : ContentPage
    {
        IMessage message = DependencyService.Get<IMessage>();
        int currentPallet = 0;
        List<int> AllPallets = new List<int>();
        private ExtendedEntry _currententry;
        public SingleOrder()
        {
            InitializeComponent();
            txfSOCode.Focused += Entry_Focused;
            txfItemCode.Focused += Entry_Focused;
            _ = checkCompete();
        }
        private async Task checkCompete()
        {
            if (await CompleteCheck())
            {
                CompletedStack.IsVisible = true;
                palletAddStack.IsVisible = false;
            }
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            Task.Delay(100);
            txfSOCode.Focus();
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
        private async void txfSOCode_Completed(object sender, EventArgs e)
        {
            if (txfSOCode.Text.Length != 0)
            {
                LodingIndiactor.IsVisible = true;
                if (await GetItems(txfSOCode.Text.ToUpper()))
                {
                    if (!await PalletCheck())
                    {
                        LodingIndiactor.IsVisible = false;
                        await Navigation.PopAsync();
                        return;
                    }
                    if (!await GetPallets(txfSOCode.Text.ToUpper()))
                    {
                        LodingIndiactor.IsVisible = false;
                        await Navigation.PopAsync();
                        return;
                    }
                    currentPallet = AllPallets.First();
                    DocLine d = new DocLine();

                    try
                    {
                        d = await GoodsRecieveingApp.App.Database.GetOneSpecificDocAsync(txfSOCode.Text.ToUpper());
                    }
                    catch (Exception ef)
                    {
                    }
                    txfSOCode.IsEnabled = false;
                    txfSOCode.IsVisible = false;
                    try
                    {
                        await GoodsRecieveingApp.App.Database.Delete(await GoodsRecieveingApp.App.Database.GetHeader(d.DocNum));
                    }
                    catch
                    {

                    }
                    await GoodsRecieveingApp.App.Database.Insert(new DocHeader { DocNum = txfSOCode.Text.ToUpper(), PackerUser = GoodsRecieveingApp.MainPage.UserCode, AccName = d.SupplierName, AcctCode = d.SupplierCode });
                    LodingIndiactor.IsVisible = false;
                    lblPalletNumber.Text = "" + currentPallet;
                    ToolbarItem item = new ToolbarItem
                    {
                        IconImageSource = "ViewAll.png",
                        Order = ToolbarItemOrder.Primary
                    };
                    item.Clicked += btnViewSO_Clicked;
                    this.ToolbarItems.Add(item);
                    ItemCodeLayout.IsVisible = true;
                    GridLayout.IsVisible = true;
                    await RefreshList();
                    txfItemCode.Focus();
                }
                else
                {
                    txfSOCode.Text = "";
                    txfSOCode.Focus();
                }
            }
        }
        async Task<bool> PalletCheck()
        {
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                RestSharp.RestClient client = new RestSharp.RestClient();
                string path = "DocumentSQLConnection";
                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                {
                    string str = $"GET?qry=SELECT * FROM PalletTransaction WHERE SONum='{txfSOCode.Text.ToUpper()}'";
                    var Request = new RestSharp.RestRequest();
                    Request.Resource = str;
                    Request.Method = RestSharp.Method.GET;
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    if (res.IsSuccessful && res.Content.Contains("PalletID"))
                    {
                        DataSet myds = new DataSet();
                        myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                        if (!(myds.Tables[0].Rows.Count > 1))
                        {
                            if (myds.Tables[0].Rows.Count == 1)
                            {
                                //if (!isMultiPallet)
                                //{
                                //   Vibration.Vibrate();
                                //  message.DisplayMessage("This order has been started as a single pallet", true);                                   
                                //  return false;
                                // }
                            }
                        }
                    }
                    else
                    {
                        if (!await AddPallet(txfSOCode.Text.ToUpper()))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                Vibration.Vibrate();
                message.DisplayMessage("Please recconect to the internet", true);
                return false;
            }
            return true;
        }
        private async Task<bool> GetItems(string code)
        {
            if (await RemoveAllOld(code))
            {
                if (Connectivity.NetworkAccess == NetworkAccess.Internet)
                {
                    RestSharp.RestClient client = new RestSharp.RestClient();
                    string path = "GetDocument";
                    client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                    {
                        string str = $"GET?qrystr=ACCHISTL|6|{code}|102|" + GoodsRecieveingApp.MainPage.UserCode;
                        var Request = new RestSharp.RestRequest();
                        Request.Resource = str;
                        Request.Method = RestSharp.Method.GET;
                        var cancellationTokenSource = new CancellationTokenSource();
                        var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                        if (res.Content.ToString().Contains("DocNum"))
                        {
                            await GoodsRecieveingApp.App.Database.DeleteSpecificDocs(code);
                            DataSet myds = new DataSet();
                            myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                            foreach (DataRow row in myds.Tables[0].Rows)
                            {
                                try
                                {
                                    var Doc = new DocLine();
                                    Doc.DocNum = row["DocNum"].ToString();
                                    Doc.SupplierCode = row["SupplierCode"].ToString();
                                    Doc.SupplierName = row["SupplierName"].ToString();
                                    Doc.ItemBarcode = row["ItemBarcode"].ToString();
                                    Doc.ItemCode = row["ItemCode"].ToString();
                                    Doc.ItemDesc = row["ItemDesc"].ToString();
                                    Doc.Bin = row["Bin"].ToString();
                                    try
                                    {
                                        Doc.ScanAccQty = Convert.ToInt32(row["ScanAccQty"].ToString().Trim());
                                    }
                                    catch
                                    {
                                        Doc.ScanAccQty = 0;
                                    }
                                    Doc.ScanRejQty = 0;
                                    try
                                    {
                                        Doc.PalletNum = Convert.ToInt32(row["PalletNumber"].ToString().Trim());
                                    }
                                    catch
                                    {
                                        Doc.PalletNum = 0;
                                    }
                                    try
                                    {
                                        Doc.Balacnce = Convert.ToInt32(row["Balacnce"].ToString().Trim());
                                    }
                                    catch
                                    {
                                        Doc.Balacnce = 0;
                                    }
                                    currentPallet = Doc.PalletNum;
                                    if (Convert.ToInt32(Doc.Balacnce) == -1)
                                    {
                                        Doc.Balacnce = 0;
                                    }
                                    Doc.ItemQty = Convert.ToInt32(row["ItemQty"].ToString().Trim());
                                    lblSOCode.Text = Doc.SupplierName + " - " + Doc.DocNum;
                                    await GoodsRecieveingApp.App.Database.Insert(Doc);
                                }
                                catch (Exception ex)
                                {
                                    LodingIndiactor.IsVisible = false;
                                    Vibration.Vibrate();
                                    message.DisplayMessage("Error In Server!!" + ex, true);
                                    return false;
                                }
                            }
                            return true;
                        }
                        else
                        {
                            LodingIndiactor.IsVisible = false;
                            Vibration.Vibrate();
                            message.DisplayMessage("Error Invalid SO Code!!", true);
                        }
                    }
                }
                else
                {
                    LodingIndiactor.IsVisible = false;
                    Vibration.Vibrate();
                    message.DisplayMessage("Internet Connection Problem!", true);
                }
            }
            return false;
        }
        private async Task<bool> RemoveAllOld(string docNum)
        {
            try
            {
                foreach (DocLine dl in (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(docNum)))
                {
                    await GoodsRecieveingApp.App.Database.Delete(dl);
                }
            }
            catch
            {
            }
            return true;
        }
        private async void btnViewSO_Clicked(object sender, EventArgs e)
        {
            message.DisplayMessage("Loading...", false);
            try
            {
                await GoodsRecieveingApp.App.Database.GetOneSpecificDocAsync(txfSOCode.Text.ToUpper());
                await Navigation.PushAsync(new ViewItems(txfSOCode.Text.ToUpper()));
            }
            catch
            {
                Vibration.Vibrate();
                message.DisplayMessage("Error!Could not load SO", false);
            }
        }
        private async void txfItemCode_Completed(object sender, EventArgs e)
        {
            if (txfItemCode.Text.Length != 0)
            {
                //txfItemCode.Text = GoodsRecieveingApp.MainPage.CalculateCheckDigit(txfItemCode.Text);
                if (txfItemCode.Text.Length == 8)
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
                        txfItemCode.Focus();
                        return;
                    }
                    if (await CheckOrderItemCode(bi.ItemCode))
                    {
                        List<DocLine> docs = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text.ToUpper())).Where(x => x.ItemCode == bi.ItemCode).ToList();
                        int i = docs.Sum(x => x.ScanAccQty);
                        if (i + bi.Qty <= docs.First().ItemQty)
                        {
                            DocLine docline = new DocLine { Balacnce = 0, Complete = "No", DocNum = txfSOCode.Text.ToUpper(), isRejected = false, ItemBarcode = docs.First().ItemBarcode, ItemDesc = docs.First().ItemDesc, ItemCode = docs.First().ItemCode, ItemQty = docs.First().ItemQty, PalletNum = currentPallet, ScanAccQty = bi.Qty };
                            await GoodsRecieveingApp.App.Database.Insert(docline);
                            await RefreshList();
                        }
                        else
                        {
                            Vibration.Vibrate();
                            message.DisplayMessage("All of this item have been scanned for this order", true);
                        }
                    }
                    else
                    {
                        Vibration.Vibrate();
                        message.DisplayMessage("This Item is not on this order", true);
                    }
                }
                else
                {
                    if (await CheckOrderBarcode(txfItemCode.Text))
                    {
                        List<DocLine> docs = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text.ToUpper())).Where(x => x.ItemBarcode == txfItemCode.Text).ToList();
                        int i = docs.Sum(x => x.ScanAccQty);
                        if (i + 1 <= docs.First().ItemQty)
                        {
                            DocLine docline = new DocLine { Balacnce = 0, Complete = "No", DocNum = txfSOCode.Text.ToUpper(), isRejected = false, ItemBarcode = txfItemCode.Text, ItemDesc = docs.First().ItemDesc, ItemCode = docs.First().ItemCode, ItemQty = docs.First().ItemQty, PalletNum = currentPallet, ScanAccQty = 1 };
                            await GoodsRecieveingApp.App.Database.Insert(docline);
                            await RefreshList();
                        }
                        else
                        {
                            Vibration.Vibrate();
                            message.DisplayMessage("All of this item have been scanned for this order", true);
                        }
                    }
                    else
                    {
                        Vibration.Vibrate();
                        message.DisplayMessage("This Item is not on this order", true);
                    }
                }
                if (await CompleteCheck())
                {
                    CompletedStack.IsVisible = true;
                    palletAddStack.IsVisible = false;
                }
                else
                {
                    CompletedStack.IsVisible = false;
                    palletAddStack.IsVisible = true;
                }
                txfItemCode.Text = "";
                txfItemCode.Focus();
            }
        }
        private async Task<bool> CompleteCheck()
        {
            List<DocLine> docs = await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text.ToUpper());
            foreach (string str in docs.Select(x => x.ItemCode).Distinct())
            {
                try
                {
                    if (docs.Where(x => x.ItemCode == str).FirstOrDefault().ItemQty != docs.Where(x => x.ItemCode == str).Sum(c => c.ScanAccQty))
                    {
                        return false;
                    }
                }
                catch
                {
                    return false;
                }
            }
            return true;
        }
        async Task<bool> CheckOrderBarcode(string Code)
        {
            List<DocLine> docs = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text.ToUpper())).Where(x => x.ItemBarcode == Code).ToList();
            if (docs.Count == 0)
                return false;

            return true;
        }
        async Task<bool> CheckOrderItemCode(string Code)
        {
            List<DocLine> docs = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text.ToUpper())).Where(x => x.ItemCode == Code).ToList();
            if (docs.Count == 0)
                return false;

            return true;
        }
        async Task RefreshList()
        {
            try
            {
                lstItems.ItemsSource = null;
                List<DocLine> docs = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text.ToUpper())).Where(x => x.PalletNum == currentPallet || x.ScanAccQty == 0).OrderBy(s => s.Bin).ToList();
                if (docs == null)
                    return;
                List<DocLine> displayDocs = new List<DocLine>();
                foreach (string s in docs.Select(x => x.ItemDesc).Distinct())
                {
                    DocLine TempDoc = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text.ToUpper())).Where(x => x.ItemDesc == s).First();
                    TempDoc.ScanAccQty = docs.Where(x => x.ItemDesc == s).Sum(x => x.ScanAccQty);
                    TempDoc.ItemQty = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text.ToUpper())).Where(x => x.ItemDesc == s).First().ItemQty;
                    TempDoc.PalletNum = currentPallet;
                    TempDoc.Balacnce = TempDoc.ItemQty - (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text.ToUpper())).Where(x => x.ItemDesc == s).Sum(x => x.ScanAccQty);
                    if (TempDoc.Balacnce == 0)
                    {
                        TempDoc.Complete = "Yes";
                    }
                    else if (TempDoc.Balacnce != TempDoc.ItemQty)
                    {
                        TempDoc.Complete = "No";
                    }
                    else
                    {
                        TempDoc.Complete = "NotStarted";
                    }
                    displayDocs.Add(TempDoc);
                }
                lstItems.ItemsSource = displayDocs;
            }
            catch (Exception ec)
            {
                Vibration.Vibrate();
                message.DisplayMessage("Could not load SO" + ec, true);
            }

        }
        private async void lstItems_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            DocLine dl = e.SelectedItem as DocLine;
            string output = await DisplayActionSheet("Reset Qty to zero for this item on this pallet?", "YES", "NO");
            switch (output)
            {
                case "NO":
                    break;
                case "YES":
                    await GoodsRecieveingApp.App.Database.DeleteAllWithItemWithFilter(dl);
                    await RefreshList();
                    break;
            }
        }
        private async void btnPrevPallet_Clicked(object sender, EventArgs e)
        {
            if (currentPallet == AllPallets.First())
            {
                Vibration.Vibrate();
                message.DisplayMessage("This is the first pallet", false);
            }
            else
            {
                currentPallet = AllPallets[AllPallets.IndexOf(currentPallet - 1)];
                lblPalletNumber.Text = "" + currentPallet;
                await RefreshList();
            }
            txfItemCode.Focus();
        }
        async Task<bool> GetPallets(string SOCode)
        {
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                RestSharp.RestClient client = new RestSharp.RestClient();
                string path = "Pallet";
                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                {
                    string str = $"POST?SOCode={SOCode}";
                    var Request = new RestSharp.RestRequest();
                    Request.Resource = str;
                    Request.Method = RestSharp.Method.POST;
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    if (res.IsSuccessful && res.Content.Contains("Complete"))
                    {
                        string s = res.Content.Replace('"', ' ').Replace('\\', ' ').Trim();
                        foreach (string strin in s.Split('|'))
                        {
                            if (strin != "")
                            {
                                try
                                {
                                    if (!AllPallets.Contains(Convert.ToInt32(strin)))
                                    {
                                        AllPallets.Add(Convert.ToInt32(strin));
                                        AllPallets.Sort();
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                    else
                    {
                        Vibration.Vibrate();
                        message.DisplayMessage("Error in the server", true);
                        return false;
                    }
                }
            }
            else
            {
                Vibration.Vibrate();
                message.DisplayMessage("Please recconect to the internet", true);
                return false;
            }
            return true;
        }
        private async void btnNextPallet_Clicked(object sender, EventArgs e)
        {
            foreach (DocLine dl in await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text.ToUpper()))
            {
                if (dl.PalletNum == currentPallet)
                {
                    if (currentPallet == AllPallets.Last())
                    {
                        if (!await AddPallet(txfSOCode.Text.ToUpper()))
                        {
                            Vibration.Vibrate();
                            message.DisplayMessage("Pallet could not be added", true);
                            return;
                        }
                        if (!AllPallets.Contains(currentPallet))
                        {
                            AllPallets.Add(currentPallet);
                            AllPallets.Sort();
                        }
                    }
                    else
                    {
                        currentPallet = AllPallets[(AllPallets.IndexOf(currentPallet) + 1)];
                    }
                    lblPalletNumber.Text = "" + currentPallet;
                    await RefreshList();
                    txfItemCode.Focus();
                    return;
                }
            }
            Vibration.Vibrate();
            message.DisplayMessage("You must put at least one item on this pallet", false);
            txfItemCode.Focus();
        }
        async Task<bool> AddPallet(string SOCode)
        {
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                RestSharp.RestClient client = new RestSharp.RestClient();
                string path = "DocumentSQLConnection";
                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                {
                    string qry = $"SELECT MAX(PalletID)AS[MAXPall] FROM PalletTransaction";
                    string str = $"GET?qry={qry}";
                    var Request = new RestSharp.RestRequest();
                    Request.Resource = str;
                    Request.Method = RestSharp.Method.GET;
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    if (res.IsSuccessful && res.Content.Contains("MAXPall"))
                    {
                        DataSet myds = new DataSet();
                        myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                        int maxpal = 0;
                        try
                        {
                            maxpal = Convert.ToInt32(myds.Tables[0].Rows[0]["MAXPall"]);
                        }
                        catch
                        {
                            maxpal = 0;
                        }
                        await NewPallet(maxpal + 1);
                    }
                    else
                    {
                        Vibration.Vibrate();
                        message.DisplayMessage("Error in the server!", true);
                        return false;
                    }
                }
            }
            else
            {
                Vibration.Vibrate();
                message.DisplayMessage("Please recconect to the internet", true);
                return false;
            }
            return true;
        }
        async Task<bool> NewPallet(int pallet)
        {
            RestSharp.RestClient client = new RestSharp.RestClient();
            string path = "DocumentSQLConnection";
            client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
            {
                string qry = $"INSERT INTO PalletTransaction(SONum, PalletID) VALUES('{txfSOCode.Text.ToUpper()}',{pallet})";
                string str = $"POST?qry={qry}";
                var Request = new RestSharp.RestRequest();
                Request.Resource = str;
                Request.Method = RestSharp.Method.POST;
                var cancellationTokenSource = new CancellationTokenSource();
                var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                if (res.IsSuccessful && res.Content.Contains("Complete"))
                {
                    currentPallet = pallet;
                    return true;
                }
                else
                {
                    Vibration.Vibrate();
                    message.DisplayMessage("Error in the server!", true);
                    return false;
                }
            }
        }
        private async void btnSave_Clicked(object sender, EventArgs e)
        {
            message.DisplayMessage("Saving....", true);
            if (await SaveData())
            {
                Navigation.RemovePage(Navigation.NavigationStack[2]);
                await Navigation.PopAsync();
            }
            else
            {
                Vibration.Vibrate();
                message.DisplayMessage("Error!!! Could Not Save!", true);
            }
        }
        private async Task<bool> SaveData()
        {
            var ds = new DataSet();
            try
            {
                var t1 = new DataTable();
                DataRow row = null;
                t1.Columns.Add("DocNum");
                t1.Columns.Add("ItemBarcode");
                t1.Columns.Add("ScanAccQty");
                t1.Columns.Add("Balance");
                t1.Columns.Add("ScanRejQty");
                t1.Columns.Add("PalletNumber");
                t1.Columns.Add("GRV");
                string s = txfSOCode.Text.ToUpper();
                List<DocLine> docs = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(s)).ToList();
                foreach (string str in docs.Select(x => x.ItemDesc).Distinct())
                {
                    foreach (int ints in docs.Where(x => x.PalletNum != 0 && x.ItemDesc == str).Select(x => x.PalletNum).Distinct())
                    {
                        row = t1.NewRow();
                        row["DocNum"] = docs.Select(x => x.DocNum).FirstOrDefault();
                        row["ScanAccQty"] = docs.Where(x => x.PalletNum == ints && x.ItemDesc == str).Sum(x => x.ScanAccQty);
                        row["ScanRejQty"] = 0;
                        row["ItemBarcode"] = docs.Where(x => x.PalletNum == ints && x.ItemDesc == str).Select(x => x.ItemBarcode).FirstOrDefault();
                        row["Balance"] = -1;
                        row["PalletNumber"] = ints;
                        row["GRV"] = false;
                        t1.Rows.Add(row);
                    }
                }
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
                var Request = new RestSharp.RestRequest("SaveDocLine", RestSharp.Method.POST);
                Request.RequestFormat = RestSharp.DataFormat.Json;
                Request.AddJsonBody(myds);
                var cancellationTokenSource = new CancellationTokenSource();
                var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                if (res.IsSuccessful && res.Content.Contains("COMPLETE"))
                {
                    return true;
                }
            }
            return false;
        }
        async Task<bool> SendStatus(string status)
        {
            string Status = "DocControlUser";
            if (status == "2")
            {
                Status = "PickerUser";
            }
            else if (status == "3")
            {
                Status = "PackerUser";
            }
            else if (status == "4")
            {
                Status = "AuthUser";
            }
            else
            {
                Status = "DocControlUser";
            }
            RestSharp.RestClient client = new RestSharp.RestClient();
            string path = "DocumentSQLConnection";
            client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
            {
                string str = $"POST?qry=UPDATE tblTempDocHeader SET DocStatus={((Convert.ToInt32(status)) + 1)},{Status}=1 WHERE DocNum ='1'";
                var Request = new RestSharp.RestRequest();
                Request.Resource = str;
                Request.Method = RestSharp.Method.POST;
                var cancellationTokenSource = new CancellationTokenSource();
                var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                if (res.Content.ToString().Contains("Complete"))
                {
                    return true;
                }
            }
            return false;
        }
        private async void btnComplete_Clicked(object sender, EventArgs e)
        {
            await SaveData();
            btnViewSO_Clicked(null, null);
        }
    }
}