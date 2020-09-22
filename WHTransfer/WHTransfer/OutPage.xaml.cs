using Data.KeyboardContol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.Model;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Threading;
using System.Data;
using RestSharp;
using Data.Message;
using Xamarin.Essentials;

namespace WHTransfer
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class OutPage : ContentPage
    {
        private List<string> WHIDs = new List<string>();
        IMessage message = DependencyService.Get<IMessage>();
        public static string FromWH="";
        public OutPage()
        {
            InitializeComponent();
        }
        private async void Button_Clicked(object sender, EventArgs e)
        {
            if (pickerFromWH.SelectedIndex==-1|| pickerToWH.SelectedIndex == -1)
            {
                Vibration.Vibrate();
                message.DisplayMessage("Please enter all fields",true);
            }
            else
            {
                FromWH = pickerFromWH.SelectedItem.ToString();
                await GoodsRecieveingApp.App.Database.Insert(new IBTHeader { TrfDate = DateTime.Now.ToString("dd MMM yyyy"), FromWH = pickerFromWH.SelectedItem.ToString(), ToWH = pickerToWH.SelectedItem.ToString(), FromDate = DatePickerFrom.Date.ToString("dd MMM yyyy"), RecDate = DatePickerRec.Date.ToString("dd MMM yyyy"), Active = true });
                message.DisplayMessage("Complete! Transfer started",true);
                await Navigation.PushAsync(new OutItems());
            }
        }   
        private async void BtnContinue_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new OutItems());
        }
        private void DatePickerFrom_Unfocused(object sender, FocusEventArgs e)
        {
            lblDatePickFrom.Text = "Sending Date: " + DatePickerFrom.Date.ToString("dd MMM yyyy");
        }
        private void DatePickerRec_Unfocused(object sender, FocusEventArgs e)
        {
            lblDatePickRec.Text = "Receving Date: " + DatePickerRec.Date.ToString("dd MMM yyyy");
        }        
        protected async override void OnAppearing()
        {
            base.OnAppearing();
            await FetchWH();
            await GoodsRecieveingApp.App.Database.DeleteAllHeaders();
            DatePickerFrom.Date = DateTime.Today;
            DatePickerFrom.MinimumDate = DateTime.Today;
            DatePickerRec.MinimumDate = DateTime.Today;
            DatePickerRec.Date = DateTime.Today;
        }
        private async Task<bool> FetchWH()
        {
            RestSharp.RestClient client = new RestSharp.RestClient();
            string path = "GetWarehouses";
            client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
            {
                string str = $"GET?";
                var Request = new RestSharp.RestRequest();
                Request.Resource = str;
                Request.Method = RestSharp.Method.GET;
                var cancellationTokenSource = new CancellationTokenSource();
                var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                if (res.Content.ToString().Contains("WHID"))
                {
                    DataSet myds = new DataSet();
                    myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                    foreach (DataRow row in myds.Tables[0].Rows)
                    {
                        WHIDs.Add(row["WHID"].ToString());
                    }
                    pickerToWH.ItemsSource = WHIDs;
                    pickerFromWH.ItemsSource = WHIDs;
                    return true;
                }
            }
            return false;
        }
        private void txfToWH_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (pickerToWH.SelectedIndex != -1)
            {
                if (pickerFromWH.SelectedIndex != -1)
                {
                    if (pickerToWH.SelectedItem.ToString() == pickerFromWH.SelectedItem.ToString())
                    {
                        message.DisplayMessage("You cannot have the same warehouse for both", true);
                        Vibration.Vibrate();
                        pickerToWH.SelectedIndex = -1;
                        btnAdd.IsEnabled = false;
                        DatePickerRec.IsEnabled = false;
                        DatePickerFrom.IsEnabled = false;
                    }
                    else
                    {
                        btnAdd.IsEnabled = true;
                        DatePickerRec.IsEnabled = true;
                        DatePickerFrom.IsEnabled = true;
                    }
                }
            }
        }
        private void txfFromWH_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (pickerFromWH.SelectedIndex != -1)
            {
                if (pickerToWH.SelectedIndex!=-1)
                {
                    if (pickerFromWH.SelectedItem.ToString() == pickerToWH.SelectedItem.ToString())
                    {
                        message.DisplayMessage("You cannot have the same warehouse for both", true);
                        Vibration.Vibrate();
                        pickerFromWH.SelectedIndex = -1;
                        pickerToWH.IsEnabled = true;
                        btnAdd.IsEnabled = false;
                        DatePickerRec.IsEnabled = false;
                        DatePickerFrom.IsEnabled = false;
                    }
                    else
                    {
                        pickerToWH.IsEnabled = true;
                        btnAdd.IsEnabled = true;
                        DatePickerRec.IsEnabled = true;
                        DatePickerFrom.IsEnabled = true;
                    }
                }
                else
                {
                    pickerToWH.IsEnabled = true;
                }
            }
        }
    }
}