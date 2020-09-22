using Data.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ScannerFDB
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AdminPage : ContentPage
    {       
        public AdminPage()
        {
            InitializeComponent();           
        }
    
        private async void Button_Clicked(object sender, EventArgs e)
        {
            var sug = await GoodsRecieveingApp.App.Database.GetBOMITEMSAsync();
            LodingIndiactor.IsVisible = true;
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                RestSharp.RestClient client = new RestSharp.RestClient();
                string path = "Find";
                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path);
                {
                    string str = $"GET?qrystr=ACCBOML|0|0&posInString=0&searchValue=0";
                    var Request = new RestSharp.RestRequest();
                    Request.Resource = str;
                    Request.Method = RestSharp.Method.GET;
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    if (res.IsSuccessful)
                    {
                        await GoodsRecieveingApp.App.Database.DeleteBOMData();
                        foreach (string s in res.Content.Split('#'))
                        {
                            if (s.Contains("0"))
                            {
                                BOMItem item = new BOMItem();
                                item.PackBarcode = s.Split('|')[1];
                                item.ItemCode = s.Split('|')[3];
                                item.Qty = Convert.ToInt32(s.Split('|')[4]);
                                RestSharp.RestClient client1 = new RestSharp.RestClient();
                                string path1 = "GetField";
                                client1.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath + path1);
                                {
                                    string str1 = $"GET?qrystr=ACCPRD|0|{ s.Split('|')[3] }|3";
                                    var Request1 = new RestSharp.RestRequest();
                                    Request1.Resource = str1;
                                    Request1.Method = RestSharp.Method.GET;
                                    var cancellationTokenSource1 = new CancellationTokenSource();
                                    var res1 = await client1.ExecuteAsync(Request1, cancellationTokenSource1.Token);
                                    if (res1.IsSuccessful)
                                    {
                                        item.ItemDesc = res1.Content.Split('|')[1].Substring(0,(res1.Content.Split('|')[1].Length-1));
                                    }
                                }
                                var sugs = await GoodsRecieveingApp.App.Database.GetBOMITEMSAsync();
                                await GoodsRecieveingApp.App.Database.Insert(item);
                                var sdg = await GoodsRecieveingApp.App.Database.GetBOMITEMSAsync();
                            }                                                                                                        
                        }
                        LodingIndiactor.IsVisible = false;
                        await DisplayAlert("Complete","All items successfully updated","OK");
                    }
                }
            }
            else
            {
                LodingIndiactor.IsVisible = false;
                await DisplayAlert("Error!!", "Please Reconnect to the internet", "OK");
            }
        }
 
        //private async void Add_User_Clicked(object sender, EventArgs e)
        //{
        //    await Navigation.PushAsync(new CreateUser());
        //}

        private void btnUpdate_Clicked(object sender, EventArgs e)
        {

        }

        private async void btnReset_Clicked(object sender, EventArgs e)
        {          
            RestSharp.RestClient client = new RestSharp.RestClient();
            string path = "DocumentSQLConnection";
            client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath+path);
            {
                string qry = "DELETE FROM tblTempDocHeader";
                string str = $"Post?qry={qry}";
                var Request = new RestSharp.RestRequest();
                Request.Resource = str;
                Request.Method = RestSharp.Method.POST;
                var res = client.Execute(Request);
                if (res.IsSuccessful && res.Content != null)
                {
                }

                qry = "DELETE FROM tblTempDocLines";
                 str = $"Post?qry={qry}";
                 Request = new RestSharp.RestRequest();
                Request.Resource = str;
                Request.Method = RestSharp.Method.POST;
                res = client.Execute(Request);
                if (res.IsSuccessful && res.Content != null)
                {
                } 
                qry = "UPDATE InventoryLines SET FirstScanQty=0,SecondScanQty=0,isFirst=1,Complete=0";
                 str = $"Post?qry={qry}";
                 Request = new RestSharp.RestRequest();
                Request.Resource = str;
                Request.Method = RestSharp.Method.POST;
                res = client.Execute(Request);
                if (res.IsSuccessful && res.Content != null)
                {
                }
                qry = "DELETE FROM tblIBTLines";
                str = $"Post?qry={qry}";
                Request = new RestSharp.RestRequest();
                Request.Resource = str;
                Request.Method = RestSharp.Method.POST;
                res = client.Execute(Request);
                if (res.IsSuccessful && res.Content != null)
                {
                }

                qry = "DELETE FROM tblIBTHeader";
                str = $"Post?qry={qry}";
                Request = new RestSharp.RestRequest();
                Request.Resource = str;
                Request.Method = RestSharp.Method.POST;
                res = client.Execute(Request);
                if (res.IsSuccessful && res.Content != null)
                {
                }

                qry = "DELETE FROM PalletTransaction";
                str = $"Post?qry={qry}";
                Request = new RestSharp.RestRequest();
                Request.Resource = str;
                Request.Method = RestSharp.Method.POST;
                res = client.Execute(Request);
                if (res.IsSuccessful && res.Content != null)
                {
                }
            }
            Button_Clicked(null, null);
        }

        private async void btnConnectionSetup_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ConfigPage());
        }

        private void btnModuleActive_Clicked(object sender, EventArgs e)
        {

        }
    }
}