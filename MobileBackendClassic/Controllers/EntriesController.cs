using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MobileBackend.Services;
using Newtonsoft.Json;
using IoTDemoSharedLibrary;

namespace MobileBackend.Controllers
{
    [Produces("application/json")]
    [Route("api/Entries/[action]")]
    public class EntriesController : Controller
    {
        private readonly IDocumentDbService _dbService;
        public EntriesController(IDocumentDbService dbService)
        {
            _dbService = dbService;
        }
        // GET: api/Entries/GetLast
        //Gets last entry
        [HttpGet]
        public Entry GetLast()
        {
            return _dbService.GetLastEntry();
        }

        // GET: api/Entries/Get/12-12-2015
        [HttpGet("{date}", Name = "GetEntriesForDate")]

        public List<Entry> Get(DateTime? date)
        {
            var res = _dbService.GetEntries(date.Value.Date);
            return res;
        }
        
       
    }
}
