using Data.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ScannerFDB
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CreateUser : ContentPage
    {
        public CreateUser()
        {
            InitializeComponent();
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            var a = this.Navigation.NavigationStack.ToList();
            Navigation.RemovePage(a[1]);
            List<string> s = new List<string>();
            s.Add("Default");
            s.Add("Pick and Pack Manager");
            s.Add("Warehouse Manager");
            s.Add("Super User");
            pickerLevel.ItemsSource = s;
        }
        private async void BtnCreateUser_Clicked(object sender, EventArgs e)
        {
            if (txfUserName.Text != null && txfEmpNum.Text != null && txfUserName.Text != ""&&txfEmpNum.Text!=""&&pickerLevel.SelectedItem!=null)
            {
                switch (pickerLevel.SelectedItem.ToString())
                {
                    case "Picker":
                        if (await AddUser(txfUserName.Text,txfEmpNum.Text,1))
                        {
                            await DisplayAlert("Complete", "The User has been added", "OK");                            
                        }
                        else
                        {
                            await DisplayAlert("Error!", "The User has not been added", "OK");
                        }                       
                        break;
                    case "Pick and Pack Supervisor":
                        if (await AddUser(txfUserName.Text, txfEmpNum.Text, 2))
                        {
                            await DisplayAlert("Complete", "The User has been added", "OK");
                        }
                        else
                        {
                            await DisplayAlert("Error!", "The User has not been added", "OK");
                        }                       
                        break;
                    case "Warehouse Manager":
                        if (await AddUser(txfUserName.Text, txfEmpNum.Text, 3))
                        {
                            await DisplayAlert("Complete", "The User has been added", "OK");
                        }
                        else
                        {
                            await DisplayAlert("Error!", "The User has not been added", "OK");
                        }                       
                        break;
                    case "Super User":
                        if (await AddUser(txfUserName.Text, txfEmpNum.Text, 4))
                        {
                            await DisplayAlert("Complete", "The User has been added", "OK");
                        }
                        else
                        {
                            await DisplayAlert("Error!", "The User has not been added", "OK");
                        }                       
                        break;
                    
                    default:
                        await DisplayAlert("Error", "The access level you have entered is invalid", "OK");
                        break;
                }
            }
        }
        public async Task<bool> AddUser(string userName,string UserNum,int Access)
        {
            try
            {
                RestSharp.RestClient client = new RestSharp.RestClient();
                string path = "AddUser";
                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                {
                    string str = $"POST?UserName={userName}&UserNum={UserNum}&Access={Access}";
                    var Request = new RestSharp.RestRequest();
                    Request.Resource = str;
                    Request.Method = RestSharp.Method.POST;
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    if (res.IsSuccessful && res.Content.Contains("User has been added"))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
            return false;
        }
    }
}