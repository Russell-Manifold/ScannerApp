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
            string docL = CreateDocLines(docs, det);
            string docH = CreateDocHeader(det);
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

                /////!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                //Change 1

                /////when document import is complete 
                /////if config.DeleteSOLines
                /////Call "Edit document controller, loop through all lines of the Sales order and delete lines. (this is available in the Edit Document controller and SDK edit Document.
                /////THIS CAN ONLY HAPPEN WHEN RECIVING OF THE ENTIRE DOCUMENT IS COMPLETE - DO NOT DELETE THE LINES ON PART RECEIVING.

               
                
                //Change 2
                // need to send user id with each document (Currently in the API we are sending user 0 for all transactions)
                // we will ammend the API to suit, so the relevant user code is sent with the document and the user code is then sent to the SDK.

                //Receiving we send:                            config.ReceiveUser
                //Pick Pack /Invoicing we send:            config.InvoiceUser
                //Warehouse Transfer we send:            configWhTrfUser


                /////!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!


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
            string s = "", txtype = "00";
            foreach (string CurrItem in d.Where(x=>x.PalletNum==0).Select(x=>x.ItemCode).Distinct())
            {
                DataRow CurrentRow = det.Select($"ItemCode='{CurrItem}'").FirstOrDefault();
                if (CurrentRow["TaxType"].ToString().Length == 1)
                {
                    txtype = "0" + CurrentRow["TaxType"].ToString();
                }
                else
                {
                    txtype = CurrentRow["TaxType"].ToString();
                }
                if (CurrentRow != null)
                    s += $"{CurrentRow["CostPrice"].ToString()}|{CurrentRow["ItemQty"].ToString()}|{CurrentRow["ExVat"].ToString()}|{txtype}|{CurrentRow["Unit"].ToString()}|{CurrentRow["TaxType"].ToString()}|{CurrentRow["DiscType"].ToString()}|{CurrentRow["DiscPerc"].ToString()}|{CurrentRow["ItemCode"].ToString().PadRight(15, ' ')}|{CurrentRow["ItemDesc"].ToString().PadRight(40, ' ')}|4||{GoodsRecieveingApp.MainPage.ACCWH}%23";
                        //                                 285 | 1                                | 350.88                         | 400.00                           | EACH                          | 01                               |                                   |                                   | ACC /                             |                       Description |4|001             
            }
            return s;
        }
        string CreateDocHeader(DataTable det)
        {         
            DataRow CurrentRow = det.Rows[0];
            string ret = $"||N|{CurrentRow["CustomerCode"].ToString()}|{DateTime.Now.ToString("dd/MM/yyyy")}|{CurrentRow["OrderNumber"].ToString()}|N|0|{CurrentRow["Message_1"].ToString()}|{CurrentRow["Message_2"].ToString()}|{CurrentRow["Message_3"].ToString()}|{CurrentRow["Address1"].ToString()}|{CurrentRow["Address2"].ToString()}|{CurrentRow["Address3"].ToString()}|{CurrentRow["Address4"].ToString()}|||{CurrentRow["SalesmanCode"].ToString()}||{Convert.ToDateTime(CurrentRow["Due_Date"]).ToString("dd/MM/yyyy")}||||1";
            return ret.Replace('&', '+').Replace('\'', ' ');
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