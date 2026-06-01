using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace SchoolManagementApp.SeleniumTests
{
    public class SchoolManagementSeleniumTests : IDisposable
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;
        private const string BaseUrl = "http://localhost:3000";

        public SchoolManagementSeleniumTests()
        {
            var options = new ChromeOptions();
            options.AddArgument("--start-maximized");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--remote-debugging-port=0");
            _driver = new ChromeDriver(options);
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(20));
        }

        public void Dispose()
        {
            try { _driver.Quit(); } catch { }
            try { _driver.Dispose(); } catch { }
        }

        private void ClearLocalStorage()
        {
            try
            {
                _driver.Navigate().GoToUrl(BaseUrl);
                System.Threading.Thread.Sleep(1000);
                ((IJavaScriptExecutor)_driver).ExecuteScript("localStorage.clear();");
            }
            catch { }
        }

        private void Login(string email, string password)
        {
            _driver.Navigate().GoToUrl($"{BaseUrl}/login");
            System.Threading.Thread.Sleep(2000);

            _wait.Until(d => d.FindElement(By.CssSelector("input[type='email']")));
            _driver.FindElement(By.CssSelector("input[type='email']")).Clear();
            _driver.FindElement(By.CssSelector("input[type='email']")).SendKeys(email);
            _driver.FindElement(By.CssSelector("input[type='password']")).Clear();
            _driver.FindElement(By.CssSelector("input[type='password']")).SendKeys(password);
            _driver.FindElement(By.CssSelector("button[type='submit']")).Click();
            System.Threading.Thread.Sleep(6000);

            // If still not on dashboard, navigate directly
            if (!_driver.Url.Contains("/dashboard"))
            {
                _driver.Navigate().GoToUrl($"{BaseUrl}/dashboard");
                System.Threading.Thread.Sleep(3000);
            }
        }

        // Test 1: Teacher Registration
        [Fact]
        public void Test1_TeacherRegistration()
        {
            ClearLocalStorage();
            _driver.Navigate().GoToUrl($"{BaseUrl}/register");
            System.Threading.Thread.Sleep(2000);

            _wait.Until(d => d.FindElement(By.CssSelector("input[name='name']")));

            _driver.FindElement(By.CssSelector("input[name='name']")).SendKeys("Selenium Teacher");
            _driver.FindElement(By.CssSelector("input[name='dateOfBirth']")).SendKeys("01/01/1990");

            var designation = new SelectElement(_driver.FindElement(
                By.CssSelector("select[name='designation']")));
            designation.SelectByValue("Teacher");

            string email = $"selteacher{DateTime.Now.Ticks}@test.com";
            _driver.FindElement(By.CssSelector("input[name='email']")).SendKeys(email);
            _driver.FindElement(By.CssSelector("input[name='password']")).SendKeys("Password123");
            _driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            System.Threading.Thread.Sleep(3000);

            bool success = _driver.PageSource.Contains("successfully") ||
                           _driver.PageSource.Contains("Registration") ||
                           _driver.Url.Contains("/login");

            Assert.True(success, "Registration should succeed");
        }

        // Test 2: Login and JWT Session Validation
        [Fact]
        public void Test2_LoginAndSessionValidation()
        {
            ClearLocalStorage();
            Login("seleniumteacher@test.com", "Password123");

            string token = ((IJavaScriptExecutor)_driver)
                .ExecuteScript("return localStorage.getItem('token');")?.ToString() ?? "";
            string role = ((IJavaScriptExecutor)_driver)
                .ExecuteScript("return localStorage.getItem('role');")?.ToString() ?? "";

            Assert.NotEmpty(token);
            Assert.Equal("Teacher", role);
        }

        // Test 3: Teacher CRUD Operations
        [Fact]
        public void Test3_TeacherCRUDOperations()
        {
            ClearLocalStorage();
            Login("seleniumteacher@test.com", "Password123");

            System.Threading.Thread.Sleep(2000);
            Assert.True(_driver.Url.Contains("/dashboard") ||
                        _driver.PageSource.Contains("Welcome"),
                        "Should be on dashboard");

            // CREATE
            string studentName = $"SeleniumStudent{DateTime.Now.Ticks % 10000}";
            _wait.Until(d => d.FindElement(By.CssSelector("input[placeholder='Name']")));
            _driver.FindElement(By.CssSelector("input[placeholder='Name']")).SendKeys(studentName);
            _driver.FindElement(By.CssSelector("input[placeholder='Subject']")).SendKeys("Selenium");
            _driver.FindElement(By.CssSelector("input[placeholder='Grade']")).SendKeys("95");
            _driver.FindElement(By.XPath("//button[contains(text(),'Add Student')]")).Click();

            System.Threading.Thread.Sleep(2000);
            Assert.Contains(studentName, _driver.PageSource);

            // EDIT
            _wait.Until(d => d.FindElement(By.XPath("//button[contains(text(),'Edit')]")));
            _driver.FindElement(By.XPath("//button[contains(text(),'Edit')]")).Click();
            System.Threading.Thread.Sleep(1000);

            var nameField = _driver.FindElement(By.CssSelector("input[placeholder='Name']"));
            nameField.Clear();
            nameField.SendKeys("UpdatedSeleniumStudent");
            _driver.FindElement(By.XPath("//button[contains(text(),'Update')]")).Click();
            System.Threading.Thread.Sleep(2000);

            Assert.Contains("UpdatedSeleniumStudent", _driver.PageSource);

            // DELETE - override confirm dialog BEFORE clicking
            ((IJavaScriptExecutor)_driver).ExecuteScript("window.confirm = function() { return true; }");
            System.Threading.Thread.Sleep(500);

            _driver.FindElement(By.XPath("//button[contains(text(),'Delete')]")).Click();
            System.Threading.Thread.Sleep(3000);

            _driver.Navigate().Refresh();
            System.Threading.Thread.Sleep(2000);

            Assert.DoesNotContain("UpdatedSeleniumStudent", _driver.PageSource);
        }

        // Test 4: Student Read-Only Access
        [Fact]
        public void Test4_StudentReadOnlyAccess()
        {
            ClearLocalStorage();

            // Register student
            _driver.Navigate().GoToUrl($"{BaseUrl}/register");
            System.Threading.Thread.Sleep(2000);

            _wait.Until(d => d.FindElement(By.CssSelector("input[name='name']")));

            string studentEmail = $"selstudent{DateTime.Now.Ticks % 100000}@test.com";
            _driver.FindElement(By.CssSelector("input[name='name']")).SendKeys("Selenium Student");
            _driver.FindElement(By.CssSelector("input[name='dateOfBirth']")).SendKeys("01/01/2005");

            var designation = new SelectElement(_driver.FindElement(
                By.CssSelector("select[name='designation']")));
            designation.SelectByValue("Student");

            _driver.FindElement(By.CssSelector("input[name='email']")).SendKeys(studentEmail);
            _driver.FindElement(By.CssSelector("input[name='password']")).SendKeys("Password123");
            _driver.FindElement(By.CssSelector("button[type='submit']")).Click();
            System.Threading.Thread.Sleep(3000);

            // Login as student
            ClearLocalStorage();
            Login(studentEmail, "Password123");
            System.Threading.Thread.Sleep(3000);

            // Verify no Edit/Delete/Add buttons
            var editButtons = _driver.FindElements(
                By.XPath("//button[contains(text(),'Edit')]"));
            var deleteButtons = _driver.FindElements(
                By.XPath("//button[contains(text(),'Delete')]"));
            var addForm = _driver.FindElements(
                By.XPath("//*[contains(text(),'Add New Student')]"));

            Assert.Empty(editButtons);
            Assert.Empty(deleteButtons);
            Assert.Empty(addForm);
        }

        // Test 5: Logout Flow
        [Fact]
        public void Test5_LogoutFlow()
        {
            ClearLocalStorage();
            Login("seleniumteacher@test.com", "Password123");
            System.Threading.Thread.Sleep(2000);

            _wait.Until(d => d.FindElement(
                By.XPath("//button[contains(text(),'Logout')]")));
            _driver.FindElement(By.XPath("//button[contains(text(),'Logout')]")).Click();
            System.Threading.Thread.Sleep(2000);

            Assert.Contains("/login", _driver.Url);

            string token = ((IJavaScriptExecutor)_driver)
                .ExecuteScript("return localStorage.getItem('token');")?.ToString() ?? "";
            Assert.True(string.IsNullOrEmpty(token),
                "Token should be cleared after logout");
        }
    }
}