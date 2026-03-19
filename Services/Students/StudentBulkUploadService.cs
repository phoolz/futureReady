using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Apiary.Data;
using Apiary.Models.School;
using Apiary.Models.Students;

namespace Apiary.Services.Students
{
    public class StudentBulkUploadService : IStudentBulkUploadService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantProvider? _tenantProvider;

        private static readonly Regex EmailRegex = new(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public StudentBulkUploadService(ApplicationDbContext context, ITenantProvider? tenantProvider = null)
        {
            _context = context;
            _tenantProvider = tenantProvider;
        }

        public async Task<BulkUploadResultModel> ProcessCsvAsync(Stream csvStream, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            if (!tenantId.HasValue)
                throw new InvalidOperationException("Tenant must be known when uploading students.");

            var result = new BulkUploadResultModel();
            var lines = new List<string>();

            using (var reader = new StreamReader(csvStream))
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lines.Add(line);
                }
            }

            if (lines.Count == 0)
            {
                result.RowResults.Add(new BulkUploadRowResult
                {
                    LineNumber = 1,
                    Status = BulkUploadRowStatus.Error,
                    Message = "CSV file is empty"
                });
                result.ErrorCount = 1;
                return result;
            }

            // Parse header row
            var headerLine = lines[0];
            var headers = ParseCsvLine(headerLine);
            var columnMap = BuildColumnMap(headers);

            if (!columnMap.ContainsKey("email"))
            {
                result.RowResults.Add(new BulkUploadRowResult
                {
                    LineNumber = 1,
                    Status = BulkUploadRowStatus.Error,
                    Message = "CSV must contain an 'Email' column"
                });
                result.ErrorCount = 1;
                return result;
            }

            // Check row limit
            var dataLines = lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            if (dataLines.Count > 500)
            {
                result.RowResults.Add(new BulkUploadRowResult
                {
                    LineNumber = 1,
                    Status = BulkUploadRowStatus.Error,
                    Message = $"CSV contains {dataLines.Count} rows. Maximum allowed is 500."
                });
                result.ErrorCount = 1;
                return result;
            }

            // Get existing emails in tenant (case-insensitive)
            var existingEmails = await _context.Set<Student>()
                .Where(s => s.TenantId == tenantId.Value && s.Email != null)
                .Select(s => s.Email!.ToLower())
                .ToListAsync();
            var existingEmailSet = new HashSet<string>(existingEmails);

            // Track emails in this upload to detect duplicates within the file
            var uploadedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            result.TotalRows = dataLines.Count;

            for (int i = 0; i < dataLines.Count; i++)
            {
                var lineNumber = i + 2; // Account for header and 1-based indexing
                var line = dataLines[i];
                var rowResult = await ProcessRowAsync(
                    line, lineNumber, columnMap, tenantId.Value,
                    existingEmailSet, uploadedEmails);

                result.RowResults.Add(rowResult);

                switch (rowResult.Status)
                {
                    case BulkUploadRowStatus.Success:
                        result.SuccessCount++;
                        break;
                    case BulkUploadRowStatus.Skipped:
                        result.SkippedCount++;
                        break;
                    case BulkUploadRowStatus.Error:
                        result.ErrorCount++;
                        break;
                }
            }

            return result;
        }

        private async Task<BulkUploadRowResult> ProcessRowAsync(
            string line,
            int lineNumber,
            Dictionary<string, int> columnMap,
            Guid tenantId,
            HashSet<string> existingEmails,
            HashSet<string> uploadedEmails)
        {
            var values = ParseCsvLine(line);
            var rowResult = new BulkUploadRowResult { LineNumber = lineNumber };

            // Get email (required)
            var email = GetColumnValue(values, columnMap, "email")?.Trim();
            rowResult.Email = email ?? "";

            if (string.IsNullOrWhiteSpace(email))
            {
                rowResult.Status = BulkUploadRowStatus.Error;
                rowResult.Message = "Email is required";
                return rowResult;
            }

            if (!EmailRegex.IsMatch(email))
            {
                rowResult.Status = BulkUploadRowStatus.Error;
                rowResult.Message = "Invalid email format";
                return rowResult;
            }

            // Check for duplicates
            var emailLower = email.ToLower();
            if (existingEmails.Contains(emailLower))
            {
                rowResult.Status = BulkUploadRowStatus.Skipped;
                rowResult.Message = "Student with this email already exists";
                return rowResult;
            }

            if (uploadedEmails.Contains(email))
            {
                rowResult.Status = BulkUploadRowStatus.Skipped;
                rowResult.Message = "Duplicate email in this upload";
                return rowResult;
            }

            // Parse optional fields
            var firstName = GetColumnValue(values, columnMap, "firstname")?.Trim();
            var lastName = GetColumnValue(values, columnMap, "lastname")?.Trim();

            // Use email prefix if names not provided
            if (string.IsNullOrWhiteSpace(firstName))
            {
                var atIndex = email.IndexOf('@');
                firstName = atIndex > 0 ? email.Substring(0, atIndex) : email;
            }
            if (string.IsNullOrWhiteSpace(lastName))
            {
                lastName = "(from import)";
            }

            var student = new Student
            {
                TenantId = tenantId,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PreferredName = GetColumnValue(values, columnMap, "preferredname")?.Trim(),
                StudentNumber = GetColumnValue(values, columnMap, "studentnumber")?.Trim(),
                Phone = GetColumnValue(values, columnMap, "phone")?.Trim(),
                StudentType = GetColumnValue(values, columnMap, "studenttype")?.Trim(),
                YearLevel = GetColumnValue(values, columnMap, "yearlevel")?.Trim(),
                MedicareNumber = GetColumnValue(values, columnMap, "medicarenumber")?.Trim()
            };

            // Parse DateOfBirth
            var dobStr = GetColumnValue(values, columnMap, "dateofbirth")?.Trim();
            if (!string.IsNullOrWhiteSpace(dobStr))
            {
                if (DateOnly.TryParseExact(dobStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dob))
                {
                    student.DateOfBirth = dob;
                }
            }

            // Parse GraduationYear
            var gradYearStr = GetColumnValue(values, columnMap, "graduationyear")?.Trim();
            if (!string.IsNullOrWhiteSpace(gradYearStr))
            {
                if (int.TryParse(gradYearStr, out var gradYear))
                {
                    student.GraduationYear = gradYear;
                }
            }

            try
            {
                _context.Add(student);
                await _context.SaveChangesAsync();

                uploadedEmails.Add(email);
                existingEmails.Add(emailLower);

                rowResult.Status = BulkUploadRowStatus.Success;
                rowResult.Message = $"Created: {student.FullName}";
            }
            catch (Exception ex)
            {
                rowResult.Status = BulkUploadRowStatus.Error;
                rowResult.Message = $"Database error: {ex.Message}";
            }

            return rowResult;
        }

        private static Dictionary<string, int> BuildColumnMap(List<string> headers)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Count; i++)
            {
                var header = headers[i].Trim().ToLower().Replace(" ", "").Replace("_", "");
                if (!map.ContainsKey(header))
                {
                    map[header] = i;
                }
            }
            return map;
        }

        private static string? GetColumnValue(List<string> values, Dictionary<string, int> columnMap, string columnName)
        {
            if (columnMap.TryGetValue(columnName, out var index) && index < values.Count)
            {
                var value = values[index];
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
            return null;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var values = new List<string>();
            var current = "";
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            // Escaped quote
                            current += '"';
                            i++;
                        }
                        else
                        {
                            // End of quoted field
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        current += c;
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (c == ',')
                    {
                        values.Add(current);
                        current = "";
                    }
                    else
                    {
                        current += c;
                    }
                }
            }

            values.Add(current);
            return values;
        }
    }
}
