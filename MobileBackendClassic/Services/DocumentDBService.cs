using IoTDemoSharedLibrary;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;
using MobileBackend.Data;
using MobileBackend.Extensions;
using MobileBackend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MobileBackend.Services
{
    public class DocumentDbService : IDocumentDbService
    {
        private readonly DocumentDbProvider _provider;
        public DocumentDbService(IConfiguration configuration)
        {
            _provider = new DocumentDbProvider(new DocumentDbSettings(configuration));
        }

       
        /// <summary>
        /// Builds a query for Entries
        /// </summary>
        /// <returns></returns>
        public List<Entry> GetEntries(DateTime date)
        {
            var feedOptions = new FeedOptions() {  };
            return _provider.CreateQuery<Entry>(feedOptions).Where(x => x.TimeStamp > date.Date && x.TimeStamp < date.AddDays(1).Date).ToList();
        }

        public Entry GetLastEntry()
        {

            var entry = (Entry)_provider.CreateQuery<dynamic>("SELECT TOP 1 * FROM c ORDER BY c.timeStamp DESC",null).AsEnumerable().FirstOrDefault();

            var feedOptions = new FeedOptions() { MaxItemCount = 1 };
            var identEntry = _provider.CreateQuery<Entry>(feedOptions).Where(x => x.TimeStamp > entry.TimeStamp.AddMinutes(-2) && x.EntrantName != "").AsEnumerable().FirstOrDefault();
            if (identEntry != null)
            {
                entry = identEntry;
            }

            return entry;
        }

        public List<Entry> GetIdentifiedEntrants(DateTime date)
        {
            var feedOptions = new FeedOptions() { };
            var identEntries = _provider.CreateQuery<Entry>(feedOptions).Where(x => x.TimeStamp > date.Date && x.TimeStamp < date.AddDays(1).Date && x.EntrantName != "").ToList();
            

            return identEntries;
        }

        public Entry GetLastEntryForPerson(string name)
        {
            var feedOptions = new FeedOptions() { };
            return (Entry)_provider.CreateQuery<dynamic>($"SELECT TOP 1 * FROM c WHERE c.entrantName = '{name}' ORDER BY c.timeStamp DESC", null).AsEnumerable().FirstOrDefault();
        }

        //SAMPLE OPERATIONS

        /// <summary>
        /// Adds a contact address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        //public async Task<string> AddContactAddress(auth0documentdb.Models.Db.ContactAddress address)
        //{
        //    return await _provider.AddItem<auth0documentdb.Models.Db.ContactAddress>(address);
        //}

        /// <summary>
        /// Updates a contact address
        /// </summary>
        /// <param name="address"></param>
        //public async Task UpdateContactAddress(auth0documentdb.Models.Db.ContactAddress address)
        //{
        //    await _provider.UpdateItem<auth0documentdb.Models.Db.ContactAddress>(address, address.Id);
        //}

        /// <summary>
        /// Deletes a contact address
        /// </summary>
        /// <param name="address"></param>
        //public async Task DeleteContactAddress(string id)
        //{
        //    await _provider.DeleteItem(id);
        //}


        /// <summary>
        /// Adds notification preferences
        /// </summary>
        /// <param name="preferences"></param>
        /// <returns></returns>
        //public async Task<string> AddNotificationPreferences(auth0documentdb.Models.Db.NotificationPreferences preferences)
        //{
        //    return await _provider.AddItem<auth0documentdb.Models.Db.NotificationPreferences>(preferences);
        //}

        /// <summary>
        /// Updates notification preferences
        /// </summary>
        /// <param name="preferences"></param>
        //public async Task UpdateNotificationPreferences(auth0documentdb.Models.Db.NotificationPreferences preferences)
        //{
        //    await _provider.UpdateItem<auth0documentdb.Models.Db.NotificationPreferences>(preferences, preferences.Id);
        //}
    }
}
