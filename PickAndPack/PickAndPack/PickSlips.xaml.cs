using Data.KeyboardContol;
using Data.Message;
using Data.Model;
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

namespace PickAndPack
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PickSlips : ContentPage
    {
        IMessage message = DependencyService.Get<IMessage>();
        private ExtendedEntry _currententry;
        string SOTextlbl = "";
        string supplier = "";
        public PickSlips()
        {
            InitializeComponent();
            txfSOCodes.Focused += Entry_Focused;
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
                {}
            }
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            Task.Delay(100);
            txfSOCodes.Focus();
        }
        async Task<bool> FetchSO(string code)
        {
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                RestSharp.RestClient client = new RestSharp.RestClient();
                string path = "GetDocument";
                client.BaseUrl = new Uri(GoodsRecieveingApp.MainPage.APIPath+ path);
                {
                    string str = $"GET?qrystr=ACCHISTL|6|{code}|102|"+ GoodsRecieveingApp.MainPage.UserCode;
                    var Request = new RestSharp.RestRequest();
                    Request.Resource = str;
                    Request.Method = RestSharp.Method.GET;
                    var cancellationTokenSource = new CancellationTokenSource();
                    var res = await client.ExecuteAsync(Request, cancellationTokenSource.Token);
                    if (res.Content.ToString().Contains("DocNum"))
                    {
                        DataSet myds = new DataSet();
                        myds = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSet>(res.Content);
                       // if (await GoodsRecieveingApp.App.Database.GetHeader(myds.Tables[0].Rows[0]["OrderNumber"].ToString())==null){
                            foreach (DataRow row in myds.Tables[0].Rows)
                            {
                                try
                                {
                                    var Doc = new DocLine();
                                    Doc.DocNum = row["DocNum"].ToString();
                                    Doc.SupplierCode = row["SupplierCode"].ToString();
                                    Doc.SupplierName = row["SupplierName"].ToString();
                                    Doc.ItemBarcode = row["ItemBarcode"].ToString();
                                    Doc.ItemCode = row["ItemCode"].ToString();
                                    Doc.ItemDesc = row["ItemDesc"].ToString();
                                    Doc.ScanAccQty = 0;
                                    Doc.ScanRejQty = 0;
                                    Doc.ItemQty = Convert.ToInt32(row["ItemQty"].ToString().Trim());
                                    await GoodsRecieveingApp.App.Database.Insert(Doc);
                                    supplier = Doc.SupplierName;
                                }
                                catch
                                {
                                    return false;
                                }
                            }
                        if (!SOTextlbl.Contains(txfSOCodes.Text))
                        {
                            SOTextlbl += txfSOCodes.Text + " - " + supplier + "\n";
                        }                          
                           return true;
                        //}
                    }
                }
            }
            return false;
        }
        private async void txfSOCodes_Completed(object sender, EventArgs e)
        {
            if (txfSOCodes.Text.Length == 8)
            {
                lblSoName.IsVisible = false;
                LoadingIndicator.IsVisible = true;
                if (await FetchSO(txfSOCodes.Text))
                {
                    LoadingIndicator.IsRunning = false;
                    message.DisplayMessage("Success", true);
                    lblSoName.IsVisible = true;
                    lblSoName.Text = SOTextlbl;
                }
                else
                {
                    LoadingIndicator.IsRunning = false;
                    Vibration.Vibrate();
                    message.DisplayMessage("Something went wrong", true);
                    lblSoName.IsVisible = false;
                }
                LoadingIndicator.IsVisible = false;
                txfSOCodes.Text = "";
                txfSOCodes.Focus();
            }
        }
        private async  void btnComplete_Clicked(object sender, EventArgs e)
        {
            await DisplayAlert("COMPLETE","All the data has been saved!","OK");
            await Navigation.PopAsync();
        }
    }
}