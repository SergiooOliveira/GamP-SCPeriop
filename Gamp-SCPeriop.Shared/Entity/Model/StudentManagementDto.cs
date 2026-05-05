using System;
using System.Collections.Generic;
using System.Text;

namespace GamP_SCPeriop.Shared.Entity.Model
{
    public class StudentManagementDto
    {
        public int StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // For the progress bar column
        public int OverallProgress { get; set; }

        // For the "Há 2 horas" column
        public DateTime? LastAccess { get; set; }

        // For the colorful tags in the table
        public List<PathwayTagDto> ActivePathways { get; set; } = new List<PathwayTagDto>();
    }

    // A tiny helper DTO just for the tags
    public class PathwayTagDto
    {
        public int PathwayId { get; set; }
        public string Title { get; set; } = string.Empty;
    }
}
