using Data.KeyboardContol;
using Data.Message;
using Data.Model;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace WHTransfer
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AuthOut : ContentPage
    {
        private ExtendedEntry _currententry;
        IMessage message = DependencyService.Get<IMessage>();
        private string AdminName, WH;
        private int AdminCode,ColID;
        public AuthOut()
        {
            InitializeComponent();
            txfUserCode.Focused +=Entry_Focused;
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            txfUserCode.Focus();
        }
        private async void BtnDone_Clicked(object sender, EventArgs e)
        {
            if (await completeOrder())
            {
                if (await PastelTransferToGit())
                {
                    await GoodsRecieveingApp.App.Database.DeleteAllHeaders();
                    message.DisplayMessage("COMPLETE!", false);
                    Navigation.RemovePage(Navigation.NavigationStack[4]);
                    Navigation.RemovePage(Navigation.NavigationStack[3]);
                    Navigation.RemovePage(Navigation.NavigationStack[2]);
                    await Navigation.PopAsync();
                    return;
                }         
            }
            message.DisplayMessage("There was a error in sending the information", true);
            Vibration.Vibrate();
        }
        private async Task<bool> completeOrder()
        {
            if(await InsertHeader())
            {
                UpdateRecords();
                if (await InsertLines())
                {
                    return true;
                }
            }
            return false;
        }
        private async Task<bool> InsertHeader()
        {
            try
            {
                IBTHeader header = (await GoodsRecieveingApp.App.Database.GetIBTHeaders()).First();
                WH = header.FromWH;
                RestSharp.RestClient client = new RestSharp.RestClient();
                string path = "IBTHeader";
                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                {
                    string str = $"POST?TrfDate={DateTime.Now.ToString("dd MMM yyyy")}&FromWH={header.FromWH}&ToWH={header.ToWH}&FromDate={header.FromDate}&RecDate={header.RecDate}&PickerUser={GoodsRecieveingApp.MainPage.UserCode}&AuthUser={AdminCode}&Active=true";
                    var Request = new RestSharp.RestRequest();
                    Request.Resource = str;
                    Request.Method = RestSharp.Method.POST;
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    if (res.IsSuccessful && res.Content != null)
                    {
                        ColID = Convert.ToInt32(res.Content.Replace('\\', ' ').Replace('"', ' '));
                        await DisplayAlert("Out Complete!", "Transfer OUT Number " + ColID + " Complete", "Continue");
                        return true;
                    }
                }
            }
            catch
            {

            }           
            return false;
        }
        private void UpdateRecords()
        {
            OutItems.items.Select(x=> { x.PickerUser = GoodsRecieveingApp.MainPage.UserCode;x.AuthUser = AdminCode; x.iTrfID=ColID; x.WH=WH; return x;}).ToList();
        }
        private async Task<bool> InsertLines()
        {
            foreach (IBTItem i in OutItems.items)
            {
                try
                {
                    RestSharp.RestClient client = new RestSharp.RestClient();
                    string path = "IBTLines";
                    client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                    {
                        string str = $"POST?ScanBarcode={i.ScanBarcode}&ItemBarcode={i.ItemBarcode}&ItemCode={i.ItemCode}&ItemDesc={i.ItemDesc}&ItemQtyOut={i.ItemQtyOut}&ItemQtyIn={i.ItemQtyIn}&PickerUser={i.PickerUser}&AuthUser={i.AuthUser}&PickDateTime={i.PickDateTime.ToString("dd MMM yyyy")}&WH={i.WH}&iTrfId={i.iTrfID}";
                        var Request = new RestRequest(str, Method.POST);
                        var cancellationTokenSource = new CancellationTokenSource();
                        var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                        if (!(res.IsSuccessful && res.Content.Contains("Complete")))
                        {
                            return false;
                        }
                    }
                }                
                catch
                {                   
                    message.DisplayMessage("Error in sending the lines",true);
                    Vibration.Vibrate();
                    return false;
                }
            }
            return true;
        }
        private async void TxfUserCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            await Task.Delay(100);
            if (txfUserCode.Text.Length>1)
            {
                Loading.IsVisible = true;
                if (txfUserCode.Text!=GoodsRecieveingApp.MainPage.UserName&&await CheckUser(txfUserCode.Text)) 
                { 
                     AdminName = txfUserCode.Text;
                     btnDone.IsVisible = true;
                     Loading.IsVisible = false;
                     return;
                }
                    Loading.IsVisible = false;
                Vibration.Vibrate();
                message.DisplayMessage("Invalid User",true);
                    txfUserCode.Text = "";
                    txfUserCode.Focus();               
            }
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
        private async Task<bool> CheckUser(string usercode)
        {
            try
            {
                RestSharp.RestClient client = new RestSharp.RestClient();
                string path = "GetUser";
                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                {
                    string str = $"GET?UserName={usercode}";
                    var Request = new RestRequest(str, Method.GET);
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    if (res.IsSuccessful && res.Content != null)
                    {
                        DataSet myds = new DataSet();
                        myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                        foreach (DataRow row in myds.Tables[0].Rows)
                        {
                            try
                            {
                                if (Convert.ToBoolean(row["AuthWHTrf"]))
                                {
                                    myds.Dispose();
                                    return true;
                                }
                                else
                                {
                                    myds.Dispose();
                                    return false;
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                            }
                        }
                        myds.Dispose();
                    }
                }
            }
            catch
            {
            }
            return false;
        }
        private async Task<bool> PastelTransferToGit()
        {
            //List<string> DoneItems = new List<string>();
            //foreach (IBTItem i in OutItems.items)
            //{
            //    if (!DoneItems.Contains(i.ItemCode))
            //    {
            //        DoneItems.Add(i.ItemCode);
            //        string JnlAcc = await GetGlCode(i.ItemCode, i.WH);
            //        RestSharp.RestClient client2 = new RestSharp.RestClient();
            //        int k = OutItems.items.Where(x => x.ItemCode == i.ItemCode).Sum(c => c.ItemQtyOut);
            //        string path2 = "WHTransfer";
            //        client2.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path2);
            //        {
            //            string str2 = $"POST?itemCode={i.ItemCode}&JnlAcc={JnlAcc}&JnlDate={DateTime.Now.ToString("dd MMM yyyy")}&JobCode={i.iTrfID}&Desc={i.iTrfID}&Ref={DateTime.Now.ToString("ddMMMyyyy") + "-" + i.iTrfID}&Qty={k}&Store={i.WH}";
            //            var Request2 = new RestSharp.RestRequest();
            //            Request2.Resource = str2;
            //            Request2.Method = RestSharp.Method.POST;
            //            var cancellationTokenSource2 = new CancellationTokenSource();
            //            var res2 = await client2.ExecuteAsync(Request2, cancellationTokenSource2.Token);
            //            if (!(res2.IsSuccessful && res2.Content != null))
            //            {

            //            }
            //        }
            //    }
            //}
            //return false;
            //string dataString = GetItems();
            //if (dataString.Length > 1)
            //{
            //    RestSharp.RestClient client = new RestSharp.RestClient();
            //    string path = "PastelInv";
            //    client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
            //    {
            //        string str = $"POST?";
            //        var Request = new RestSharp.RestRequest(str, RestSharp.Method.POST);
            //        Request.RequestFormat = RestSharp.DataFormat.Json;
            //        Request.AddJsonBody(dataString);
            //        var cancellationTokenSource = new CancellationTokenSource();
            //        var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
            //        if (res.StatusCode.ToString().Contains("OK") && res.Content.Contains("Complete"))
            //        {
            //            return true;
            //        }
            //    }
            //}
            //return false;
            return true;
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
                    str = $"GET?qrystr=ACCGRP|0|{res.Content.Replace('"', ' ').Replace('\\', ' ').Trim().Split('|')[1]}|6";//////////////////////////////////////////////////////////
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
        private string GetItems()
        {
            //DataSet ds = new DataSet();
            //DataTable dt = new DataTable();
            //dt.Columns.Add("Cost");
            //dt.Columns.Add("ItemCode");
            //dt.Columns.Add("AccountGL");
            //dt.Columns.Add("JnlDate");
            //dt.Columns.Add("JobCode");
            //dt.Columns.Add("Narration");
            //dt.Columns.Add("Reference");
            //dt.Columns.Add("QtyAdjust");
            //dt.Columns.Add("Store");
            //foreach (IBTItem item in lines)
            //{
            //    DataRow drAdd = dt.NewRow();
            //    drAdd["Cost"] = 0;
            //    drAdd["ItemCode"] = item.ItemCode;
            //    drAdd["JnlDate"] = DateTime.Now.ToString("dd MMM yyyy");
            //    drAdd["JobCode"] = "NA";
            //    drAdd["Narration"] = "WH TRANSFER IN";
            //    drAdd["Reference"] = "TRFID:" + item.iTrfID;
            //    drAdd["QtyAdjust"] = item.ItemQtyOut;
            //    drAdd["AccountGL"] = "";
            //    drAdd["Store"] = GitWH;
            //    dt.Rows.Add(drAdd);
            //    DataRow drMinus = dt.NewRow();
            //    drMinus["Cost"] = 0;
            //    drMinus["ItemCode"] = item.ItemCode;
            //    drMinus["JnlDate"] = DateTime.Now.ToString("dd MMM yyyy");
            //    drMinus["JobCode"] = "NA";
            //    drMinus["Narration"] = "WH TRANSFER OUT";
            //    drMinus["Reference"] = "TRFID:" + item.iTrfID;
            //    drMinus["QtyAdjust"] = -item.ItemQtyOut;
            //    drMinus["AccountGL"] = "";
            //    drMinus["Store"] = CurrentHeader.FromWH;
            //    dt.Rows.Add(drMinus);
            //}
            //ds.Tables.Add(dt);
            //string myds = Newtonsoft.Json.JsonConvert.SerializeObject(ds);
            //return myds;
            return null;
        }
    }
}