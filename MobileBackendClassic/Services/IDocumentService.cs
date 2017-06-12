using IoTDemoSharedLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MobileBackend.Services
{
    /// <summary>
    /// DocumentDB service
    /// </summary>
    public interface IDocumentDbService
    {
    
        List<Entry> GetEntries(DateTime date);
        Entry GetLastEntry();
        List<Entry> GetIdentifiedEntrants(DateTime date);
        Entry GetLastEntryForPerson(string name);
    }
}
