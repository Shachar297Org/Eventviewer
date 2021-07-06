using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventViewer.Models
{
    public class JqueryDataTablesParameters
    {
        public int Draw { get; set; }
        public DTColumn[] Columns { get; set; }
        public DTOrder[] Order { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public DTSearch Search { get; set; }
        public string SortOrder { get; }
        public IEnumerable<string> AdditionalValues { get; set; }
    }

    public class DTSearch
    {
        public string Value { get; set; }
        public bool Regex { get; set; }
    }

    public enum DTOrderDir
    {
        ASC = 0,
        DESC = 1
    }

    public class DTOrder
    {     
        public int Column { get; set; }
        public DTOrderDir Dir { get; set; }
    }

    public class DTColumn
    {
        public string Data { get; set; }
        public string Name { get; set; }
        public bool Searchable { get; set; }
        public bool Orderable { get; set; }
        public DTSearch Search { get; set; }
    }
}
