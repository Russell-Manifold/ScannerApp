using Data.KeyboardContol;
using Data.Message;
using Data.Model;
using Newtonsoft.Json;
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

namespace WHTransfer
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class InPage : ContentPage
    {
        IMessage message = DependencyService.Get<IMessage>();
        private List<IBTHeader> headers = new List<IBTHeader>();
        private List<IBTItem> lines = new List<IBTItem>();
        private List<string> PickerItems = new List<string>();
        private List<string> DoneItems = new List<string>();
        private IBTHeader CurrentHeader;
        private ExtendedEntry _currententry;
        public InPage()
        {
            InitializeComponent();
            txfScannedItem.Focused += Entry_Focused;
        }
        protected async override void OnAppearing()
        {
            base.OnAppearing();
            pickerHeaders.IsEnabled = false;
            isLoading.IsVisible = true;
            await FetchHeaders();
            pickerHeaders.IsEnabled = true;
            isLoading.IsVisible = false;
            txfScannedItem.Focus();
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
        private bool CheckItem(string barcode)
        {
            try
            {
                lines.Where(x => x.ItemBarcode == barcode && x.ItemQtyIn == 0).First().ItemQtyIn = -1;
                return true;
            }
            catch
            {

            }
            return false;
        }
        private async Task FetchHeaders()
        {
            try
            {
                RestSharp.RestClient client = new RestSharp.RestClient();
                string path = "IBTHeader";
                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                {
                    string str = $"GET?";
                    var Request = new RestSharp.RestRequest();
                    Request.Resource = str;
                    Request.Method = RestSharp.Method.GET;
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
                                headers.Add(new IBTHeader { ID = Convert.ToInt32(row["TrfId"]), TrfDate = row["TrfDate"].ToString(), FromWH = row["FromWH"].ToString(), ToWH = row["ToWH"].ToString(), FromDate = row["FromDate"].ToString(), RecDate = row["RecDate"].ToString(), PickerUser = Convert.ToInt32(row["PickerUser"].ToString()), AuthUser = Convert.ToInt32(row["AuthUser"].ToString()), Active = Convert.ToBoolean(row["Active"]) });
                                PickerItems.Add(row["TrfId"].ToString());
                            }
                            catch
                            {

                            }
                        }
                        pickerHeaders.ItemsSource = PickerItems;
                    }
                }
            }
            catch
            {

            }
        }
        private async void PickerHeaders_SelectedIndexChanged(object sender, EventArgs e)
        {
            isLoading.IsVisible = true;
            CurrentHeader = headers.Where(x => x.ID == Convert.ToInt32(pickerHeaders.SelectedItem.ToString())).FirstOrDefault();
            await GetLines(CurrentHeader.ID);
            pickerHeaders.IsVisible = false;
            lblTop.IsVisible = false;
            LayoutMain.IsVisible = true;
            lblInfo.Text = "TRN :" + CurrentHeader.ID + "\nOn :" + Convert.ToDateTime(CurrentHeader.TrfDate).ToString("dd/MMM/yyyy");
            ListViewItems.ItemsSource = lines;
            isLoading.IsVisible = false;
            await Task.Delay(200);
            txfScannedItem.Focus();
        }
        private async Task<bool> GetLines(int trf)
        {
            try
            {
                RestSharp.RestClient client = new RestSharp.RestClient();
                string path = "IBTLines";
                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);

                string str = $"Get?qry=SELECT* FROM tblIBTLines WHERE iTrfId={trf}";
                var Request = new RestSharp.RestRequest();
                Request.Resource = str;
                Request.Method = RestSharp.Method.GET;
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
                            lines.Add(new IBTItem { ScanBarcode = row["ScanBarcode"].ToString(), ItemBarcode = row["ItemBarcode"].ToString(), ItemCode = row["ItemCode"].ToString(), ItemDesc = row["ItemDesc"].ToString(), ItemQtyOut = Convert.ToInt32(row["ItemQtyOut"]), ItemQtyIn = Convert.ToInt32(row["ItemQtyIn"]), PickerUser = Convert.ToInt32(row["PickerUser"]), AuthUser = Convert.ToInt32(row["AuthUser"]), PickDateTime = Convert.ToDateTime(row["PickDateTime"]), WH = row["WH"].ToString(), iTrfID = Convert.ToInt32(row["iTrfId"]) });
                        }
                        catch
                        {

                        }
                    }
                    return true;
                }
            }            
            catch
            {
            }
            return false;
        }
        private async Task IsDone()
        {
            var changes = new IBTItem();
            try
            {
                changes = lines.Where(x => x.ItemQtyIn == 0).First();
            }
            catch (InvalidOperationException)
            {
                message.DisplayMessage("Sending data...",true);
                await Complete();
            }         
                    
        }
        private async Task Complete()
        {
            try
            {
                DoneItems.Clear();
                foreach (IBTItem i in lines)
                {
                    if (!DoneItems.Contains(i.ItemCode)) {
                        DoneItems.Add(i.ItemCode);
                        string JnlAcc = await GetGlCode(i.ItemCode,i.WH);
                        int k = lines.Where(x => x.ItemCode == i.ItemCode).Sum(c=>c.ItemQtyOut);
                        RestSharp.RestClient client2 = new RestSharp.RestClient();
                        string path2 = "WHTransfer";
                        client2.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path2);
                        {
                            string str2 = $"POST?itemCode={i.ItemCode}&JnlAcc={JnlAcc}&JnlDate={DateTime.Now.ToString("dd MMM yyyy")}&JobCode={i.iTrfID}&Desc={i.iTrfID+"-Transfer items in"}&Ref={DateTime.Now.ToString("ddMMMyyyy")+"-"+i.iTrfID}&Qty={k}&Store={CurrentHeader.ToWH}";
                            var Request2 = new RestSharp.RestRequest();
                            Request2.Resource = str2;
                            Request2.Method = RestSharp.Method.POST;
                            var cancellationTokenSource2 = new CancellationTokenSource();
                            var res2 = await client2.ExecuteAsync(Request2, cancellationTokenSource2.Token);
                            if (res2.IsSuccessful && res2.Content != null)
                            {
                                str2 = $"POST?itemCode={i.ItemCode}&JnlAcc={JnlAcc}&JnlDate={DateTime.Now.ToString("dd MMM yyyy")}&JobCode={i.iTrfID}&Desc={i.iTrfID + "-Transfer items out"}&Ref={DateTime.Now.ToString("ddMMMyyyy") + "-" + i.iTrfID}&Qty={(k/-1)}&Store={CurrentHeader.FromWH}";
                                Request2 = new RestSharp.RestRequest();
                                Request2.Resource = str2;
                                Request2.Method = RestSharp.Method.POST;
                                cancellationTokenSource2 = new CancellationTokenSource();
                                res2 = await client2.ExecuteAsync(Request2, cancellationTokenSource2.Token);
                                if (res2.IsSuccessful && res2.Content != null)
                                {
                                    RestSharp.RestClient client = new RestSharp.RestClient();
                                    string path = "IBTHeader";
                                    client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                                    {
                                        string str = $"DELETE?coid={CurrentHeader.ID}";
                                        var Request = new RestSharp.RestRequest();
                                        Request.Resource = str;
                                        Request.Method = RestSharp.Method.DELETE;
                                        var cancellationTokenSource = new CancellationTokenSource();
                                        var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                                        if (res.IsSuccessful && res.Content != null)
                                        {
                                            
                                        }
										else
										{
                                            await DisplayAlert("Error","Could not upload","OK");
                                            return;
                                        }
                                    } 
                                }
                                else
                                {
                                    await DisplayAlert("Error", "Could not upload", "OK");
                                    return;
                                }
                            }
                            else if (!res2.IsSuccessful && res2.Content != null)
							{
                                await DisplayAlert("Error!",""+res2.Content.Substring(res2.Content.IndexOf("Message"),res2.Content.IndexOf("Data")-res2.Content.IndexOf("Message")),"OK");
                                return;
                            }
                            else
                            {
                                await DisplayAlert("Error", "Could not upload", "OK");
                                return;
                            }
                        }
                    }
                }               
            }
            catch(Exception ed)
            {
                await DisplayAlert("Error!", "Could not complete", "OK");
                return;
            }         
            await DisplayAlert("Complete", "Transfer complete!", "OK");
            Navigation.RemovePage(Navigation.NavigationStack[2]);
            await Navigation.PopAsync();
        }
        private async void btnComplete_Clicked(object sender, EventArgs e)
        {
            await IsDone();
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
        private async void txfScannedItem_Completed(object sender, EventArgs e)
		{
            if (txfScannedItem.Text.Length > 1)
            {
                //txfScannedItem.Text = GoodsRecieveingApp.MainPage.CalculateCheckDigit(txfScannedItem.Text);
                if (!CheckItem(txfScannedItem.Text))
                {
                    Vibration.Vibrate();
                    message.DisplayMessage("This item is invalid", true);
                }
                else
                {
                    _ = IsDone();
                    await Task.Delay(100);
                    ListViewItems.ItemsSource = null;
                    ListViewItems.ItemsSource = lines;
                }
                txfScannedItem.Text = "";
                txfScannedItem.Focus();
            }
        }
	}
}