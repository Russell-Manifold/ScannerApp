using Data.KeyboardContol;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GoodsRecieveingApp;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Data.Message;
using Xamarin.Essentials;
using Data.Model;

namespace ScannerFDB
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginPage : ContentPage
    {
        private ExtendedEntry _currententry;
        IMessage message = DependencyService.Get<IMessage>();
        public LoginPage()
        {
            InitializeComponent();
            txfUserBarcode.Focused += Entry_Focused;
        }
        protected async override void OnAppearing()
        {
            base.OnAppearing();
            txfUserBarcode.Text = "";
            await Task.Delay(200);
            txfUserBarcode.Focus();
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
        private async Task<bool> CheckUser()
        {
            AccessLoading.IsVisible = true;
            try
            {
                RestClient client = new RestClient();
                string path = "GetUser";
                //client.BaseUrl = new Uri("https://manifoldsa.co.za/FDBAPI/api/" + path);
                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath+ path);
                {
                    string str = $"GET?UserName={txfUserBarcode.Text}";
                    var Request = new RestRequest(str, Method.GET);
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                     if (res.IsSuccessful && res.Content.Contains("UserName"))
                    {
                        DataSet myds = new DataSet();
                        myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                        foreach (DataRow row in myds.Tables[0].Rows)
                        {
                            GoodsRecieveingApp.MainPage.UserName = row["UserName"].ToString();
                            GoodsRecieveingApp.MainPage.UserCode = Convert.ToInt32(row["Id"].ToString());

                            GoodsRecieveingApp.MainPage.fReceive = Convert.ToBoolean(row["fReceive"]);
                            GoodsRecieveingApp.MainPage.fRepack = Convert.ToBoolean(row["fRepack"]);
                            GoodsRecieveingApp.MainPage.fInvCount = Convert.ToBoolean(row["fInvCount"]);
                            GoodsRecieveingApp.MainPage.fWhTrf = Convert.ToBoolean(row["fWhTrf"]);
                            GoodsRecieveingApp.MainPage.fPickPack = Convert.ToBoolean(row["fPickPack"]);

                            GoodsRecieveingApp.MainPage.AuthWHTrf = Convert.ToBoolean(row["AuthWHTrf"]);
                            GoodsRecieveingApp.MainPage.AuthReceive = Convert.ToBoolean(row["AuthReceive"]);
                            GoodsRecieveingApp.MainPage.AuthDispatch = Convert.ToBoolean(row["AuthDispatch"]);
                            GoodsRecieveingApp.MainPage.PickChecker = Convert.ToBoolean(row["PickChecker"]);
                            GoodsRecieveingApp.MainPage.SystAdmin = Convert.ToBoolean(row["SystAdmin"]);
                            GoodsRecieveingApp.MainPage.CreateInvCount = Convert.ToBoolean(row["CreateInvCount"]);
                            GoodsRecieveingApp.MainPage.CloseInvCount = Convert.ToBoolean(row["CloseInvCount"]);
                            //GoodsRecieveingApp.MainPage.CanPartRec = Convert.ToBoolean(row["CanPartReceive"]);
                            GoodsRecieveingApp.MainPage.PSCollect = Convert.ToBoolean(row["PSCollect"]);
                        }
                        _ = GetInfo();
                            await Navigation.PushAsync(new MainPage());
                            AccessLoading.IsVisible = false;
                            return true;
                        
                    }
                    else
                    {
                        AccessLoading.IsVisible = false;
                        message.DisplayMessage("Error - " + res.ErrorMessage,true);
                        Vibration.Vibrate();
                        return false;
                    }
                }
            }
            catch
            {
                AccessLoading.IsVisible = false;
                message.DisplayMessage("Invalid user!", true);
                Vibration.Vibrate();
                return false;
            }  
        }
        private async Task GetInfo()
        {
            try
            {
                RestClient client = new RestClient();
                string path = "DocumentSQLConnection";
                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                {
                    string str = $"GET?qry=SELECT * FROM SystemConfig";
                    var Request = new RestRequest(str, Method.GET);
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    if (res.IsSuccessful && res.Content.Contains("PaperPickSlips"))
                    {
                        DataSet myds = new DataSet();
                        myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                        try
                        {
                            DeviceConfig conf = await GoodsRecieveingApp.App.Database.GetConfig();
                            conf.PaperPickSlips = Convert.ToBoolean(myds.Tables[0].Rows[0]["PaperPickSlips"]);
                            conf.UseBins = Convert.ToBoolean(myds.Tables[0].Rows[0]["UseBins"]);
                            conf.UseZones = Convert.ToBoolean(myds.Tables[0].Rows[0]["UseZones"]);
                            await GoodsRecieveingApp.App.Database.Update(conf);
                        }
                        catch
                        {
                            await GoodsRecieveingApp.App.Database.Insert(new DeviceConfig {PaperPickSlips=Convert.ToBoolean(myds.Tables[0].Rows[0]["PaperPickSlips"]), UseBins = Convert.ToBoolean(myds.Tables[0].Rows[0]["UseBins"]), UseZones = Convert.ToBoolean(myds.Tables[0].Rows[0]["UseZones"]) });
                        }
                    }
                }
            }
            catch
            {

            }
        }
        private async void txfUserBarcode_Completed(object sender, EventArgs e)
        {
            //txfUserBarcode.Text = "USER";
            
            if (!(txfUserBarcode.Text.Length < 2))
            {
                if (!await CheckUser())
                {
                    txfUserBarcode.Text = ""; 
                    txfUserBarcode.Focus();
                }
            }
        }

        private void btnInWH_Clicked(object sender, EventArgs e)
        {
            btnOUTWH.IconImageSource = "WHTrfOut.png";
            btnInWH.IconImageSource = "WHTrfINGreen.png";
            GoodsRecieveingApp.MainPage.APIPath = GoodsRecieveingApp.MainPage.APIPathIN;
        }

        private void btnOUTWH_Clicked(object sender, EventArgs e)
        {
            btnInWH.IconImageSource = "WHTrfIN.png";
            btnOUTWH.IconImageSource = "WHTrfOutGreen.png";
            GoodsRecieveingApp.MainPage.APIPath= GoodsRecieveingApp.MainPage.APIPathOUT;
        }

        private void txfUserBarcode_TextChanged(object sender, TextChangedEventArgs e)
        {
              string str = txfUserBarcode.Text;
        }
    }
}