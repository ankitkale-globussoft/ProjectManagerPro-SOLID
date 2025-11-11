using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectManagerPro_SOLID.Models
{
    public class Comment
    {
        public string Id { get; set; }

        public string UserName { get; set; }

        public string Message { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string TaskItemId { get; set; }

        public TaskItem TaskItem { get; set; }

    }
    public class ProjectComment
    {
        public string ID { get; set; }
        List<Comment> Comments { get; set; }
    }
}
