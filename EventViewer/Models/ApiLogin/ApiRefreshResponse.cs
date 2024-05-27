using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventViewer.Models.ApiLogin
{
    public class ApiRefreshResponse<T>
    {
        public List<T> Data { get; set; } = new List<T>();
    }
}
