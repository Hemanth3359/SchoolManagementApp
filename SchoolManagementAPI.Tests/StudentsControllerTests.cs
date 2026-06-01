using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementAPI.Controllers;
using SchoolManagementAPI.Data;
using SchoolManagementAPI.Models;
using System.Security.Claims;

namespace SchoolManagementAPI.Tests
{
    public class StudentsControllerTests
    {
        private AppDbContext GetInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new AppDbContext(options);
        }

        private StudentsController CreateController(AppDbContext context, string role = "Teacher")
        {
            var controller = new StudentsController(context);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Name, "TestUser")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
            return controller;
        }

        // Test 1: GetAll returns empty list when no students
        [Fact]
        public async Task GetAll_ReturnsEmptyList_WhenNoStudents()
        {
            var context = GetInMemoryContext("Test_GetAll_Empty");
            var controller = CreateController(context);

            var result = await controller.GetAll();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var students = Assert.IsType<List<Student>>(okResult.Value);
            Assert.Empty(students);
        }

        // Test 2: GetAll returns all students
        [Fact]
        public async Task GetAll_ReturnsAllStudents()
        {
            var context = GetInMemoryContext("Test_GetAll_WithData");
            context.Students.AddRange(
                new Student { Name = "Alice", Subject = "Math", Grade = 90 },
                new Student { Name = "Bob", Subject = "Science", Grade = 85 }
            );
            await context.SaveChangesAsync();

            var controller = CreateController(context);
            var result = await controller.GetAll();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var students = Assert.IsType<List<Student>>(okResult.Value);
            Assert.Equal(2, students.Count);
        }

        // Test 3: Create adds a new student successfully
        [Fact]
        public async Task Create_AddsStudent_ReturnsCreated()
        {
            var context = GetInMemoryContext("Test_Create");
            var controller = CreateController(context, "Teacher");

            var newStudent = new Student { Name = "Charlie", Subject = "English", Grade = 88 };
            var result = await controller.Create(newStudent);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var student = Assert.IsType<Student>(createdResult.Value);
            Assert.Equal("Charlie", student.Name);
            Assert.Equal(1, await context.Students.CountAsync());
        }

        // Test 4: GetById returns correct student
        [Fact]
        public async Task GetById_ReturnsStudent_WhenExists()
        {
            var context = GetInMemoryContext("Test_GetById");
            var student = new Student { Name = "Diana", Subject = "History", Grade = 92 };
            context.Students.Add(student);
            await context.SaveChangesAsync();

            var controller = CreateController(context);
            var result = await controller.GetById(student.Id);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<Student>(okResult.Value);
            Assert.Equal("Diana", returned.Name);
        }

        // Test 5: GetById returns NotFound for invalid id
        [Fact]
        public async Task GetById_ReturnsNotFound_WhenNotExists()
        {
            var context = GetInMemoryContext("Test_GetById_NotFound");
            var controller = CreateController(context);

            var result = await controller.GetById(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        // Test 6: Update modifies student correctly
        [Fact]
        public async Task Update_ModifiesStudent_ReturnsOk()
        {
            var context = GetInMemoryContext("Test_Update");
            var student = new Student { Name = "Eve", Subject = "Art", Grade = 75 };
            context.Students.Add(student);
            await context.SaveChangesAsync();

            var controller = CreateController(context, "Teacher");
            var updated = new Student { Name = "Eve Updated", Subject = "Art", Grade = 80 };
            var result = await controller.Update(student.Id, updated);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<Student>(okResult.Value);
            Assert.Equal("Eve Updated", returned.Name);
            Assert.Equal(80, returned.Grade);
        }

        // Test 7: Delete removes student successfully
        [Fact]
        public async Task Delete_RemovesStudent_ReturnsOk()
        {
            var context = GetInMemoryContext("Test_Delete");
            var student = new Student { Name = "Frank", Subject = "PE", Grade = 70 };
            context.Students.Add(student);
            await context.SaveChangesAsync();

            var controller = CreateController(context, "Teacher");
            var result = await controller.Delete(student.Id);

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, await context.Students.CountAsync());
        }

        // Test 8: Update returns NotFound for invalid id
        [Fact]
        public async Task Update_ReturnsNotFound_WhenStudentDoesNotExist()
        {
            var context = GetInMemoryContext("Test_Update_NotFound");
            var controller = CreateController(context, "Teacher");

            var result = await controller.Update(999, new Student { Name = "Ghost" });

            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}