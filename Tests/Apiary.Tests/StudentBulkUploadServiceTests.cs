using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Apiary.Data;
using Apiary.Models.School;
using Apiary.Models.Students;
using Apiary.Services;
using Apiary.Services.Students;

namespace Apiary.Tests
{
    public class StudentBulkUploadServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
        private readonly StudentBulkUploadService _service;
        private readonly Guid _tenantId = Guid.NewGuid();
        private readonly FakeTenantProvider _tenantProvider;

        public StudentBulkUploadServiceTests()
        {
            var userProvider = new FakeUserProvider();
            _tenantProvider = new FakeTenantProvider(_tenantId);
            (_context, _connection) = TestDbContextFactory.CreateSqliteInMemoryContext(userProvider, _tenantProvider);
            _service = new StudentBulkUploadService(_context, _tenantProvider);
        }

        public void Dispose()
        {
            _context?.Dispose();
            _connection?.Dispose();
        }

        #region Helper Methods

        private static MemoryStream CreateCsvStream(string csvContent)
        {
            var bytes = Encoding.UTF8.GetBytes(csvContent);
            return new MemoryStream(bytes);
        }

        private Student CreateTestStudent(string email, Guid? tenantId = null)
        {
            return new Student
            {
                Id = Guid.NewGuid(),
                FirstName = "Existing",
                LastName = "Student",
                Email = email,
                TenantId = tenantId ?? _tenantId
            };
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task ProcessCsvAsync_EmptyFile_ReturnsError()
        {
            // Arrange
            using var stream = CreateCsvStream("");

            // Act
            var result = await _service.ProcessCsvAsync(stream, _tenantId);

            // Assert
            Assert.Equal(1, result.ErrorCount);
            Assert.Single(result.RowResults);
            Assert.Equal(BulkUploadRowStatus.Error, result.RowResults[0].Status);
            Assert.Contains("empty", result.RowResults[0].Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ProcessCsvAsync_MissingEmailColumn_ReturnsError()
        {
            // Arrange
            var csv = "FirstName,LastName\nJohn,Doe";
            using var stream = CreateCsvStream(csv);

            // Act
            var result = await _service.ProcessCsvAsync(stream, _tenantId);

            // Assert
            Assert.Equal(1, result.ErrorCount);
            Assert.Single(result.RowResults);
            Assert.Equal(BulkUploadRowStatus.Error, result.RowResults[0].Status);
            Assert.Contains("Email", result.RowResults[0].Message);
        }

        [Fact]
        public async Task ProcessCsvAsync_ExceedsRowLimit_ReturnsError()
        {
            // Arrange
            var sb = new StringBuilder("Email\n");
            for (int i = 0; i < 501; i++)
            {
                sb.AppendLine($"student{i}@test.com");
            }
            using var stream = CreateCsvStream(sb.ToString());

            // Act
            var result = await _service.ProcessCsvAsync(stream, _tenantId);

            // Assert
            Assert.Equal(1, result.ErrorCount);
            Assert.Single(result.RowResults);
            Assert.Equal(BulkUploadRowStatus.Error, result.RowResults[0].Status);
            Assert.Contains("501", result.RowResults[0].Message);
            Assert.Contains("500", result.RowResults[0].Message);
        }

        [Fact]
        public async Task ProcessCsvAsync_NoTenant_ThrowsInvalidOperationException()
        {
            // Arrange
            var serviceWithoutTenant = new StudentBulkUploadService(_context, null);
            var csv = "Email\ntest@test.com";
            using var stream = CreateCsvStream(csv);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => serviceWithoutTenant.ProcessCsvAsync(stream, null));
            Assert.Contains("Tenant", exception.Message);
        }

        #endregion

        #region Email Validation Tests

        [Fact]
        public async Task ProcessCsvAsync_MissingEmail_ReturnsError()
        {
            // Arrange
            var csv = "Email,FirstName\n,John";
            using var stream = CreateCsvStream(csv);

            // Act
            var result = await _service.ProcessCsvAsync(stream, _tenantId);

            // Assert
            Assert.Equal(1, result.ErrorCount);
            Assert.Equal(BulkUploadRowStatus.Error, result.RowResults[0].Status);
            Assert.Contains("required", result.RowResults[0].Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ProcessCsvAsync_InvalidEmailFormat_ReturnsError()
        {
            // Arrange
            var csv = "Email\nnot-an-email";
            using var stream = CreateCsvStream(csv);

            // Act
            var result = await _service.ProcessCsvAsync(stream, _tenantId);

            // Assert
            Assert.Equal(1, result.ErrorCount);
            Assert.Equal(BulkUploadRowStatus.Error, result.RowResults[0].Status);
            Assert.Contains("Invalid email", result.RowResults[0].Message);
        }

        #endregion

        #region Duplicate Handling Tests

        [Fact]
        public async Task ProcessCsvAsync_DuplicateEmailInDatabase_SkipsRow()
        {
            // Arrange
            var existingStudent = CreateTestStudent("existing@test.com");
            _context.Students.Add(existingStudent);
            await _context.SaveChangesAsync();

            var csv = "Email\nexisting@test.com";
            using var stream = CreateCsvStream(csv);

            // Act
            var result = await _service.ProcessCsvAsync(stream, _tenantId);

            // Assert
            Assert.Equal(1, result.SkippedCount);
            Assert.Equal(BulkUploadRowStatus.Skipped, result.RowResults[0].Status);
            Assert.Contains("already exists", result.RowResults[0].Message);
        }

        [Fact]
        public async Task ProcessCsvAsync_DuplicateEmailInSameUpload_SkipsSecond()
        {
            // Arrange
            var csv = "Email\ntest@test.com\ntest@test.com";
            using var stream = CreateCsvStream(csv);

            // Act
            var result = await _service.ProcessCsvAsync(stream, _tenantId);

            // Assert
            Assert.Equal(1, result.SuccessCount);
            Assert.Equal(1, result.SkippedCount);
            Assert.Equal(BulkUploadRowStatus.Success, result.RowResults[0].Status);
            Assert.Equal(BulkUploadRowStatus.Skipped, result.RowResults[1].Status);
            // After first row succeeds, its email is added to existingEmails, so second row gets "already exists" message
            Assert.Contains("already exists", result.RowResults[1].Message);
        }

        [Fact]
        public async Task ProcessCsvAsync_DuplicateEmailCaseInsensitive_SkipsRow()
        {
            // Arrange
            var existingStudent = CreateTestStudent("existing@test.com");
            _context.Students.Add(existingStudent);
            await _context.SaveChangesAsync();

            var csv = "Email\nEXISTING@TEST.COM";
            using var stream = CreateCsvStream(csv);

            // Act
            var result = await _service.ProcessCsvAsync(stream, _tenantId);

            // Assert
            Assert.Equal(1, result.SkippedCount);
            Assert.Equal(BulkUploadRowStatus.Skipped, result.RowResults[0].Status);
        }

        [Fact]
        public async Task ProcessCsvAsync_SameEmailDifferentTenant_CreatesStudent()
        {
            // Arrange
            var otherTenantId = Guid.NewGuid();
            var existingStudent = CreateTestStudent("student@test.com", otherTenantId);
            _context.Students.Add(existingStudent);
            await _context.SaveChangesAsync();

            var csv = "Email\nstudent@test.com";
            using var stream = CreateCsvStream(csv);

            // Act
            var result = await _service.ProcessCsvAsync(stream, _tenantId);

            // Assert
            Assert.Equal(1, result.SuccessCount);
            Assert.Equal(BulkUploadRowStatus.Success, result.RowResults[0].Status);

            var students = await _context.Students.Where(s => s.Email == "student@test.com").ToListAsync();
            Assert.Equal(2, students.Count);
        }

        #endregion

        #region Success Cases

        [Fact]
        public async Task ProcessCsvAsync_ValidEmailOnly_CreatesStudentWithDefaults()
        {
            // Arrange
            var csv = "Email\njohn.doe@test.com";
            using var stream = CreateCsvStream(csv);

            // Act
            var result = await _service.ProcessCsvAsync(stream, _tenantId);

            // Assert
            Assert.Equal(1, result.SuccessCount);
            Assert.Equal(BulkUploadRowStatus.Success, result.RowResults[0].Status);

            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == "john.doe@test.com");
            Assert.NotNull(student);
            Assert.Equal("john.doe", student!.FirstName);
            Assert.Equal("(from import)", student.LastName);
            Assert.Equal(_tenantId, student.TenantId);
        }

        [Fact]
        public async Task ProcessCsvAsync_AllFields_CreatesStudentWithAllData()
        {
            // Arrange
            var csv = "Email,FirstName,LastName,PreferredName,StudentNumber,Phone,StudentType,YearLevel,MedicareNumber,DateOfBirth,GraduationYear\n" +
                      "jane@test.com,Jane,Smith,Janey,STU001,0412345678,Full-time,12,1234567890,2005-03-15,2027";
            using var stream = CreateCsvStream(csv);

            // Act
            var result = await _service.ProcessCsvAsync(stream, _tenantId);

            // Assert
            Assert.Equal(1, result.SuccessCount);

            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == "jane@test.com");
            Assert.NotNull(student);
            Assert.Equal("Jane", student!.FirstName);
            Assert.Equal("Smith", student.LastName);
            Assert.Equal("Janey", student.PreferredName);
            Assert.Equal("STU001", student.StudentNumber);
            Assert.Equal("0412345678", student.Phone);
            Assert.Equal("Full-time", student.StudentType);
            Assert.Equal("12", student.YearLevel);
            Assert.Equal("1234567890", student.MedicareNumber);
            Assert.Equal(new DateOnly(2005, 3, 15), student.DateOfBirth);
            Assert.Equal(2027, student.GraduationYear);
        }

        [Fact]
        public async Task ProcessCsvAsync_MultipleRows_ProcessesAll()
        {
            // Arrange
            var csv = "Email,FirstName,LastName\n" +
                      "student1@test.com,Alice,Anderson\n" +
                      "student2@test.com,Bob,Brown\n" +
                      "student3@test.com,Carol,Chen";
            using var stream = CreateCsvStream(csv);

            // Act
            var result = await _service.ProcessCsvAsync(stream, _tenantId);

            // Assert
            Assert.Equal(3, result.TotalRows);
            Assert.Equal(3, result.SuccessCount);
            Assert.Equal(0, result.SkippedCount);
            Assert.Equal(0, result.ErrorCount);

            var students = await _context.Students.ToListAsync();
            Assert.Equal(3, students.Count);
        }

        #endregion

        #region Field Mapping Tests

        [Fact]
        public async Task ProcessCsvAsync_FirstName_MapsCorrectly()
        {
            // Arrange
            var csv = "Email,FirstName\ntest@test.com,Alexander";
            using var stream = CreateCsvStream(csv);

            // Act
            var result = await _service.ProcessCsvAsync(stream, _tenantId);

            // Assert
            Assert.Equal(1, result.SuccessCount);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == "test@test.com");
            Assert.NotNull(student);
            Assert.Equal("Alexander", student!.FirstName);
        }

        [Fact]
        public async Task ProcessCsvAsync_LastName_MapsCorrectly()
        {
            // Arrange
            var csv = "Email,LastName\ntest@test.com,Montgomery";
            using var stream = CreateCsvStream(csv);

            // Act
            var result = await _service.ProcessCsvAsync(stream, _tenantId);

            // Assert
            Assert.Equal(1, result.SuccessCount);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == "test@test.com");
            Assert.NotNull(student);
            Assert.Equal("Montgomery", student!.LastName);
        }

        [Fact]
        public async Task ProcessCsvAsync_PreferredName_MapsCorrectly()
        {
            // Arrange
            var csv = "Email,PreferredName\ntest@test.com,Alex";
            using var stream = CreateCsvStream(csv);

            // Act
            var result = await _service.ProcessCsvAsync(stream, _tenantId);

            // Assert
            Assert.Equal(1, result.SuccessCount);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == "test@test.com");
            Assert.NotNull(student);
            Assert.Equal("Alex", student!.PreferredName);
        }

        [Fact]
        public async Task ProcessCsvAsync_StudentNumber_MapsCorrectly()
        {
            // Arrange
            var csv = "Email,StudentNumber\ntest@test.com,STU12345";
            using var stream = CreateCsvStream(csv);

            // Act
            var result = await _service.ProcessCsvAsync(stream, _tenantId);

            // Assert
            Assert.Equal(1, result.SuccessCount);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == "test@test.com");
            Assert.NotNull(student);
            Assert.Equal("STU12345", student!.StudentNumber);
        }

        [Fact]
        public async Task ProcessCsvAsync_Phone_MapsCorrectly()
        {
            // Arrange
            var csv = "Email,Phone\ntest@test.com,0412345678";
            using var stream = CreateCsvStream(csv);

            // Act
            var result = await _service.ProcessCsvAsync(stream, _tenantId);

            // Assert
            Assert.Equal(1, result.SuccessCount);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == "test@test.com");
            Assert.NotNull(student);
            Assert.Equal("0412345678", student!.Phone);
        }

        [Fact]
        public async Task ProcessCsvAsync_StudentType_MapsCorrectly()
        {
            // Arrange
            var csv = "Email,StudentType\ntest@test.com,Part-time";
            using var stream = CreateCsvStream(csv);

            // Act
            var result = await _service.ProcessCsvAsync(stream, _tenantId);

            // Assert
            Assert.Equal(1, result.SuccessCount);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == "test@test.com");
            Assert.NotNull(student);
            Assert.Equal("Part-time", student!.StudentType);
        }

        [Fact]
        public async Task ProcessCsvAsync_YearLevel_MapsCorrectly()
        {
            // Arrange
            var csv = "Email,YearLevel\ntest@test.com,11";
            using var stream = CreateCsvStream(csv);

            // Act
            var result = await _service.ProcessCsvAsync(stream, _tenantId);

            // Assert
            Assert.Equal(1, result.SuccessCount);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == "test@test.com");
            Assert.NotNull(student);
            Assert.Equal("11", student!.YearLevel);
        }

        [Fact]
        public async Task ProcessCsvAsync_MedicareNumber_MapsCorrectly()
        {
            // Arrange
            var csv = "Email,MedicareNumber\ntest@test.com,2123456701";
            using var stream = CreateCsvStream(csv);

            // Act
            var result = await _service.ProcessCsvAsync(stream, _tenantId);

            // Assert
            Assert.Equal(1, result.SuccessCount);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == "test@test.com");
            Assert.NotNull(student);
            Assert.Equal("2123456701", student!.MedicareNumber);
        }

        [Fact]
        public async Task ProcessCsvAsync_DateOfBirth_ParsesCorrectly()
        {
            // Arrange
            var csv = "Email,DateOfBirth\ntest@test.com,2006-07-25";
            using var stream = CreateCsvStream(csv);

            // Act
            var result = await _service.ProcessCsvAsync(stream, _tenantId);

            // Assert
            Assert.Equal(1, result.SuccessCount);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == "test@test.com");
            Assert.NotNull(student);
            Assert.Equal(new DateOnly(2006, 7, 25), student!.DateOfBirth);
        }

        [Fact]
        public async Task ProcessCsvAsync_DateOfBirth_InvalidFormat_IgnoresField()
        {
            // Arrange
            var csv = "Email,DateOfBirth\ntest@test.com,25/07/2006";
            using var stream = CreateCsvStream(csv);

            // Act
            var result = await _service.ProcessCsvAsync(stream, _tenantId);

            // Assert
            Assert.Equal(1, result.SuccessCount);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == "test@test.com");
            Assert.NotNull(student);
            Assert.Null(student!.DateOfBirth);
        }

        [Fact]
        public async Task ProcessCsvAsync_GraduationYear_ParsesCorrectly()
        {
            // Arrange
            var csv = "Email,GraduationYear\ntest@test.com,2028";
            using var stream = CreateCsvStream(csv);

            // Act
            var result = await _service.ProcessCsvAsync(stream, _tenantId);

            // Assert
            Assert.Equal(1, result.SuccessCount);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == "test@test.com");
            Assert.NotNull(student);
            Assert.Equal(2028, student!.GraduationYear);
        }

        [Fact]
        public async Task ProcessCsvAsync_GraduationYear_InvalidFormat_IgnoresField()
        {
            // Arrange
            var csv = "Email,GraduationYear\ntest@test.com,twenty-twenty-eight";
            using var stream = CreateCsvStream(csv);

            // Act
            var result = await _service.ProcessCsvAsync(stream, _tenantId);

            // Assert
            Assert.Equal(1, result.SuccessCount);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == "test@test.com");
            Assert.NotNull(student);
            Assert.Null(student!.GraduationYear);
        }

        #endregion

        #region CSV Parsing Tests

        [Fact]
        public async Task ProcessCsvAsync_QuotedFields_ParsesCorrectly()
        {
            // Arrange
            var csv = "Email,FirstName,LastName\ntest@test.com,\"Smith, Jr.\",O'Brien";
            using var stream = CreateCsvStream(csv);

            // Act
            var result = await _service.ProcessCsvAsync(stream, _tenantId);

            // Assert
            Assert.Equal(1, result.SuccessCount);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == "test@test.com");
            Assert.NotNull(student);
            Assert.Equal("Smith, Jr.", student!.FirstName);
            Assert.Equal("O'Brien", student.LastName);
        }

        [Fact]
        public async Task ProcessCsvAsync_EscapedQuotes_ParsesCorrectly()
        {
            // Arrange
            var csv = "Email,FirstName\ntest@test.com,\"The \"\"Great\"\" One\"";
            using var stream = CreateCsvStream(csv);

            // Act
            var result = await _service.ProcessCsvAsync(stream, _tenantId);

            // Assert
            Assert.Equal(1, result.SuccessCount);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == "test@test.com");
            Assert.NotNull(student);
            Assert.Equal("The \"Great\" One", student!.FirstName);
        }

        [Fact]
        public async Task ProcessCsvAsync_HeaderVariations_MapsCorrectly()
        {
            // Arrange - Test "First Name" with space
            var csv1 = "Email,First Name\ntest1@test.com,Alice";
            using var stream1 = CreateCsvStream(csv1);

            var result1 = await _service.ProcessCsvAsync(stream1, _tenantId);
            Assert.Equal(1, result1.SuccessCount);
            var student1 = await _context.Students.FirstOrDefaultAsync(s => s.Email == "test1@test.com");
            Assert.Equal("Alice", student1!.FirstName);

            // Arrange - Test "first_name" with underscore
            var csv2 = "Email,first_name\ntest2@test.com,Bob";
            using var stream2 = CreateCsvStream(csv2);

            var result2 = await _service.ProcessCsvAsync(stream2, _tenantId);
            Assert.Equal(1, result2.SuccessCount);
            var student2 = await _context.Students.FirstOrDefaultAsync(s => s.Email == "test2@test.com");
            Assert.Equal("Bob", student2!.FirstName);

            // Arrange - Test "FirstName" camelCase
            var csv3 = "Email,FirstName\ntest3@test.com,Carol";
            using var stream3 = CreateCsvStream(csv3);

            var result3 = await _service.ProcessCsvAsync(stream3, _tenantId);
            Assert.Equal(1, result3.SuccessCount);
            var student3 = await _context.Students.FirstOrDefaultAsync(s => s.Email == "test3@test.com");
            Assert.Equal("Carol", student3!.FirstName);
        }

        #endregion

        #region Mixed Results Tests

        [Fact]
        public async Task ProcessCsvAsync_MixedResults_ReturnsCorrectCounts()
        {
            // Arrange - Add existing student for skip
            var existingStudent = CreateTestStudent("existing@test.com");
            _context.Students.Add(existingStudent);
            await _context.SaveChangesAsync();

            var csv = "Email,FirstName,LastName\n" +
                      "new@test.com,New,Student\n" +           // Success
                      "existing@test.com,Existing,Student\n" + // Skip (exists)
                      "invalid-email,Bad,Email\n" +            // Error (invalid email)
                      ",Missing,Email\n" +                     // Error (missing email)
                      "another@test.com,Another,Student";      // Success
            using var stream = CreateCsvStream(csv);

            // Act
            var result = await _service.ProcessCsvAsync(stream, _tenantId);

            // Assert
            Assert.Equal(5, result.TotalRows);
            Assert.Equal(2, result.SuccessCount);
            Assert.Equal(1, result.SkippedCount);
            Assert.Equal(2, result.ErrorCount);

            Assert.Equal(5, result.RowResults.Count);
            Assert.Equal(BulkUploadRowStatus.Success, result.RowResults[0].Status);
            Assert.Equal(BulkUploadRowStatus.Skipped, result.RowResults[1].Status);
            Assert.Equal(BulkUploadRowStatus.Error, result.RowResults[2].Status);
            Assert.Equal(BulkUploadRowStatus.Error, result.RowResults[3].Status);
            Assert.Equal(BulkUploadRowStatus.Success, result.RowResults[4].Status);
        }

        #endregion

        #region Test Helpers

        private class FakeUserProvider : IUserProvider
        {
            public string? GetCurrentUsername() => "test-user";
        }

        private class FakeTenantProvider : ITenantProvider
        {
            private readonly Guid _id;
            public FakeTenantProvider(Guid? tenantId = null) => _id = tenantId ?? Guid.NewGuid();
            public Guid? GetCurrentTenantId() => _id;
        }

        #endregion
    }
}
