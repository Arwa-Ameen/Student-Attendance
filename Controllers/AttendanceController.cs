using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Globalization;

namespace StudentsAttendance.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttendanceController : ControllerBase
    {
        [HttpPost]
        public IActionResult RecordAttendance([FromBody] AttendanceRequest request)
        {
            // Validate time: only allow attendance within 15 minutes of section start
            DateTime sectionStart;
            if (!DateTime.TryParseExact(request.SectionStartTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out sectionStart))
            {
                return BadRequest("Invalid section start time format. Use HH:mm.");
            }

            

            // Determine Excel file path
            string safeSectionStartTime = request.SectionStartTime.Replace(":", "-");
            string fileName = $"Dr_{request.DoctorName}_{request.SubjectName}_{safeSectionStartTime}.xlsx";
            string filePath = Path.Combine("Data", fileName);
            // Ensure directory exists
            Directory.CreateDirectory("Data");
            ExcelPackage.License.SetNonCommercialOrganization("My Noncommercial organization");

            // Open or create Excel file
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                ExcelWorksheet worksheet;
                if (package.Workbook.Worksheets.Count == 0)
                {
                    // Create worksheet and header rows if the file is new
                    worksheet = package.Workbook.Worksheets.Add("Attendance");
                    worksheet.Cells[1, 1].Value = "رقم_الطالب";
                    worksheet.Cells[1, 2].Value = "اسم_الطالب";
                }
                else
                {
                    worksheet = package.Workbook.Worksheets[0];
                }

                // Define session column header
                string sessionHeader = $"Section_{DateTime.Now:yyyyMMdd}_{request.SectionStartTime}";
                int sessionColumn = 0;
                for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                {
                    if (worksheet.Cells[1, col].Text == sessionHeader)
                    {
                        sessionColumn = col;
                        break;
                    }
                }
                if (sessionColumn == 0)
                {
                    // New session: add header
                    sessionColumn = worksheet.Dimension.End.Column + 1;
                    worksheet.Cells[1, sessionColumn].Value = sessionHeader;
                }

                // Find student row (here we assume student IDs are in column 1)
                int studentRow = 0;
                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {
                    if (worksheet.Cells[row, 1].Text == request.StudentId)
                    {
                        studentRow = row;
                        break;
                    }
                }
                // If not found, add a new row for the student
                if (studentRow == 0)
                {
                    studentRow = worksheet.Dimension.End.Row + 1;
                    worksheet.Cells[studentRow, 1].Value = request.StudentId;
                    worksheet.Cells[studentRow, 2].Value = request.StudentName;
                }

                // Prevent double attendance in the session column
                
                // Mark attendance
                worksheet.Cells[studentRow, sessionColumn].Value = DateTime.Now.ToString("HH:mm:ss");

                // Save the Excel file
                package.Save();
            }

            return Ok("تم تسجيل الغياب بنجاح");
        }

        [HttpGet("students")]
        public IActionResult GetStudentLists(
           [FromQuery] string doctorName,
           [FromQuery] string subjectName,
           [FromQuery] string sectionStartTime)
        {
            // Make file-name safe (replace ':' with '-')
            string safeStart = sectionStartTime.Replace(":", "-");
            string fileName = $"Dr_{doctorName}_{subjectName}_{safeStart}.xlsx";
            string filePath = Path.Combine("Data", fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound($"File '{fileName}' not found.");

            // Ensure EPPlus knows your license
            ExcelPackage.License.SetNonCommercialOrganization("My Noncommercial organization");

            var ids = new List<string>();
            var names = new List<string>();

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var ws = package.Workbook.Worksheets[0];
                if (ws.Dimension == null)
                    return BadRequest("Excel file is empty.");

                int headerRow = 1;
                int idCol = -1;
                int nameCol = -1;

                // Locate the two columns
                for (int col = 1; col <= ws.Dimension.End.Column; col++)
                {
                    var txt = ws.Cells[headerRow, col].Text.Trim();
                    if (txt == "رقم_الطالب") idCol = col;
                    if (txt == "اسم_الطالب") nameCol = col;
                }

                if (idCol < 0 || nameCol < 0)
                    return BadRequest("Required headers 'رقم_الطالب' or 'اسم_الطالب' not found.");

                // Collect data from each row
                for (int row = headerRow + 1; row <= ws.Dimension.End.Row; row++)
                {
                    var sid = ws.Cells[row, idCol].Text;
                    var snm = ws.Cells[row, nameCol].Text;
                    if (!string.IsNullOrEmpty(sid)) ids.Add(sid);
                    if (!string.IsNullOrEmpty(snm)) names.Add(snm);
                }
            }

            var response = new
            {
                StudentIds = ids,
                StudentNames = names
            };

            return Ok(response);
        }
    }
}


