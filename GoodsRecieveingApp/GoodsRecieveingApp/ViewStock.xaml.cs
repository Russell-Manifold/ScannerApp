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

namespace GoodsRecieveingApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ViewStock : ContentPage
    {
        string docCode ="";
        IMessage message = DependencyService.Get<IMessage>();
        DeviceConfig config = new DeviceConfig();
        bool isSaving = false;
        public ViewStock(string dl)
        {
            InitializeComponent();
            docCode = dl;
            PopData();
        }
        public async void PopData()
        {
			try
			{
                config = await GoodsRecieveingApp.App.Database.GetConfig();
			}
			catch
			{
			}
            lstItems.ItemsSource = null;
            List<DocLine> lines = await App.Database.GetSpecificDocsAsync(docCode);
            List<DocLine> list = new List<DocLine>();
            foreach (DocLine dc in lines.Where(x=>x.ItemQty!=0))
            {
                dc.ScanAccQty = lines.Where(c=>c.ItemCode==dc.ItemCode).Sum(x=>x.ScanAccQty);
                dc.ScanRejQty = lines.Where(c => c.ItemCode == dc.ItemCode).Sum(x => x.ScanRejQty);
                dc.Balacnce = dc.ItemQty - (dc.ScanAccQty + dc.ScanRejQty);
                    if (dc.Balacnce == 0 && dc.ItemQty != 0)
                    {
                        dc.Complete = "Yes";
                    }
                    else if (dc.Balacnce == dc.ItemQty)
                    {
                        dc.Complete = "NotStarted";
                    }
                    else if (dc.Balacnce<0)
                    {
                        dc.Complete = "Wrong";
                    }
                    else
                    {
                        dc.Complete = "No";
                    }
                list.Add(dc);                
            }
            list.RemoveAll(x => x.ItemCode.Length < 2);
            lstItems.ItemsSource = list.OrderBy(x=>x.ItemDesc);
        }
        private async void LstItems_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            DocLine dl=e.SelectedItem as DocLine;
            string output =await DisplayActionSheet("Confirm:-Reset QTY to zero?","YES", "NO");
            switch (output)
            {
                case "NO":
                    break;
                case "YES":
                    if (await ResetItem(dl))
                    {
                        if (await restetQty(dl))
                        {
                            PopData();
                        }                      
                    }                                                            
                    break;
            }
        }
        public async Task<bool> restetQty(DocLine d)
        {
            List<DocLine> dls = await App.Database.GetSpecificDocsAsync(d.DocNum);
            foreach (DocLine docline in dls.Where(x => x.ItemCode == d.ItemCode && x.ItemQty == 0&&!x.GRN))
            {
                await App.Database.Delete(docline);
            }
            List<DocLine> Orig = (await App.Database.GetSpecificDocsAsync(d.DocNum)).Where(x => x.ItemQty != 0&& x.ItemCode == d.ItemCode).ToList();
            Orig.ForEach(a=>a.Balacnce=0);
            Orig.ForEach(a=>a.ScanRejQty=0);
            Orig.ForEach(a=>a.ScanAccQty=0);
            await App.Database.Update(Orig.FirstOrDefault());
            return true;
        }
        private async Task InsertUser(string user)
        {
            RestSharp.RestClient client = new RestSharp.RestClient();
            client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath);
            {
                var Request = new RestSharp.RestRequest("DocumentSQLConnection", RestSharp.Method.POST);
                Request.Resource = "UPDATE tblTempDocHeader SET AuthUser='" + user + "' WHERE DocNum='" + docCode + "'";
                var cancellationTokenSource = new CancellationTokenSource();
                var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
            }
        }
        private async void BtnComplete_Clicked(object sender, EventArgs e)
        {
            btnComplete.IsEnabled = false;
            message.DisplayMessage("Saving information!", true);
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                if (await Check())
                {
                    string uName = "";
                    while (uName == "" || uName == null)
                    {
                        uName = await DisplayPromptAsync("Scan user barcode", "Scan Auth barcode", "OK");
                    }
                    await InsertUser(uName);
                    if (!await SendToPastel())
                    {
                        Vibration.Vibrate();
                        message.DisplayMessage("Error! - Could not Complete", true);
                    }
                    else
                    {
                        if (await SaveData())
                        {
                            if (Navigation.NavigationStack.Count == 4)
                            {
                                Navigation.RemovePage(Navigation.NavigationStack[2]);
                            }
                            else
                            {
                                Navigation.RemovePage(Navigation.NavigationStack[2]);
                                Navigation.RemovePage(Navigation.NavigationStack[3]);
                                await Navigation.PopAsync();
                            }
                            message.DisplayMessage("Complete!!!", true);
                        }
                        else
                        {
                            message.DisplayMessage("Could not save to database", true);
                        }
                    }
                }
                else
                {
                    if (await CanPartRecieve())
                    {
                        string res = await DisplayActionSheet("Confirm - Part recieve the items you have scanned?", "YES", "NO");
                        if (res == "YES")
                        {
                            await MarkAndComplete();
                        }
                        else
                        {
                            Vibration.Vibrate();
                            message.DisplayMessage("Please make sure all items are GREEN!", true);
                        }
                    }
                    else
                    {
                        Vibration.Vibrate();
                        message.DisplayMessage("You have no new items scanned!", true);
                    }
                }
            }
            else
            {
                Vibration.Vibrate();
                message.DisplayMessage("No Internet connection", true);
            }
        }
        private async Task MarkAndComplete()
        {
            if (await SavePart())
            {
                await MarAsRecieved();
                if (await SaveData())
                {
                    //await DisplayAlert("Complete!", "All the parts have been saved", "OK");
                    if (Navigation.NavigationStack.Count == 4)
                    {
                        Navigation.RemovePage(Navigation.NavigationStack[2]);
                    }
                    else
                    {
                        Navigation.RemovePage(Navigation.NavigationStack[2]);
                        Navigation.RemovePage(Navigation.NavigationStack[3]);
                        await Navigation.PopAsync();
                    }
                }
                else
                {
                    Vibration.Vibrate();
                    message.DisplayMessage("Error!!! Could Not Save data in database!", true);
                }
            }
        }
        async Task MarAsRecieved()
        {
            List<DocLine> lines = (await App.Database.GetSpecificDocsAsync(docCode)).Where(x => !x.GRN).ToList();
            foreach (string itemCode in lines.Select(x => x.ItemCode).Distinct())
            {
                if (lines.Where(x => x.ItemCode == itemCode && (x.ScanAccQty > 0 || x.ScanRejQty > 0)).Count() > 0)
                {
                    DocLine GRVItem = (await App.Database.GetSpecificDocsAsync(docCode)).Where(x => x.GRN && x.ItemCode == itemCode).FirstOrDefault();
                    if (GRVItem != null)
                    {
                        GRVItem.ScanAccQty = lines.Where(x => x.ItemCode == itemCode).Sum(x => x.ScanAccQty);
                        GRVItem.ScanRejQty = lines.Where(x => x.ItemCode == itemCode).Sum(x => x.ScanRejQty);
                        await App.Database.Update(GRVItem);
                    }
                    else
                    {
                        await App.Database.Insert(new DocLine { DocNum = docCode, ItemCode = itemCode, ItemBarcode = lines.Where(x => x.ItemQty != 0 && x.ItemCode == itemCode).FirstOrDefault().ItemBarcode, Balacnce = 0, ScanAccQty = lines.Where(x => x.ItemCode == itemCode).Sum(x => x.ScanAccQty), ScanRejQty = lines.Where(x => x.ItemCode == itemCode).Sum(x => x.ScanRejQty), PalletNum = lines.Where(x => x.ItemQty != 0 && x.ItemCode == itemCode).FirstOrDefault().PalletNum, GRN = true, ItemQty = 0 });
                    }
                    foreach (DocLine dl in lines.Where(x => x.ItemCode == itemCode))
                    {
                        dl.ScanAccQty = 0;
                        dl.ScanRejQty = 0;
                        await App.Database.Update(dl);
                    }
                    foreach (DocLine docLine in lines.Where(x => x.ItemQty == 0 && x.ScanAccQty == 0 && x.ScanRejQty == 0))
                    {
                        await App.Database.Delete(docLine);
                    }
                }
            }
        }
        async Task<bool> CanPartRecieve()
        {
            List<DocLine> lines = await App.Database.GetSpecificDocsAsync(docCode);
            foreach (DocLine docLine in lines)
            {
                if (!docLine.GRN && (docLine.ScanAccQty > 0 || docLine.ScanRejQty > 0))
                {
                    return true;
                }
            }
            return false;
        }
        async Task<string> CreatePartDocLines(List<DocLine> d)
        {
            string thisline = "";
            List<string> doneItems = new List<string>();
            DataTable det = await GetDocDetails(docCode);
            if (det == null)
            {
                return "";
            }
            string s = "", txtype = "00";
            foreach (DocLine docline in d)
            {
                if (!doneItems.Contains(docline.ItemBarcode))
                {
                    docline.ScanAccQty = d.Where(x => x.ItemBarcode == docline.ItemBarcode && !x.GRN).Sum(x => x.ScanAccQty);
                    docline.ScanRejQty = d.Where(x => x.ItemBarcode == docline.ItemBarcode && !x.GRN).Sum(x => x.ScanRejQty);

                    if (docline.ScanAccQty > 0)
                    {
                        DataRow CurrentRow = det.Select($"ItemCode='{docline.ItemCode}'").FirstOrDefault();
                        if (CurrentRow["TaxType"].ToString().Length == 1)
                        {
                            txtype = "0" + CurrentRow["TaxType"].ToString();
                        }
                        else
                        {
                            txtype = CurrentRow["TaxType"].ToString();
                        }
                        //GLCode = await GetGlCode(docline.ItemCode, MainPage.ACCWH);
                        if (CurrentRow != null)
                            s += $"{(CurrentRow["CostPrice"].ToString()).Replace(',', '.')}|{docline.ScanAccQty}|{CurrentRow["ExVat"].ToString().Replace(',', '.')}|{CurrentRow["InclVat"].ToString().Replace(',', '.')}|{CurrentRow["Unit"].ToString()}|{CurrentRow["TaxType"].ToString()}|{CurrentRow["DiscType"].ToString()}|{CurrentRow["DiscPerc"].ToString().Replace(',', '.')}|{CurrentRow["ItemCode"].ToString().PadRight(15, ' ')}|{CurrentRow["ItemDesc"].ToString().PadRight(40, ' ')}|4|{config.DefaultAccWH}%23";
                            //thisline = "";
                            //thisline = CurrentRow["CostPrice"].ToString() + "|" + docline.ScanAccQty + "|" + CurrentRow["ExVat"].ToString() + "|" + CurrentRow["InclVat"].ToString() + "|" + CurrentRow["Unit"].ToString() + "|" + txtype + "|" + CurrentRow["DiscType"].ToString() + "|" + CurrentRow["DiscPerc"].ToString() + "|" + docline.ItemCode.PadRight(15, ' ') + "|" + CurrentRow["ItemDesc"].ToString().PadRight(40, ' ') + "|" + "4" + "|" + "001" + "|" + CurrentRow["CostCode"].ToString() + "|" + "%23";
                            //s += thisline.ToString();
                        //s += $"{CurrentRow["CostPrice"].ToString()}|{docline.ScanAccQty}|{CurrentRow["ExVat"].ToString()}|{CurrentRow["InclVat"].ToString()}|{CurrentRow["Unit"].ToString()}|{txtype}|{CurrentRow["DiscType"].ToString()}|{CurrentRow["DiscPerc"].ToString()}|{docline.ItemCode.PadRight(15, ' ')}|{CurrentRow["ItemDesc"].ToString().PadRight(40, ' ')}|4|{MainPage.ACCWH}|{CurrentRow["CostCode"].ToString()}%23";
                    }
                    if (docline.ScanRejQty > 0)
                    {
                        DataRow CurrentRow = det.Select($"ItemCode=={docline.ItemCode}").FirstOrDefault();
                        if (CurrentRow["TaxType"].ToString().Length == 1)
                        {
                            txtype = "0" + CurrentRow["TaxType"].ToString();
                        }
                        else
                        {
                            txtype = CurrentRow["TaxType"].ToString();
                        }
                        // GLCode = await GetGlCode(docline.ItemCode, MainPage.REJWH);
                        if (CurrentRow != null)
                            s += $"{(CurrentRow["CostPrice"].ToString()).Replace(',', '.')}|{docline.ScanRejQty}|{CurrentRow["ExVat"].ToString().Replace(',', '.')}|{CurrentRow["InclVat"].ToString().Replace(',', '.')}|{CurrentRow["Unit"].ToString()}|{CurrentRow["TaxType"].ToString()}|{CurrentRow["DiscType"].ToString()}|{CurrentRow["DiscPerc"].ToString().Replace(',', '.')}|{CurrentRow["ItemCode"].ToString().PadRight(15, ' ')}|{CurrentRow["ItemDesc"].ToString().PadRight(40, ' ')}|4|{config.DefaultRejWH}%23";
                        //thisline = "";
                        //thisline = CurrentRow["CostPrice"].ToString() + "|" + docline.ScanAccQty + "|" + CurrentRow["ExVat"].ToString() + "|" + CurrentRow["InclVat"].ToString() + "|" + CurrentRow["Unit"].ToString() + "|" + txtype + "|" + CurrentRow["DiscType"].ToString() + "|" + CurrentRow["DiscPerc"].ToString() + "|" + docline.ItemCode.PadRight(15, ' ') + "|" + CurrentRow["ItemDesc"].ToString().PadRight(40, ' ') + "|" + "4" + "|" + "001" + "|" + CurrentRow["CostCode"].ToString() + "|" + "%23";
                        //s += thisline.ToString();
                        //s += $"{CurrentRow["CostPrice"].ToString()}|{docline.ScanRejQty}|{CurrentRow["ExVat"].ToString()}|{CurrentRow["InclVat"].ToString()}|{CurrentRow["Unit"].ToString()}|{txtype}|{CurrentRow["DiscType"].ToString()}|{CurrentRow["DiscPerc"].ToString()}|{docline.ItemCode.PadRight(15, ' ')}|{CurrentRow["ItemDesc"].ToString().PadRight(40, ' ')}|4|{MainPage.REJWH}|{CurrentRow["CostCode"].ToString()}%23";
                    }
                    doneItems.Add(docline.ItemBarcode);
                }
            }
            return s.Replace(',','.');
        }
        async Task<DataTable> GetDocDetails(string DocNum)
        {//http://192.168.0.100/FDBAPI/api/GetFullDocDetails/GET?qrystr=ACCHISTL|6|PO100330|106
            RestClient client = new RestClient();
            string path = "GetFullDocDetails";
            //string path = "GetDocument";
            client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
            {
                string str = $"GET?qrystr=ACCHISTL|6|{DocNum}|106";
                var Request = new RestRequest(str, Method.GET);
                var cancellationTokenSource = new CancellationTokenSource();
                var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                if (res.IsSuccessful && res.Content.Contains("0"))
                {
                    DataSet myds = new DataSet();
                    myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                    return myds.Tables[0];
                }
            }
            return null;
        }
        private async Task<bool> SavePart()
        {
            List<DocLine> docs = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(docCode)).Where(x => !x.GRN).ToList();
            string docL = await CreatePartDocLines(docs);
            string docH = await CreateDocHeader();
            string doctype = "107";
            if (docL == "" || docH == "")
                return false;
            RestClient client = new RestClient();
            string path = "AddDocument";
            client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
            {
				if (config.ReceiveUser==""|| config.ReceiveUser==null)
				{
                    config.ReceiveUser = "0";

                }
                string str = $"GET?DocHead={docH}&Docline={docL}&DocType={doctype}&Userid={config.ReceiveUser}"; 
                var Request = new RestRequest(str, Method.POST);
                var cancellationTokenSource = new CancellationTokenSource();
                var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                if (res.IsSuccessful && res.Content.Contains("0"))
                {
                    await DisplayAlert("Complete!", "Request sent to pastel." + Environment.NewLine + res.Content.Split('|')[1].ToString().Substring(0, res.Content.Split('|')[1].Length - 1) + " successfully created", "OK");
                    return true;
                }
            }
            return false;
        }
        async Task<string> CreateDocLines(List<DocLine> d)
        {
            string thisline = "";
            List<string> doneItems = new List<string>();
            DataTable det = await GetDocDetails(docCode);
            if (det==null)
            {
                return "";
            }
            string s = "", txtype = "00";
            foreach (DocLine docline in d)
            {
                if (!doneItems.Contains(docline.ItemBarcode))
                {
                    docline.ScanAccQty = d.Where(x => x.ItemBarcode == docline.ItemBarcode && !x.GRN).Sum(x => x.ScanAccQty);
                    docline.ScanRejQty = d.Where(x => x.ItemBarcode == docline.ItemBarcode && !x.GRN).Sum(x => x.ScanRejQty);
                    if (docline.ScanAccQty > 0)
                    {
                        DataRow CurrentRow = det.Select($"ItemCode='{docline.ItemCode}'").FirstOrDefault();
                        // set tax type to 2 chars  
                        if (CurrentRow["TaxType"].ToString().Length == 1)
                        {
                            txtype = "0" + CurrentRow["TaxType"].ToString();
                        }
                        else
                        {
                            txtype = CurrentRow["TaxType"].ToString();
                        }
                        if (CurrentRow != null)
                            s += $"{(CurrentRow["CostPrice"].ToString()).Replace(',', '.')}|{docline.ScanAccQty}|{CurrentRow["ExVat"].ToString().Replace(',', '.')}|{CurrentRow["InclVat"].ToString().Replace(',', '.')}|{CurrentRow["Unit"].ToString()}|{CurrentRow["TaxType"].ToString()}|{CurrentRow["DiscType"].ToString()}|{CurrentRow["DiscPerc"].ToString().Replace(',', '.')}|{CurrentRow["ItemCode"].ToString().PadRight(15, ' ')}|{CurrentRow["ItemDesc"].ToString().PadRight(40, ' ')}|4|{config.DefaultAccWH}%23";
                        //thisline = "";
                        //thisline = CurrentRow["CostPrice"].ToString() + "|" + docline.ScanAccQty + "|" + CurrentRow["ExVat"].ToString() + "|" + CurrentRow["InclVat"].ToString() + "|" + CurrentRow["Unit"].ToString() + "|" + txtype + "|" + CurrentRow["DiscType"].ToString() + "|" + CurrentRow["DiscPerc"].ToString() + "|" + docline.ItemCode.PadRight(15, ' ') + "|" + CurrentRow["ItemDesc"].ToString().PadRight(40, ' ') + "|" + "4" + "|" + "001" + "|" + CurrentRow["CostCode"].ToString() + "|" + "%23";
                        //s += thisline.ToString();
                    }
                    if (docline.ScanRejQty > 0)
                    {                       
                        DataRow CurrentRow = det.Select($"ItemCode=={docline.ItemCode}").FirstOrDefault();
                        if (CurrentRow["TaxType"].ToString().Length == 1)
                        {
                            txtype = "0" + CurrentRow["TaxType"].ToString();
                        }
                        else
                        {
                            txtype = CurrentRow["TaxType"].ToString();
                        }
                        if (CurrentRow != null)
                            s += $"{(CurrentRow["CostPrice"].ToString()).Replace(',', '.')}|{docline.ScanRejQty}|{CurrentRow["ExVat"].ToString().Replace(',', '.')}|{CurrentRow["InclVat"].ToString().Replace(',', '.')}|{CurrentRow["Unit"].ToString()}|{CurrentRow["TaxType"].ToString()}|{CurrentRow["DiscType"].ToString()}|{CurrentRow["DiscPerc"].ToString().Replace(',', '.')}|{CurrentRow["ItemCode"].ToString().PadRight(15, ' ')}|{CurrentRow["ItemDesc"].ToString().PadRight(40, ' ')}|4|{config.DefaultRejWH}%23";
                        //thisline = "";
                        //thisline = CurrentRow["CostPrice"].ToString() + "|" + docline.ScanAccQty + "|" + CurrentRow["ExVat"].ToString() + "|" + CurrentRow["InclVat"].ToString() + "|" + CurrentRow["Unit"].ToString() + "|" + txtype + "|" + CurrentRow["DiscType"].ToString() + "|" + CurrentRow["DiscPerc"].ToString() + "|" + docline.ItemCode.PadRight(15, ' ') + "|" + CurrentRow["ItemDesc"].ToString().PadRight(40, ' ') + "|" + "4" + "|" + "001" + "|" + CurrentRow["CostCode"].ToString() + "|" + "%23";
                        //s += thisline.ToString();
                        //s += $"{CurrentRow["CostPrice"].ToString()}|{docline.ScanRejQty}|{CurrentRow["ExVat"].ToString()}|{CurrentRow["InclVat"].ToString()}|{CurrentRow["Unit"].ToString()}|{txtype}|{CurrentRow["DiscType"].ToString()}|{CurrentRow["DiscPerc"].ToString()}|{docline.ItemCode.PadRight(15, ' ')}|{CurrentRow["ItemDesc"].ToString().PadRight(40, ' ')}|4|{MainPage.REJWH}|{CurrentRow["CostCode"].ToString()}%23";                 
                    }
                    doneItems.Add(docline.ItemBarcode);

                }              
            }
            //old code
            //foreach (string CODE in d.Select(x=>x.ItemCode).Distinct())
            //{
            //    foreach (string WH in d.Where(x=>x.ItemCode==CODE && (x.ScanAccQty!=0||x.ScanRejQty!=0)).Select(x => x.WarehouseID).Distinct())
            //    {            
            //        var CurrentRow =(from dr in det.Select()where(string)dr["ItemCode"]==CODE select dr).FirstOrDefault() ;
            //        GLCode = await GetGlCode(CODE, WH);
            //        if (CurrentRow!=null)
            //            s += $"{CurrentRow["CostPrice"].ToString()}|{CurrentRow["ItemQty"].ToString()}|{CurrentRow["ExVat"].ToString()}|{CurrentRow["InclVat"].ToString()}|{CurrentRow["Unit"].ToString()}|{CurrentRow["TaxType"].ToString()}|{CurrentRow["DiscType"].ToString()}|{CurrentRow["DiscPerc"].ToString()}|{GLCode}|{CurrentRow["ItemDesc"].ToString()}|6|{WH}|{CurrentRow["CostCode"].ToString()}%23";
            //    }                  
            //}
            return s.Replace(',','.');
        }
        async Task<string> CreateDocHeader()
        {
            DataTable det = await GetDocDetails(docCode);
            if (det==null)
                return "";        
            DataRow CurrentRow = det.Rows[0];
             return $"||N|{CurrentRow["CustomerCode"].ToString()}|{DateTime.Now.ToString("dd/MM/yyyy")}|{CurrentRow["ExtRef"].ToString()}|N|0|{CurrentRow["Message_1"].ToString()}|{CurrentRow["Message_2"].ToString()}|{CurrentRow["Message_3"].ToString()}|{CurrentRow["Address1"].ToString()}|{CurrentRow["Address2"].ToString()}|{CurrentRow["Address3"].ToString()}|{CurrentRow["Address4"].ToString()}|||{CurrentRow["SalesmanCode"].ToString()}||{Convert.ToDateTime(CurrentRow["Due_Date"]).ToString("dd/MM/yyyy")}||||1 ";
        //40IN0010 | | | HAR001                                              | 06 / 08 / 2020                                     | 20IN00000401                                    |Y| 0.000000 | LA.2020 / 00185                 |                                                      |                                                       |                                                    |                                                    |                                                   |                                                    |||                                                             || 30 / 09 / 2020                                                                                  |||| 0.00 | LA.2020 / 00185 ||||| N", False
        }
        async Task<string> GetGlCode(string itemCode,string WHCode)
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
                    str = $"GET?qrystr=ACCGRP|0|{res.Content.Replace('"', ' ').Replace('\\', ' ').Trim().Split('|')[1]}|7";
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
        private async Task<bool> SendToPastel()
        {
            List<DocLine> docs = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(docCode)).Where(x => !x.GRN).ToList(); 
            string docL = await CreateDocLines(docs);
            string docH = await CreateDocHeader();
            string doctype = "107";
            if (docL == ""||docH=="")
                return false;         
            RestClient client = new RestClient();
            string path = "AddDocument";
            client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
            {
				if (config.ReceiveUser==""|| config.ReceiveUser==null)
				{
                    config.ReceiveUser = "0";

                }
                string str = $"GET?DocHead={docH}&Docline={docL}&DocType={doctype}&Userid={config.ReceiveUser}";
                var Request = new RestRequest(str, Method.POST);
                var cancellationTokenSource = new CancellationTokenSource();
                var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                if (res.IsSuccessful && res.Content.Contains("0"))
                {
                    await DisplayAlert("Complete!", "Request sent to pastel." + Environment.NewLine + res.Content.Split('|')[1].ToString().Substring(0, res.Content.Split('|')[1].Length - 1) + " successfully created", "OK");
                    await MarAsRecieved();                  
                    return true;
                }
            }
            return false;
        }
        private async Task<bool> Check()
        {
            List<DocLine> lines = await App.Database.GetSpecificDocsAsync(docCode);
            foreach (string st in lines.Select(x => x.ItemCode).Distinct())
            {
                if (lines.Where(x => x.ItemCode == st && !x.GRN && x.ItemQty != 0).FirstOrDefault().ItemQty != (lines.Where(x => x.ItemCode == st).Sum(x => x.ScanAccQty) + lines.Where(x => x.ItemCode == st).Sum(x => x.ScanRejQty)))
                {
                    return false;
                }
            }
            if (await IsComplete())
            {
                return false;
            }
            return true;
        }
        private async Task<bool> IsComplete()
        {
            List<DocLine> lins = (await App.Database.GetSpecificDocsAsync(docCode)).Where(x=>!x.GRN).ToList();
            foreach (DocLine d in lins)
            {
                if (d.ScanAccQty>0||d.ScanRejQty>0)
                {
                    return false;
                }
            }
            return true;
        }
        private async void ToolbarItem_Clicked(object sender, EventArgs e)
        {
            if (Navigation.NavigationStack.Count== 4)
            {
                await Navigation.PopAsync();
            }
            else
            {
                Navigation.RemovePage(Navigation.NavigationStack[3]);
                await Navigation.PopAsync();
            }
        }
        private async void btnSave_Clicked(object sender, EventArgs e)
        {
			if (!isSaving)
			{
                isSaving = true;
                message.DisplayMessage("Saving....", false);
                if (await SaveData())
                {
                    message.DisplayMessage("All data Saved", true);
                    if (Navigation.NavigationStack.Count == 4)
                    {
                        Navigation.RemovePage(Navigation.NavigationStack[2]);
                        await Navigation.PopAsync();
                    }
                    else
                    {
                        Navigation.RemovePage(Navigation.NavigationStack[2]);
                        Navigation.RemovePage(Navigation.NavigationStack[3]);
                        await Navigation.PopAsync();
                    }
                }
                else
                {
                    Vibration.Vibrate();
                    message.DisplayMessage("Error!!! Could Not Save!", true);
                }
                isSaving = false;
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
                docs = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(docCode)).Where(x => x.ItemQty == 0 || x.GRN).ToList();
                if (docs.Count == 0)
                    return true;
                foreach (string str in docs.Select(x => x.ItemCode).Distinct())
                {
                    DocLine currentGRV = (await App.Database.GetSpecificDocsAsync(docCode)).Where(x => x.GRN && x.ItemCode == str).FirstOrDefault();
                    if (currentGRV != null && await GRVmodule())
                    {
                        row = t1.NewRow();
                        row["DocNum"] = docCode;
                        row["ItemBarcode"] = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(docCode)).Where(x => x.ItemCode == str && x.ItemQty != 0).FirstOrDefault().ItemBarcode;
                        row["ScanAccQty"] = currentGRV.ScanAccQty;
                        row["Balance"] = 0;
                        row["ScanRejQty"] = currentGRV.ScanRejQty;
                        row["PalletNumber"] = 0;
                        row["GRV"] = true;
                        t1.Rows.Add(row);
                    }
                    else if (currentGRV != null && !await GRVmodule())
                    {
                        await DisplayAlert("Please set up GRV in the settings", "Error", "OK");
                    }
                    List<DocLine> CurrItems = (await App.Database.GetSpecificDocsAsync(docCode)).Where(x => !x.GRN && x.ItemCode == str && x.ItemQty == 0).ToList();
                    if (CurrItems.Count() > 0)
                    {
                        row = t1.NewRow();
                        row["DocNum"] = docs.FirstOrDefault().DocNum;
                        row["ItemBarcode"] = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(docCode)).Where(x => x.ItemCode == str && x.ItemQty != 0).FirstOrDefault().ItemBarcode;
                        row["ScanAccQty"] = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(docCode)).Where(x => x.ItemCode == str && !x.GRN).Sum(x => x.ScanAccQty);
                        row["Balance"] = 0;
                        row["ScanRejQty"] = (await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(docCode)).Where(x => x.ItemCode == str && !x.GRN).Sum(x => x.ScanRejQty);
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
                if (res.IsSuccessful && res.Content.Contains("COMPLETE"))
                {
                    docs = await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(docCode);
                    foreach (DocLine doc in docs)
                    {
                        await ResetItem(doc);
                    }
                    await GoodsRecieveingApp.App.Database.DeleteSpecificDocs(docCode);
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
            catch (Exception ex)
            {
                return false;
            }

        }
    }
}