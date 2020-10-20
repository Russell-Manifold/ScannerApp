using Data.KeyboardContol;
using Data.Message;
using Data.Model;
using RestSharp;
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
    public partial class SinglePallet : ContentPage
    {
        IMessage message = DependencyService.Get<IMessage>();
        private ExtendedEntry _currententry;
        bool firstSO = true;
        int currentPallet = 0;
        public SinglePallet()
        {
            InitializeComponent();
            txfSOCode.Focused += Entry_Focused;
            txfItemCode.Focused += Entry_Focused;
        }
        public SinglePallet(string SONumber)
        {
            InitializeComponent();
            txfSOCode.Focused += Entry_Focused;
            txfItemCode.Focused += Entry_Focused;
            txfSOCode.Text = SONumber;
            txfSOCode_Completed(null, null);
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            txfSOCode.Focus();
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
                            if (!firstSO)
                            {
                                if (!await PalletAddSO())
                                {
                                    return false;
                                }
                            }
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
                                    currentPallet = Doc.PalletNum;
                                    Doc.ItemQty = Convert.ToInt32(row["ItemQty"].ToString().Trim());
                                    lblCode.Text = Doc.DocNum;
                                    lblCusName.Text = Doc.SupplierName;
                                    await GoodsRecieveingApp.App.Database.Insert(Doc);
                                }
                                catch (Exception)
                                {
                                    LodingIndiactor.IsVisible = false;
                                    Vibration.Vibrate();
                                    message.DisplayMessage("Error In Server!!", true);
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
        private async void txfItemCode_Completed(object sender, EventArgs e)
        {
            if (txfItemCode.Text.Length != 0)
            {
                // txfItemCode.Text = GoodsRecieveingApp.MainPage.CalculateCheckDigit(txfItemCode.Text);
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
                        List<DocLine> docs = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text)).Where(x => x.ItemCode == bi.ItemCode).ToList();
                        int i = docs.Sum(x => x.ScanAccQty);
                        if (i + bi.Qty <= docs.First().ItemQty)
                        {
                            DocLine docline = new DocLine { Balacnce = 0, Complete = "No", DocNum = txfSOCode.Text, isRejected = false, ItemBarcode = docs.First().ItemBarcode, ItemDesc = docs.First().ItemDesc, ItemCode = docs.First().ItemCode, ItemQty = docs.First().ItemQty, PalletNum = currentPallet, ScanAccQty = bi.Qty };
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
                        List<DocLine> docs = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text)).Where(x => x.ItemBarcode == txfItemCode.Text).ToList();
                        int i = docs.Sum(x => x.ScanAccQty);
                        if (i + 1 <= docs.First().ItemQty)
                        {
                            DocLine docline = new DocLine { Balacnce = 0, Complete = "No", DocNum = txfSOCode.Text, isRejected = false, ItemBarcode = txfItemCode.Text, ItemDesc = docs.First().ItemDesc, ItemCode = docs.First().ItemCode, ItemQty = docs.First().ItemQty, PalletNum = currentPallet, ScanAccQty = 1 };
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
                    btnComplete.IsVisible = true;
                }
                else
                    btnComplete.IsVisible = false;
                txfItemCode.Text = "";
                txfItemCode.Focus();
            }
        }
        private async Task<bool> CompleteCheck()
        {
            List<DocLine> docs = await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text);
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
        private async void btnAddSoNumber_Clicked(object sender, EventArgs e)
        {
            string res = await DisplayActionSheet("Add New Order To This Pallet?", "YES", "NO");
            if (res == "YES")
            {
                firstSO = false;
                SOCodeLayout.IsVisible = true;
                AddSoLayout.IsVisible = false;
                txfSOCode.IsEnabled = true;
                txfSOCode.IsVisible = true;
                lblSOCode.IsVisible = true;
                ItemCodeLayout.IsVisible = false;
                GridLayout.IsVisible = false;
                txfSOCode.Text = "";
                txfSOCode.Focus();
            }
        }
        async Task<bool> PalletAddSO()
        {
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                RestSharp.RestClient client = new RestSharp.RestClient();
                string path = "DocumentSQLConnection";
                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                {
                    string qry = $"SELECT * FROM PalletTransaction WHERE SONum='{txfSOCode.Text}'";
                    string str = $"GET?qry={qry}";
                    var Request = new RestSharp.RestRequest();
                    Request.Resource = str;
                    Request.Method = RestSharp.Method.GET;
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    if (res.IsSuccessful && res.Content.Contains("SONum"))
                    {
                        DataSet myds = new DataSet();
                        myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                        if (myds.Tables[0].Rows.Count == 1)
                        {
                            if (!myds.Tables[0].Rows[0]["PalletID"].ToString().Contains(currentPallet + ""))
                            {
                                Vibration.Vibrate();
                                message.DisplayMessage("This order has been started on a different pallet", true);
                                return false;
                            }
                        }
                        else
                        {
                            Vibration.Vibrate();
                            message.DisplayMessage("This order is already being scanned on multiple pallets", true);
                            return false;
                        }
                        return true;
                    }
                    else
                    {
                        if (!await AddToPallet(txfSOCode.Text))
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
        async Task<bool> AddToPallet(string SOCode)
        {
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                RestSharp.RestClient client = new RestSharp.RestClient();
                string path = "DocumentSQLConnection";
                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                {
                    string qry = $"INSERT INTO PalletTransaction(SONum, PalletID) VALUES('{SOCode}',{currentPallet})";
                    string str = $"POST?qry={qry}";
                    var Request = new RestSharp.RestRequest();
                    Request.Resource = str;
                    Request.Method = RestSharp.Method.POST;
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    if (res.IsSuccessful && res.Content.Contains("Complete"))
                    {
                        return true;
                    }
                    else
                    {
                        Vibration.Vibrate();
                        message.DisplayMessage("Error in connecting to the API", true);
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
        }
        private async void btnViewSO_Clicked(object sender, EventArgs e)
        {
            message.DisplayMessage("Loading...", false);
            try
            {
                string result = await DisplayActionSheet("Are you sure you would like to close this SO?", "YES", "NO");
                if (result == "YES")
                {
                    await Navigation.PushAsync(new SinglePallet((sender as ToolbarItem).Text));
                    Navigation.RemovePage(Navigation.NavigationStack[3]);
                }
            }
            catch
            {
                Vibration.Vibrate();
                message.DisplayMessage("Error!Could not load SO", false);
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
                    if (Connectivity.NetworkAccess == NetworkAccess.Internet)
                    {
                        if (await ResetItem(dl))
                        {
                            await GoodsRecieveingApp.App.Database.DeleteAllWithItemWithFilter(dl);
                            await RefreshList();
                        }
                    }
                    else
                    {
                        Vibration.Vibrate();
                        message.DisplayMessage("No Internet Connection", true);
                    }
                    break;
            }
        }
        private async Task<bool> ResetItem(DocLine doc)
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
                row = t1.NewRow();
                row["DocNum"] = doc.DocNum;
                row["ItemBarcode"] = doc.ItemBarcode;
                row["ScanAccQty"] = 0;
                row["Balance"] = 0;
                row["ScanRejQty"] = 0;
                row["PalletNumber"] = doc.PalletNum;
                row["GRV"] = false;
                t1.Rows.Add(row);
                ds.Tables.Add(t1);
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
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception)
            {

            }
            return false;
        }
        private async void txfSOCode_Completed(object sender, EventArgs e)
        {
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                if (txfSOCode.Text.Length != 0)
                {
                    LodingIndiactor.IsVisible = true;
                    if (await GetItems(txfSOCode.Text.ToUpper()))
                    {
                        if (!await PalletCheck(txfSOCode.Text))
                        {
                            LodingIndiactor.IsVisible = false;
                            txfSOCode.Text = "";
                            txfSOCode.Focus();
                        }
                        DocLine d = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text)).First();
                        txfSOCode.IsEnabled = false;
                        txfSOCode.IsVisible = false;
                        lblSOCode.IsVisible = false;
                        try
                        {
                            await GoodsRecieveingApp.App.Database.Delete(await GoodsRecieveingApp.App.Database.GetHeader(d.DocNum));
                        }
                        catch { }
                        await GoodsRecieveingApp.App.Database.Insert(new DocHeader { DocNum = txfSOCode.Text, PackerUser = GoodsRecieveingApp.MainPage.UserCode, AccName = d.SupplierName, AcctCode = d.SupplierCode });
                        LodingIndiactor.IsVisible = false;
                        SOCodeLayout.IsVisible = false;
                        ItemCodeLayout.IsVisible = true;
                        AddSoLayout.IsVisible = true;
                        GridLayout.IsVisible = true;
                        string SONumbers = await GetAllSONumbers(currentPallet);
                        if (SONumbers == "")
                        {
                            SONumbers = txfSOCode.Text + "|";
                        }
                        List<string> codes = new List<string>();
                        foreach (ToolbarItem items in this.ToolbarItems)
                        {
                            codes.Add(items.Text);
                        }
                        foreach (string str in SONumbers.Split('|'))
                        {
                            if (str.Length > 1)
                            {
                                if (!codes.Contains(str))
                                {
                                    ToolbarItem item = new ToolbarItem
                                    {
                                        Text = str,
                                        Order = ToolbarItemOrder.Secondary
                                    };
                                    item.Clicked += btnViewSO_Clicked;
                                    this.ToolbarItems.Add(item);
                                }
                            }
                        }
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
            else
            {
                Vibration.Vibrate();
                message.DisplayMessage("No Internet Connection", true);
                txfSOCode.Text = "";
                txfSOCode.Focus();
            }

        }
        async Task<string> GetAllSONumbers(int palletNumber)
        {
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                RestSharp.RestClient client = new RestSharp.RestClient();
                string path = "DocumentSQLConnection";
                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                {
                    string qry = $"SELECT SONum FROM PalletTransaction WHERE PalletID={palletNumber}";
                    string str = $"GET?qry={qry}";
                    var Request = new RestSharp.RestRequest();
                    Request.Resource = str;
                    Request.Method = RestSharp.Method.GET;
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    if (res.IsSuccessful && res.Content.Contains("SONum"))
                    {
                        string output = "";
                        DataSet myds = new DataSet();
                        myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                        foreach (DataRow row in myds.Tables[0].Rows)
                        {
                            output += row["SONum"] + "|";
                        }
                        return output;
                    }
                    else
                    {
                        Vibration.Vibrate();
                        message.DisplayMessage("Error in fetching data", true);
                        return "";
                    }
                }
            }
            else
            {
                Vibration.Vibrate();
                message.DisplayMessage("Please recconect to the internet", true);
                return "";
            }
        }
        async Task RefreshList()
        {
            lstItems.ItemsSource = null;
            List<DocLine> docs = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text)).OrderBy(s => s.Bin).ToList();
            if (docs == null)
                return;
            List<DocLine> displayDocs = new List<DocLine>();
            foreach (string s in docs.Select(x => x.ItemDesc).Distinct())
            {
                try
                {
                    DocLine TempDoc = docs.Where(x => x.ItemDesc == s).First();
                    TempDoc.ScanAccQty = docs.Where(x => x.ItemDesc == s).Sum(x => x.ScanAccQty);
                    TempDoc.ItemQty = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text)).Where(x => x.PalletNum == 0 && x.ItemDesc == s).First().ItemQty;
                    TempDoc.Balacnce = TempDoc.ItemQty - (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text)).Where(x => x.ItemDesc == s).Sum(x => x.ScanAccQty);
                    displayDocs.Add(TempDoc);
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
                }
                catch (Exception exes)
                {
                    await DisplayAlert(exes + "", "", "OK");
                }
            }
            lstItems.ItemsSource = displayDocs;
        }
        async Task<bool> CheckOrderBarcode(string Code)
        {
            List<DocLine> docs = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text)).Where(x => x.ItemBarcode == Code).ToList();
            if (docs.Count == 0)
                return false;

            return true;
        }
        async Task<bool> CheckOrderItemCode(string Code)
        {
            List<DocLine> docs = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text)).Where(x => x.ItemCode == Code).ToList();
            if (docs.Count == 0)
                return false;

            return true;
        }
        private async Task<bool> PalletCheck(string SO)
        {
            RestSharp.RestClient client = new RestSharp.RestClient();
            string path = "Pallet";
            client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
            {
                string str = $"POST?SOCode={SO}";
                var Request = new RestSharp.RestRequest();
                Request.Resource = str;
                Request.Method = RestSharp.Method.POST;
                var cancellationTokenSource = new CancellationTokenSource();
                var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                if (res.IsSuccessful && res.Content.Contains("Complete"))
                {
                    string s = res.Content.Replace('"', ' ').Replace('\\', ' ').Trim();
                    if (s.Split('|').Count() > 3)
                    {
                        Vibration.Vibrate();
                        message.DisplayMessage("This order is already on multiple pallets", true);
                        return false;
                    }
                    else
                    {
                        try
                        {
                            int ext = Convert.ToInt32(s.Split('|')[1]);
                            currentPallet = ext;
                            return true;
                        }
                        catch (Exception e)
                        {
                            Vibration.Vibrate();
                            message.DisplayMessage("Could not create pallet", true);
                            return false;
                        }
                    }
                }
                return false;
            }

        }
        //private async Task<int> PalletCreate()
        //{
        //    RestSharp.RestClient client = new RestSharp.RestClient();
        //    string path = "DocumentSQLConnection";
        //    client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
        //    {
        //        string qry = "";                
        //        string str = $"Post?qry={qry}";
        //        var Request = new RestSharp.RestRequest();
        //        Request.Resource = str;
        //        Request.Method = RestSharp.Method.POST;
        //        var cancellationTokenSource = new CancellationTokenSource();
        //        var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
        //        if (res.IsSuccessful && res.Content.Contains("Complete"))
        //        {
        //            var RequestNum = new RestSharp.RestRequest("Get?qry=SELECT MAX(PalletID)As PalletID FROM PalletTransaction", RestSharp.Method.GET);
        //            var cancellationToken = new CancellationTokenSource();
        //            var Res2 = await client.ExecuteAsync(RequestNum, cancellationToken.Token);
        //            if (Res2.IsSuccessful && Res2.Content.Contains("PalletID"))
        //            {
        //                DataSet myds = new DataSet();
        //                myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(Res2.Content);
        //                return Convert.ToInt32(myds.Tables[0].Rows[0]["PalletID"].ToString());
        //            }
        //        }
        //        else if (res.Content.Contains("ErrorSystem.Data.SqlClient.SqlException"))
        //        {
        //            string qrys = "Post?qry=INSERT INTO PalletTransaction(SONum,PalletID) VALUES('" + txfSOCode.Text + "',1)";
        //            var RequestNum = new RestSharp.RestRequest(qrys, RestSharp.Method.POST);
        //            var cancellationToken = new CancellationTokenSource();
        //            var Res2 = await client.ExecuteAsync(RequestNum, cancellationToken.Token);
        //            if (Res2.IsSuccessful && Res2.Content.Contains("Complete"))
        //            {
        //                return 1;
        //            }
        //        }
        //    }
        //    return -1;
        //}      
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
                string s = txfSOCode.Text;
                List<DocLine> docs = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(s)).ToList();
                foreach (string str in docs.Select(x => x.ItemDesc).Distinct())
                {
                    foreach (int ints in docs.Select(x => x.PalletNum).Distinct())
                    {
                        row = t1.NewRow();
                        row["DocNum"] = docs.Select(x => x.DocNum).FirstOrDefault();
                        row["ScanAccQty"] = docs.Where(x => x.PalletNum == ints && x.ItemDesc == str).Sum(x => x.ScanAccQty);
                        row["ScanRejQty"] = 0;
                        row["ItemBarcode"] = docs.Where(x => x.PalletNum == ints && x.ItemDesc == str).Select(x => x.ItemBarcode).FirstOrDefault();
                        row["Balance"] = 0;
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
        private async void btnComplete_Clicked(object sender, EventArgs e)
        {
            message.DisplayMessage("Loading...", false);
            try
            {
                await SaveData();
                await GoodsRecieveingApp.App.Database.GetOneSpecificDocAsync(txfSOCode.Text.ToUpper());
                await Navigation.PushAsync(new ViewItems(txfSOCode.Text.ToUpper()));
            }
            catch
            {
                Vibration.Vibrate();
                message.DisplayMessage("Error!Could not load SO", false);
            }
        }
        //private async Task<bool> SendToPastel()
        //{
        //    List<DocLine> docs = await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(txfSOCode.Text);
        //    string docL = await CreateDocLines(docs);
        //    string docH = await CreateDocHeader();
        //    if (docL == "" || docH == "")
        //        return false;
        //    RestClient client = new RestClient();
        //    string path = "AddDocument";
        //    client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
        //    {
        //        string str = $"GET?DocHead={docH}&Docline={docL}&DocType=107";
        //        var Request = new RestRequest(str, Method.POST);
        //        var cancellationTokenSource = new CancellationTokenSource();
        //        var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
        //        if (res.IsSuccessful && res.Content.Contains("0"))
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}
        //async Task<string> GetGlCode(string itemCode, string WHCode)
        //{
        //    RestClient client = new RestClient();
        //    string path = "GetField";
        //    client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
        //    {
        //        string str = $"GET?qrystr=ACCSTKST|1|{itemCode}{WHCode}|2";
        //        var Request = new RestRequest(str, Method.GET);
        //        var cancellationTokenSource = new CancellationTokenSource();
        //        var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
        //        if (res.IsSuccessful && res.Content.Contains("0"))
        //        {
        //            str = $"GET?qrystr=ACCGRP|0|{res.Content.Replace('"', ' ').Replace('\\', ' ').Trim().Split('|')[1]}|3";
        //            Request = new RestRequest(str, Method.GET);
        //            cancellationTokenSource = new CancellationTokenSource();
        //            res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
        //            if (res.IsSuccessful && res.Content.Contains("0"))
        //            {
        //                return res.Content.Replace('"', ' ').Replace('\\', ' ').Trim().Split('|')[1];
        //            }
        //        }
        //    }
        //    return "";
        //}
        //async Task<string> CreateDocLines(List<DocLine> d)
        //{
        //    DataTable det = await GetDocDetails(txfSOCode.Text);
        //    if (det == null)
        //    {
        //        return "";
        //    }
        //    string s = "", GLCode = "";
        //    foreach (string CODE in d.Select(x => x.ItemCode).Distinct())
        //    {
        //        foreach (string WH in d.Where(x => x.ItemCode == CODE && x.ItemQty != 0).Select(x => x.WarehouseID).Distinct())
        //        {
        //            DataRow CurrentRow = det.Select($"ItemCode=={CODE}").FirstOrDefault();
        //            GLCode = await GetGlCode(CODE, WH);
        //            if (CurrentRow != null)
        //                s += $"{CurrentRow["CostPrice"].ToString()}|{CurrentRow["ItemQty"].ToString()}|{CurrentRow["ExVat"].ToString()}|{CurrentRow["InclVat"].ToString()}|{CurrentRow["Unit"].ToString()}|{CurrentRow["TaxType"].ToString()}|{CurrentRow["DiscType"].ToString()}|{CurrentRow["DiscPerc"].ToString()}|{GLCode}|{CurrentRow["ItemDesc"].ToString()}|4|{WH}|{CurrentRow["CostCode"].ToString()}#";
        //        }
        //    }
        //    return s;
        //}
        //async Task<string> CreateDocHeader()
        //{
        //    DataTable det = await GetDocDetails(txfSOCode.Text);
        //    if (det == null)
        //        return "";
        //    DataRow CurrentRow = det.Rows[0];
        //    return $"||Y|{CurrentRow["CustomerCode"].ToString()}|{DateTime.Now.ToString("dd/MM/yyyy")}|{CurrentRow["OrderNumber"].ToString()}||N|0|{CurrentRow["Message_1"].ToString()}|{CurrentRow["Message_2"].ToString()}|{CurrentRow["Message_3"].ToString()}|{CurrentRow["Address1"].ToString()}|{CurrentRow["Address2"].ToString()}|{CurrentRow["Address3"].ToString()}|{CurrentRow["Address4"].ToString()}||{CurrentRow["SalesmanCode"].ToString()}|00||{CurrentRow["Due_Date"].ToString()}|-|-|-|1#"; ;
        //}
        //async Task<DataTable> GetDocDetails(string DocNum)
        //{//http://192.168.0.100/FDBAPI/api/GetFullDocDetails/GET?qrystr=ACCHISTL|6|PO100330|106
        //    RestClient client = new RestClient();
        //    string path = "GetFullDocDetails";
        //    client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
        //    {
        //        string str = $"GET?qrystr=ACCHISTL|6|{DocNum}|106";
        //        var Request = new RestRequest(str, Method.GET);
        //        var cancellationTokenSource = new CancellationTokenSource();
        //        var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
        //        if (res.IsSuccessful && res.Content.Contains("0"))
        //        {
        //            DataSet myds = new DataSet();
        //            myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
        //            return myds.Tables[0];
        //        }
        //    }
        //    return null;
        //}
    }
}