using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectManagerPro_SOLID.Enums;

namespace ProjectManagerPro_SOLID.Models
{
    public enum TaskStatus
    {
        ToDo,
        InProgress,
        InReview,
        Completed
    }

    public class TaskItem
    {
        public int Id { get; set; }

        public string Title { get; set; }
        public User AssignedTo { get; set; }

        public bool isNOtified { get; set; }

        public PriorityLevel Priority { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }

        public DateTime ModifiedDateTime { get; set; }

        public string Description { get; set; }

        public TaskStatus Status { get; set; } = TaskStatus.ToDo;

        public int ProjectId { get; set; }
        public string ProjectID { get; set; }
        public Project Project { get; set; }
        public string TaskID { get; set; }
        public List<Comment> Comments { get; set; }

        public string CreatedById { get; set; }
        public string CreatedByUsername { get; set; }
        public User CreatedBy { get; set; }
    }
}
