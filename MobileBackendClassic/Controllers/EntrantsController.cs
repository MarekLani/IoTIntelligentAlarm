using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MobileBackend.Services;
using Newtonsoft.Json;
using IoTDemoSharedLibrary;
using System.Net.Http;
using System.Net;

namespace MobileBackend.Controllers
{
    [Produces("application/json")]
    [Route("api/Entrants")]
    public class EntrantsController : Controller
    {
        private readonly IDocumentDbService _dbService;

        public EntrantsController(IDocumentDbService dbService)
        {
            _dbService = dbService;
        }

        /// <summary>
        /// Returns all entrants for today
        /// </summary>
        /// <returns></returns>
        // GET: api/Entrants
        [HttpGet]
        public List<Entry> Get()
        {
            var res = _dbService.GetIdentifiedEntrants(DateTime.Now.Date);
            return res;
          
        }

        // GET: api/Entries/marek
        [HttpGet("{name}", Name = "GetEntriesForPerson")]

        public Entry Get(string name)
        {
            var res = _dbService.GetLastEntryForPerson(name);
            if (res == null)
                return new Entry("Entry");
            return res;
        }
    }
}
