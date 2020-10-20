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

namespace InventoryCount
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class InvLandingPage : ContentPage
    {
        List<string> WHIDs = new List<string>();
        public static string WH = "";
        public static bool CustQty = false;
        public InvLandingPage()
        {
            InitializeComponent();
        }
        protected async override void OnAppearing()
        {
            base.OnAppearing();
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {


                RestClient client = new RestClient();
                string path = "DocumentSQLConnection";
                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                {
                    string str = $"GET?qry=SELECT * FROM InventoryHeader";
                    var Request = new RestRequest(str, Method.GET);
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    if (res.IsSuccessful && res.Content.Contains("CountID"))
                    {
                        pickerCountID.Items.Clear();
                        DataSet myds = new DataSet();
                        myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                        foreach (DataRow row in myds.Tables[0].Rows)
                        {
                            CustQty = Convert.ToBoolean(row["AllowCustomQty"]);
                            WHIDs.Add("" + row["Whse"]);
                            pickerCountID.Items.Add("" + row["CountID"] + "- " + row["Description"]);
                        }
                    }
                    else
                    {
                        pickerCountID.Title = "NO COUNTS FOUND";
                        pickerCountID.IsEnabled = false;
                    }
                }
                Loading.IsVisible = false;
            }
            else
            {
                Vibration.Vibrate();
                await DisplayAlert("No Internet Connection", "Please recconect", "OK");
            }
        }
        private async void Picker_SelectedIndexChanged(object sender, EventArgs e)
        {
            Loading.IsVisible = true;
            if (pickerCountID.SelectedIndex > -1)
            {
                WH = WHIDs[pickerCountID.SelectedIndex];
                await Navigation.PushAsync(new CountPage(Convert.ToInt32(pickerCountID.SelectedItem.ToString().Split('-')[0])));
            }
            Loading.IsVisible = false;
        }
    }
}
