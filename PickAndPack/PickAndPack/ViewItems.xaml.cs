using Data.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoodsRecieveingApp;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using RestSharp;
using System.Threading;
using System.Data;
using Data.Message;

namespace PickAndPack
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ViewItems : ContentPage
    {
        string docCode = "";
        IMessage message = DependencyService.Get<IMessage>();
        DeviceConfig config = new DeviceConfig();
        public ViewItems(string dl)
        {
            InitializeComponent();
            docCode = dl;
            PopData();
        }
        public async void PopData()
        {
            lstItems.ItemsSource = null;
            List<DocLine> lines = await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(docCode);
            List<DocLine> list = new List<DocLine>();
            foreach (string s in lines.Select(x => x.ItemCode).Distinct())
            {
                foreach (int i in lines.Where(x=>x.ItemCode==s).Select(x => x.PalletNum).Distinct())
                {
                    int Pall = lines.Where(x => x.ItemCode == s && x.PalletNum == i).Select(x => x.PalletNum).FirstOrDefault();
                    string itemdesc = lines.Where(x => x.ItemCode == s && x.PalletNum == i).Select(x=>x.ItemDesc).FirstOrDefault();
                    DocLine TempDoc = new DocLine() {PalletNum= Pall, ItemDesc= itemdesc };
                    TempDoc.ScanAccQty = (lines.Where(x => x.ItemCode == s && x.PalletNum == i).Sum(x => x.ScanAccQty));
                    TempDoc.ItemQty = (lines.Where(x => x.ItemCode == s).First().ItemQty);
                    TempDoc.Balacnce = TempDoc.ItemQty - TempDoc.ScanAccQty;
					if (i==0)
					{
                        TempDoc.Complete = "Orig";
                    }
					else
					{
                        if (TempDoc.Balacnce == 0)
                        {
                            TempDoc.Complete = "Yes";
                        }
                        else if (TempDoc.ScanAccQty == 0)
                        {
                            TempDoc.Complete = "NotStarted";
                        }
                        else
                        {
                            TempDoc.Complete = "No";
                        }
                    }
                  
                    list.Add(TempDoc);
                }               
            }
            //list.RemoveAll(x => x.ItemCode.Length < 2);
            //lstItems.ItemsSource = list.OrderBy(x => new { x.ItemDesc , x.PalletNum } );
            lstItems.ItemsSource = list;
        }
        private async void LstItems_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            DocLine dl = e.SelectedItem as DocLine;
            string output = await DisplayActionSheet("Confirm:- Reset QTY to zero?", "YES", "NO");
            switch (output)
            {
                case "NO":
                    break;
                case "YES":
                    if (await restetQty(dl))
                    {
                        PopData();
                    }
                    break;
            }
        }
        public async Task<bool> restetQty(DocLine d)
        {
            List<DocLine> dls = await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(d.DocNum);
            foreach (DocLine docline in dls.Where(x => x.ItemCode == d.ItemCode && x.ItemQty == 0))
            {
                await GoodsRecieveingApp.App.Database.Delete(docline);
            }
            return true;
        }
        private async Task SQLSetComplete()
        {
            RestClient client = new RestClient();
            string path = "DocumentSQLConnection";
            client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
            {
                string str = $"POST?qry=UPDATE tblTempDocHeader SET Complete=1 WHERE DocNum='" + docCode + "'";
                var Request = new RestRequest(str, Method.POST);
                var cancellationTokenSource = new CancellationTokenSource();
                var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                if (res.IsSuccessful && res.Content.Contains("Complete"))
                {
                    await DisplayAlert("Complete!","The order has been sent for approval","OK");
                    Navigation.RemovePage(Navigation.NavigationStack[Navigation.NavigationStack.Count()-1]);
                    await Navigation.PopAsync();
                }
            }
        }
        private async void BtnComplete_Clicked(object sender, EventArgs e)
        {
            if (await Check())
            {
                await SQLSetComplete();
                //if (await InvModule()) { 
                //    if (!await SendToPastel())//Add Check or mast just say that must do on other screeen ?
                //    await DisplayAlert("Error!", "Could not send data to pastel", "OK");
                //}
            }
            else
            {
                await DisplayAlert("Error!", "There is an error in the order, Please make sure all items are green", "OK");
            }
        }
        private async Task<bool> Check()
        {
            List<DocLine> lines = await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(docCode);
            foreach (DocLine dc in lines.Where(x => x.PalletNum == 0))
            {
                foreach (DocLine dl in lines.Where(x => x.PalletNum != 0))
                {
                    if (dc.ItemCode == dl.ItemCode)
                    {
                        dc.Balacnce += dl.ScanAccQty;
                    }
                }
                if (dc.Balacnce != dc.ItemQty)
                {
                    return false;
                }
            }
            return true;
        }
        private async Task<bool> SendToPastel()
        {
            btnComplete.IsEnabled = false;
            message.DisplayMessage("Saving information!", true);
            List<DocLine> docs = await GoodsRecieveingApp.App.Database.GetSpecificDocsAsync(docCode);
            DataTable det = await GetDocDetails(docCode);
            if (det == null)
            {
                return false;
            }
            string docL = CreateDocLines(docs, det);//33.5|6|60|69|PCE|15|3|0|1215|8 Lt clear locked storage box|4|001%2342.5|6|77.6|89.24|PCE|15|3|0|1216|13 Lt clear locked storage box|4|001%2358|12|108.1|124.32|PCE|15|3|0|1217|20 Lt clear locked storage box|4|001%23101|8|170|195.5|PCE|15|3|0|6716000084401|Filo Laundry Hamper Romantic Ivory|4|001%23
            string docH = CreateDocHeader(det);//||Y|TAO01|29/06/2020|IO170852|N|0||||Take a Lot  JHB Distrubution|Cnr Riverfields Boulevard &|First Road, Witfontein Ext 54|Kempton Park,Johannesburg 1619|||||27/09/2019||||1
            if (docL == "" || docH == "")
                return false;
            RestClient client = new RestClient();
            string path = "AddDocument";
            client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
            {
                string str = $"GET?DocHead={docH}&Docline={docL}&DocType=103";
                var Request = new RestRequest(str, Method.POST);
                var cancellationTokenSource = new CancellationTokenSource();
                var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                //System.IndexOutOfRangeException: Subscript out of range
                //at PasSDK._PastelPartnerSDK.DefineDocumentHeader(String Data, Boolean& AdditionalCostInvoice)
                //at FDBWebAPI.Controllers.AddDocumentController.AddDocument(String DocHead, String Docline, String DocType) in E:\\GithubRepos\\FDB\\FirstDutchWebServiceAPI\\FDBWebAPI\\Controllers\\AddDocumentController.cs:line 38"
                if (res.IsSuccessful && res.Content.Contains("0"))
                {
                    await DisplayAlert("Complete!", "Invoice "+res.Content.Split('|')[1] + " successfully generated in Pastel", "OK");
                    return true;
				}
				else if(res.IsSuccessful &&res.Content.Contains("99"))
				{
                    await DisplayAlert("Error!", "Your document could not be sent due to "+Environment.NewLine+res.Content, "OK");
                    return true;
                }
            }
            return false;
        }
        string CreateDocLines(List<DocLine> d,DataTable det)
        {       
            string s = "";
            foreach (string CurrItem in d.Where(x=>x.PalletNum==0).Select(x=>x.ItemCode).Distinct())
            {
                DataRow CurrentRow = det.Select($"ItemCode='{CurrItem}'").FirstOrDefault();
                if (CurrentRow != null)
                    s += $"{CurrentRow["CostPrice"].ToString()}|{CurrentRow["ItemQty"].ToString()}|{CurrentRow["ExVat"].ToString()}|{CurrentRow["InclVat"].ToString()}|{CurrentRow["Unit"].ToString()}|{CurrentRow["TaxType"].ToString()}|{CurrentRow["DiscType"].ToString()}|{CurrentRow["DiscPerc"].ToString()}|{CurrentRow["ItemCode"].ToString()}|{CurrentRow["ItemDesc"].ToString()}|4||{GoodsRecieveingApp.MainPage.ACCWH}%23";
                        //                                 285 | 1                                | 350.88                         | 400.00                           | EACH                          | 01                               |                                   |                                   | ACC /                             |                       Description |4|001             
            }
            return s;
        }
        string CreateDocHeader(DataTable det)
        {         
            DataRow CurrentRow = det.Rows[0];
            string ret = $"||Y|{CurrentRow["CustomerCode"].ToString()}|{DateTime.Now.ToString("dd/MM/yyyy")}|{CurrentRow["OrderNumber"].ToString()}|N|0|{CurrentRow["Message_1"].ToString()}|{CurrentRow["Message_2"].ToString()}|{CurrentRow["Message_3"].ToString()}|{CurrentRow["Address1"].ToString()}|{CurrentRow["Address2"].ToString()}|{CurrentRow["Address3"].ToString()}|{CurrentRow["Address4"].ToString()}|||{CurrentRow["SalesmanCode"].ToString()}||{Convert.ToDateTime(CurrentRow["Due_Date"]).ToString("dd/MM/yyyy")}||||1";
            return ret.Replace('&', '+').Replace('\'', ' ');
                   //||Y|ACK001                                 |05/03/1999                           |                                      |N|0|Message no.1                        |Message no.2                        |Message no.3                        |Delivery no.1                      |Delivery no.2                      |Delivery no.3                      |Delivery no.4                      |||00                                     ||05/03/1999                                                         |011-7402156|Johnny|011-7402157|1
        }
        async Task<DataTable> GetDocDetails(string DocNum)
        {//https://manifoldsa.co.za/FDBAPI/api/GetFullDocDetails/GET?qrystr=ACCHISTL|6|IO170852|102
            RestClient client = new RestClient();
            string path = "GetFullDocDetails";
            client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
            {
                string str = $"GET?qrystr=ACCHISTL|6|{DocNum}|102";
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
        async Task<string> GetGlCode(string itemCode, string WHCode)
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
                    str = $"GET?qrystr=ACCGRP|0|{res.Content.Replace('"', ' ').Replace('\\', ' ').Trim().Split('|')[1]}|5";
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
        private async Task<bool> InvModule()
        {
            config = await GoodsRecieveingApp.App.Database.GetConfig();
            return config.GRVActive;
        }
    }
}