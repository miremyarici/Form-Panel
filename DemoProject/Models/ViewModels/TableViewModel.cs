using System.Collections.Generic;
using DemoProject.Models;

namespace DemoProject.Models.ViewModels
{
    public class TableViewModel
    {
        public List<ApplicationForm>? Applications { get; set; }
        public List<Reference>? Reference { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
