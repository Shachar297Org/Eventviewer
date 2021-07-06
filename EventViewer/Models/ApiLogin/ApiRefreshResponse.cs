using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventViewer.Models.ApiLogin
{
    public class ApiRefreshResponse
    {
        public List<EventData> Data { get; set; } = new List<EventData>();
    }
}
