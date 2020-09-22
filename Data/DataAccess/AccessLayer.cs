using SQLite;
using System;
using System.Collections.Generic;
using System.Text;
using Data.Model;
using System.Threading.Tasks;

namespace Data.DataAccess
{
    public class AccessLayer
    {
        readonly SQLiteAsyncConnection database;
        public AccessLayer(string dbPath)
        {
            database = new SQLiteAsyncConnection(dbPath);
            database.CreateTableAsync<BOMItem>().Wait();
            database.CreateTableAsync<DeviceConfig>().Wait();
            database.CreateTableAsync<DocHeader>().Wait();
            database.CreateTableAsync<DocLine>().Wait();
            database.CreateTableAsync<User>().Wait();
            database.CreateTableAsync<IBTItem>().Wait();
            database.CreateTableAsync<IBTHeader>().Wait();
            database.CreateTableAsync<InventoryItem>().Wait();
        }
        public Task<DocHeader> GetHeader(string docNum)
        {
            return database.Table<DocHeader>().Where(x=>x.DocNum==docNum).FirstOrDefaultAsync();
        }
        public Task<int> DeleteOldHeaders()
        {
            return database.Table<IBTHeader>().DeleteAsync();
        }
        public Task<int>Delete(DocLine d)
        {
            return database.DeleteAsync(d);
        }
        public async Task DeleteAllWithItemWithFilter(DocLine d)
        {
            foreach (DocLine doc in await database.Table<DocLine>().Where(x => x.DocNum == d.DocNum&&x.ItemDesc==d.ItemDesc&&x.PalletNum==d.PalletNum).ToListAsync())
            {
                await database.DeleteAsync(doc);
            }
        }
        public Task<int> DeleteSpecificDocs(string docNum)
        {
            return database.ExecuteAsync($"DELETE FROM DocLine WHERE DocNum='{docNum}'");
        }
        public Task<int> Delete(DocHeader h)
        {
            return database.DeleteAsync(h);
        }
        public Task<int> DeleteAllHeaders()
        {
            return database.ExecuteAsync("DELETE FROM IBTHeader");
        }
        public Task<int> Delete(IBTItem h)
        {
            return database.DeleteAsync(h);
        }
        public Task<int> Delete(IBTHeader h)
        {
            return database.DeleteAsync(h);
        }
        public Task<int> DeleteBOMData()
        {
            return database.ExecuteAsync("DELETE FROM BOMItem");
        }
        public Task<int> DeleteUser()
        {
            return database.ExecuteAsync("DELETE FROM User");
        }
        public Task<int> Update(IBTItem data)
        {
            return database.UpdateAsync(data);
        }
        public Task<int> Update(DocLine data)
        {
            return database.UpdateAsync(data);
        }
        public Task<int> Update(DeviceConfig data)
        {
            return database.UpdateAsync(data);
        }
        public Task<int> Update(List<IBTItem> data)
        {
            return database.UpdateAllAsync(data);
        }
        public Task<int> Insert(IBTHeader data)
        {
            return database.InsertAsync(data);
        }
        public Task<int> Insert(DocLine data)
        {
            return database.InsertAsync(data);
        }
        public Task<int> Insert(IBTItem data)
        {
            return database.InsertAsync(data);
        }
        public Task<int> Insert(BOMItem data)
        {
            return database.InsertAsync(data);
        }
        public Task<int> Insert(DeviceConfig data)
        {
            return database.InsertAsync(data);
        }
        public Task<int> Insert(DocHeader data)
        {
            return database.InsertAsync(data);
        }
        public Task<int> Insert(User user)
        {
            return database.InsertAsync(user);
        }
        public Task<List<DocLine>> GetLinesAsync()
        {
            return database.Table<DocLine>().ToListAsync();
        }
        public Task<DeviceConfig> GetConfig()
        {
            return database.Table<DeviceConfig>().FirstOrDefaultAsync();
        }
        public Task<List<IBTHeader>> GetIBTHeaders()
        {
            return database.Table<IBTHeader>().ToListAsync();
        }
        public Task<List<IBTItem>> GetIBTItems()
        {
            return database.Table<IBTItem>().ToListAsync();
        }
        public Task<List<BOMItem>> GetBOMITEMSAsync()
        {
            return database.Table<BOMItem>().ToListAsync();
        }
        public Task<List<DocLine>> GetSpecificDocsAsync(string DocNumber)
        {
            return database.Table<DocLine>().Where(i => i.DocNum == DocNumber).ToListAsync();
        }
        public Task<DocLine> GetOneSpecificDocAsync(string DocNumber)
        {
            return database.Table<DocLine>().Where(i => i.DocNum == DocNumber && i.ItemQty != 0).FirstAsync();
        }
        public Task<BOMItem> GetBOMItem(string packBarcode)
        {
            return database.Table<BOMItem>().Where(i => i.PackBarcode == packBarcode).FirstAsync();
        }       
    }
}
