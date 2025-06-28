namespace StudentsAttendance
{
    public class AttendanceRequest
    {
        public required string DoctorName { get; set; }
        public required string SubjectName { get; set; }
        public required string SectionStartTime { get; set; }  // e.g., "08:00"
        public required string StudentId { get; set; }
        public required string StudentName { get; set; }
    }
}
