
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
using ZXing.Net.Mobile.Forms;

namespace GoodsRecieveingApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ScanAcc : ContentPage
    {
        DocLine UsingDoc = new DocLine();
        List<DocLine> currentDocs;
        string lastItem;
        DeviceConfig config = new DeviceConfig();
        bool wrong;
        bool EditsMade=false;
        IMessage message = DependencyService.Get<IMessage>();
        private ExtendedEntry _currententry;
        public ScanAcc(DocLine d)
        {
            InitializeComponent();
            txfAccCode.Focused += Entry_Focused;
            UsingDoc = d;
            lblMainAcc.Text = UsingDoc.SupplierName + " - " + UsingDoc.DocNum;
        }
        protected async override void OnDisappearing()
        {
            base.OnDisappearing();
            if (EditsMade)
            {
                if (await SaveData())
                {
                    message.DisplayMessage("All data Saved", true);
                }
                else
                {
                    Vibration.Vibrate();
                    message.DisplayMessage("Error!!! Could Not Save!", true);
                }
            }           
        }
        private async Task<bool> SaveData()
        {
            List<DocLine> docs = new List<DocLine>();
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
                docs = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(UsingDoc.DocNum)).Where(x => x.ItemQty == 0).ToList();
                if (docs.Count == 0)
                    return true;
                foreach (string str in docs.Select(x => x.ItemCode).Distinct())
                {
                    //DocLine currentGRV = (await App.Database.GetSpecificDocsAsync(UsingDoc.DocNum)).Where(x => x.GRN && x.ItemCode == str).FirstOrDefault();
                    //if (currentGRV != null && await GRVmodule())
                    //{
                    //    row = t1.NewRow();
                    //    row["DocNum"] = UsingDoc.DocNum;
                    //    row["ItemBarcode"] = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(UsingDoc.DocNum)).Where(x => x.ItemCode == str && x.ItemQty != 0).FirstOrDefault().ItemBarcode;
                    //    row["ScanAccQty"] = currentGRV.ScanAccQty;
                    //    row["Balance"] = 0;
                    //    row["ScanRejQty"] = currentGRV.ScanRejQty;
                    //    row["PalletNumber"] = 0;
                    //    row["GRV"] = true;
                    //    t1.Rows.Add(row);
                    //}
                    //else if (currentGRV != null && !await GRVmodule())
                    //{
                    //    await DisplayAlert("Please set up GRV in the settings", "Error", "OK");
                    //}
                    List<DocLine> CurrItems = (await App.Database.GetSpecificDocsAsync(UsingDoc.DocNum)).Where(x => !x.GRN && x.ItemCode == str && x.ItemQty == 0).ToList();
                    if (CurrItems.Count() > 0)
                    {
                        row = t1.NewRow();
                        row["DocNum"] = UsingDoc.DocNum;
                        row["ItemBarcode"] = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(UsingDoc.DocNum)).Where(x => x.ItemCode == str && x.ItemQty != 0).FirstOrDefault().ItemBarcode;
                        row["ScanAccQty"] = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(UsingDoc.DocNum)).Where(x => x.ItemCode == str && !x.GRN).Sum(x => x.ScanAccQty);
                        row["Balance"] = 0;
                        row["ScanRejQty"] = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(UsingDoc.DocNum)).Where(x => x.ItemCode == str && !x.GRN).Sum(x => x.ScanRejQty);
                        row["PalletNumber"] = 0;
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
                //var res =  client.Execute(Request);
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
        private async Task<bool> GRVmodule()
        {
            try
            {
                var confifg = await GoodsRecieveingApp.App.Database.GetConfig();
                return confifg.GRVActive;
            }
            catch(Exception ex)
            {
                return false;
            }
            
        }
        protected async override void OnAppearing()
        {
            base.OnAppearing();
            currentDocs = await App.Database.GetSpecificDocsAsync(UsingDoc.DocNum);
            txfAccCode.Focus();
            EditsMade = false;
        }
        private async void EntryAcc_TextChanged(object sender, TextChangedEventArgs e)
        {
            
        }
        public bool Check(string Icode)
        {
            foreach (DocLine dl in currentDocs.Where(x => x.ItemQty != 0))
            {
                if (Icode == dl.ItemCode)
                {
                    return true;
                }
            }
            return false;
        }
        public bool CheckBarcode(string Barcode)
        {
            foreach (DocLine dl in currentDocs.Where(x => x.ItemQty != 0))
            {
                if (Barcode == dl.ItemBarcode)
                {
                    return true;
                }
            }
            return false;
        }
        private async void SetQtyDisplay(string Icode)
        {
            currentDocs = await App.Database.GetSpecificDocsAsync(UsingDoc.DocNum);
            int scanQty=0;            
            int OrderQty = currentDocs.Find(x => x.ItemCode == Icode && x.ItemQty != 0).ItemQty;
            lblitemDescAcc.Text = currentDocs.Find(x => x.ItemCode == Icode && x.ItemQty != 0).ItemDesc+"\n"+txfAccCode.Text;
            int balance = OrderQty;
            lblBin.Text =currentDocs.Find(x=>x.ItemCode==Icode&&x.ItemQty!=0).Bin;
            scanQty = currentDocs.Where(x=>x.ItemCode==Icode).Sum(x=>x.ScanAccQty)+ currentDocs.Where(x => x.ItemCode == Icode).Sum(x => x.ScanRejQty);
            balance = balance - scanQty;      
            lblBalance.Text = balance + "";
            lblScanQTY.Text = scanQty + "";
            lblOrderQTY.Text = OrderQty + "";
            if (balance==0)
            {
                PicImage.ImageSource = "Tick.png";
                PicImage.Text = "Complete, Save Line";
                PicImage.TextColor = Color.Green;
                wrong = false;
            }
            else if (balance != OrderQty&&balance>0)
            {
                PicImage.ImageSource = "PLus.png";
                PicImage.Text = "In-Complete, Save Line";
                PicImage.TextColor = Color.Green;
                wrong = false;
            }
            else if (scanQty > OrderQty|| balance < 0)
            {
                PicImage.ImageSource = "Wrong.png";
                PicImage.Text = "Incorrect Quantity";
                PicImage.TextColor = Color.Red;
                wrong = true;
            }
            else
            {
                PicImage.ImageSource = "Wrong.png";
                wrong = true;
            }
            txfAccCode.Focus();
        }       
        private async void PicImage_Clicked(object sender, EventArgs e)
        {
            if (wrong)
            {
                string output = await DisplayActionSheet("Confirm:- Reset QTY to zero?", "YES", "NO");
                switch (output)
                {
                    case "NO":
                        break;
                    case "YES":
                        await ResetItem();
                        break;
                }
            }
            else {
                await Navigation.PushAsync(new ViewStock(UsingDoc.DocNum.ToUpper()));
            }                    
        }
        public async Task<bool> restetQty(DocLine d)
        {
            List<DocLine> dls = await App.Database.GetSpecificDocsAsync(d.DocNum);
            foreach (DocLine docline in dls.Where(x => x.ItemCode == d.ItemCode && x.ScanAccQty > 0 && !x.GRN))
            {
                await App.Database.Delete(docline);
            }
            List<DocLine> Orig = (await App.Database.GetSpecificDocsAsync(d.DocNum)).Where(x => x.ItemQty != 0 && x.ItemCode == d.ItemCode).ToList();
            Orig.ForEach(a => a.Balacnce = 0);
            Orig.ForEach(a => a.ScanRejQty = 0);
            Orig.ForEach(a => a.ScanAccQty = 0);
            await App.Database.Update(Orig.FirstOrDefault());
            return true;
        }
        private async Task<bool> ResetItem()
        {
            DocLine doc = (await App.Database.GetSpecificDocsAsync(UsingDoc.DocNum)).Where(x => x.ItemCode == lastItem).FirstOrDefault();
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
                row["PalletNumber"] = 0;
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
                        await restetQty(doc);
                        lblBalance.Text = "0";
                        lblScanQTY.Text = "";
                        lblOrderQTY.Text = "";
                        lblitemDescAcc.Text = "";
                        PicImage.IsVisible = false;
                        txfAccCode.Focus();
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
        private async void ToolbarItem_Clicked(object sender, EventArgs e)
        {
            //message.DisplayMessage("Saving....", false);
            //if (await SaveData())
            //{
            //    message.DisplayMessage("All data Saved", true);
            //    await Navigation.PopAsync();
            //}
            //else
            //{
            //    Vibration.Vibrate();
            //    message.DisplayMessage("Error!!! Could Not Save!", true);
            //}
            await Navigation.PushAsync(new ViewStock(UsingDoc.DocNum.ToUpper()));
        }
        private async Task<bool> RemoveAllOld(string docNum)
        {
            try
            {
                foreach (DocLine dl in (await App.Database.GetSpecificDocsAsync(docNum)))
                {
                    await App.Database.Delete(dl);
                }
            }
            catch
            {

            }
            return true;
        }
		private async void txfAccCode_Completed(object sender, EventArgs e)
		{               
            //BOM Barcode
            if (txfAccCode.Text.Length > 0)
            {
                EditsMade = true;
                //txfAccCode.Text=MainPage.CalculateCheckDigit(txfAccCode.Text);
                lblBarCode.Text = txfAccCode.Text;
                if (txfAccCode.Text.Length != 13 && txfAccCode.Text.Length != 12)
                {

                    BOMItem bi;
                    try
                    {
                        bi = await App.Database.GetBOMItem(txfAccCode.Text);
                    }
                    catch
                    {
                        Vibration.Vibrate();
                        message.DisplayMessage("There is no item with this barcode", true);
                        txfAccCode.Text = "";
                        return;
                    }
                    if (Check(bi.ItemCode))
                    {
                        await App.Database.Insert(new DocLine { ItemCode = bi.ItemCode, ScanAccQty = bi.Qty, GRN = false, ScanRejQty = 0, DocNum = UsingDoc.DocNum, WarehouseID = MainPage.ACCWH, isRejected = false });
                        PicImage.IsVisible = true;
                        lastItem = bi.ItemCode;
                        SetQtyDisplay(lastItem);
                        txfAccCode.Text = "";
                        btnEntry.IsVisible = false;
                    }
                    else
                    {
                        Vibration.Vibrate();
                        message.DisplayMessage("There is no item on this PO with this code", true);
                        PicImage.IsVisible = true;
                        PicImage.ImageSource = "Wrong.png";
                        txfAccCode.Text = "";
                    }
                }
                //item barcode
                else if (txfAccCode.Text.Length == 13 || txfAccCode.Text.Length == 12)
                {
                    if (CheckBarcode(txfAccCode.Text))
                    {
                        string iCode = currentDocs.Find(x => x.ItemBarcode == txfAccCode.Text && x.ItemQty != 0).ItemCode;
                        await App.Database.Insert(new DocLine { ItemCode = iCode, ScanAccQty = 1, ScanRejQty = 0, GRN = false, DocNum = UsingDoc.DocNum, WarehouseID = MainPage.ACCWH, isRejected = false });
                        PicImage.IsVisible = true;
                        lastItem = iCode;
                        SetQtyDisplay(iCode);
                        txfAccCode.Text = "";
                        btnEntry.IsVisible = true;
                    }
                    else
                    {
                        Vibration.Vibrate();
                        message.DisplayMessage("There is no item on this PO with this code", true);
                        PicImage.IsVisible = true;
                        PicImage.ImageSource = "Wrong.png";
                        txfAccCode.Text = "";
                    }
                }
            }

            txfAccCode.Focus();
        }
		private async void btnEntry_Clicked(object sender, EventArgs e)
		{
			if (lblBarCode.Text.Length==13 || lblBarCode.Text.Length == 12)
			{   
                if (lblBalance.Text.ToString() != "") {
                    if (Convert.ToInt32(lblBalance.Text) > 0)
                    {
                        string result = await DisplayPromptAsync("Enter Amount!", "Enter the amount of item to add", "OK", "Cancel", null, keyboard: Keyboard.Numeric);
                        if (Convert.ToInt32(result) > Convert.ToInt32(lblBalance.Text))
                        {
                            await DisplayAlert("Error!", "You cannot scan in more items than needed", "OK");
                        }
                        else
                        {
                            string code = lblBarCode.Text;
                            string iCode = currentDocs.Find(x => x.ItemBarcode == code && x.ItemQty != 0).ItemCode;
                            await App.Database.Insert(new DocLine { ItemCode = iCode, ScanAccQty = Convert.ToInt32(result), GRN = false, ScanRejQty = 0, DocNum = UsingDoc.DocNum, WarehouseID = MainPage.ACCWH, isRejected = false });
                            PicImage.IsVisible = true;
                            lastItem = iCode;
                            SetQtyDisplay(iCode);
                            txfAccCode.Text = "";
                        }
                    }
                    else {
                        await DisplayAlert("Error!", "No more to receive", "OK");
                    }
                }
            }          
        }
	}
}