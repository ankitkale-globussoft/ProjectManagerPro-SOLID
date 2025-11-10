using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectManagerPro_SOLID.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ProjectID { get; set; }
        public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();

    }
}
