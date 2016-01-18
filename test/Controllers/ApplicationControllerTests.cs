using System.Dynamic;
using System.Threading.Tasks;
using System.Web.Http.Results;
using FileAuditManager.Controllers;
using FileAuditManager.Data;
using FileAuditManager.Data.Models;
using Moq;
using NUnit.Framework;

namespace test.Controllers
{
    [TestFixture]
    public class ApplicationControllerTests
    {
        private Mock<IApplicationRepository> applicationRepository;
        private ApplicationController applicationController;

        private Application existingApplication;
        

        [SetUp]
        public void Setup()
        {
            existingApplication = new Application
            {
                Name = "app",
                Enabled = true
            };
            applicationRepository = new Mock<IApplicationRepository>();
            applicationController = new ApplicationController(applicationRepository.Object);
        }

        [Test]
        public async Task ItSavesANewApplication()
        {
            Application savedApplication = null;
            applicationRepository.Setup(a => a.InsertApplicationAsync(It.IsAny<Application>()))
                .Callback((Application app) => { savedApplication = app; })
                .Returns(Task.CompletedTask);
            applicationRepository.Setup(a => a.GetApplicationAsync(It.IsAny<string>())).ReturnsAsync(null);

            var result = await applicationController.Post("newApp");
            Assert.That(result, Is.TypeOf<OkResult>());
            Assert.That(savedApplication.Name, Is.EqualTo("newApp"));
        }

        [Test]
        public async Task ItFailsIfApplicationAlreadyExists()
        {
            applicationRepository.Setup(a => a.GetApplicationAsync(existingApplication.Name)).ReturnsAsync(existingApplication);
            var result = await applicationController.Post(existingApplication.Name);
            Assert.That(result, Is.TypeOf<BadRequestErrorMessageResult>());
        }

        [Test]
        public async Task ItUpdatesApplicationEnabled()
        {
            Application savedApplication = null;
            applicationRepository.Setup(a => a.GetApplicationAsync(existingApplication.Name)).ReturnsAsync(existingApplication);
            applicationRepository.Setup(a => a.UpdateApplication(It.IsAny<Application>()))
                .Callback((Application app) => { savedApplication = app; })
                .Returns(Task.CompletedTask);

            var payload = new Application {Enabled = false};
            
            var result = await applicationController.Put(existingApplication.Name, payload);
            Assert.That(result, Is.TypeOf<OkResult>());
            Assert.That(savedApplication.Enabled, Is.False);
        }
    }
}
