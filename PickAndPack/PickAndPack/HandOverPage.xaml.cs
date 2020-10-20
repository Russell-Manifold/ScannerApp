using Data.KeyboardContol;
using Data.Message;
using Data.Model;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public partial class HandOverPage : ContentPage
    {
        IMessage message = DependencyService.Get<IMessage>();
        private ExtendedEntry _currententry;
        public int RecUserCode;
        public HandOverPage()
        {
            InitializeComponent();
            txfSOCOde.Focused += Entry_Focused;
            txfRecUser.Focused += Entry_Focused;
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            txfRecUser.Focus();
        }
        private async void txfRecUser_Completed(object sender, EventArgs e)
        {
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                if (!await CheckUser())
                {
                    Loading.IsVisible = false;
                    txfRecUser.Text = "";
                    txfRecUser.Focus();
                    return;
                }
                lblRecUser.IsVisible = false;
                txfRecUser.IsVisible = false;
                lblSOCode.IsVisible = true;
                txfSOCOde.IsVisible = true;
                MainImg.IsVisible = true;
                btnComplete.IsVisible = true;
            }
            else
            {
                Vibration.Vibrate();
                message.DisplayMessage("No Internet Connection", true);
            }
            txfSOCOde.Focus();
        }
        private async void txfSOCOde_Completed(object sender, EventArgs e)
        {
            if (!lblScannedCodes.Text.Contains(txfSOCOde.Text))
            {
                if (!await FetchSO(txfSOCOde.Text))
                {
                    message.DisplayMessage("Error in Finding code", true);
                    Vibration.Vibrate();
                }
            }
            else
            {
                message.DisplayMessage("This SO has been scanned already", true);
                Vibration.Vibrate();
            }
            txfSOCOde.Text = "";
            txfSOCOde.Focus();
        }
        async Task<bool> FetchSO(string code)
        {
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                RestSharp.RestClient client = new RestSharp.RestClient();
                string path = "DocumentSQLConnection";
                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                {
                    string str = $"GET?qry=SELECT * FROM tblTempDocHeader WHERE DocNum='{code}'";
                    var Request = new RestSharp.RestRequest();
                    Request.Resource = str;
                    Request.Method = RestSharp.Method.GET;
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    if (res.Content.ToString().Contains("DocNum"))
                    {
                        DataSet myds = new DataSet();
                        myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                        foreach (DataRow row in myds.Tables[0].Rows)
                        {
                            try
                            {
                                if (!await SendStatus(row["DocStatus"].ToString()))
                                {
                                    return false;
                                }
                            }
                            catch
                            {
                                return false;
                            }
                        }
                        if (!lblScannedCodes.Text.Contains(txfSOCOde.Text))
                        {
                            lblScannedCodes.Text += txfSOCOde.Text + "\n\n";
                        }
                        return true;
                    }
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
                string str = $"POST?qry=UPDATE tblTempDocHeader SET DocStatus={((Convert.ToInt32(status)) + 1)},{Status}={RecUserCode} WHERE DocNum ='{txfSOCOde.Text}'";
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
        private void Entry_Focused(object sender, FocusEventArgs e)
        {
            _currententry = sender as ExtendedEntry;
            if (_currententry != null)
            {
                try
                {
                    _currententry.HideKeyboard();
                }
                catch
                { }
            }
        }
        private async Task<bool> CheckUser()
        {
            try
            {
                RestClient client = new RestClient();
                string path = "GetUser";
                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                {
                    string str = $"GET?UserName={txfRecUser.Text}";
                    var Request = new RestRequest(str, Method.GET);
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    if (res.IsSuccessful && res.Content.Contains("UserName"))
                    {
                        DataSet myds = new DataSet();
                        myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                        foreach (DataRow row in myds.Tables[0].Rows)
                        {
                            RecUserCode = Convert.ToInt32(row["Id"].ToString());
                        }
                        Loading.IsVisible = false;
                        return true;

                    }
                    else
                    {
                        Loading.IsVisible = false;
                        message.DisplayMessage("Invalid user!", true);
                        Vibration.Vibrate();
                        return false;
                    }
                }
            }
            catch
            {
                Loading.IsVisible = false;
                message.DisplayMessage("Invalid user!", true);
                Vibration.Vibrate();
                return false;
            }
        }
        private async void btnComplete_Clicked(object sender, EventArgs e)
        {
            await DisplayAlert("COMPLETE!", "All data saved", "OK");
            await Navigation.PopAsync();
        }
    }
}