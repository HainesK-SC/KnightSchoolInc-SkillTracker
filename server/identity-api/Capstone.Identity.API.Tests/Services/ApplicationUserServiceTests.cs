using Capstone.Identity.API.Common;
using Capstone.Identity.API.Data;
using Capstone.Identity.API.Enums;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Repositories;
using Capstone.Identity.API.Services;
using Capstone.Identity.API.Tests.TestHelpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Capstone.Identity.API.Tests.Services
{
    [TestClass]
    public class ApplicationUserServiceTests
    {
        private const string Email = "kyle@test.com";
        private const string Password = "Passw0rd!";
        private const string BuiltDisplayName = "Knight, Kyle the Brave";

        private Mock<UserManager<ApplicationUser>> _userManager = null!;
        private Mock<IUserProfileRepository> _profileRepository = null!;
        private Mock<IDisplayNameGenerator> _displayNameGenerator = null!;
        private Mock<IUnitOfWork> _unitOfWork = null!;
        private Mock<IDbContextTransaction> _transaction = null!;
        private ApplicationUserService _service = null!;

        private ApplicationUser? _createdUser;
        private UserProfile? _addedProfile;

        [TestInitialize]
        public void TestInitialize()
        {
            _createdUser = null;
            _addedProfile = null;

            // UserManager: no existing user, and creation succeeds
            _userManager = IdentityMocks.CreateUserManager();
            _userManager
                .Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser?)null);
            _userManager
                .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .Callback<ApplicationUser, string>((user, _) => CaptureCreatedUser(user))
                .ReturnsAsync(IdentityResult.Success);
            _userManager
                .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>()))
                .Callback<ApplicationUser>(CaptureCreatedUser)
                .ReturnsAsync(IdentityResult.Success);
            _userManager
                .Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string>());

            // Repository: remember whatever profile gets staged
            _profileRepository = new Mock<IUserProfileRepository>();
            _profileRepository
                .Setup(r => r.AddUserProfile(It.IsAny<UserProfile>()))
                .Callback<UserProfile>(profile => _addedProfile = profile);

            // Generator: fixed, predictable values
            _displayNameGenerator = new Mock<IDisplayNameGenerator>();
            _displayNameGenerator.Setup(g => g.DefaultSalutation).Returns(DisplayNameSalutations.Knight);
            _displayNameGenerator.Setup(g => g.PickRandomModifier()).Returns(DisplayNameModifiers.TheBrave);
            _displayNameGenerator
                .Setup(g => g.GenerateDisplayName(It.IsAny<DisplayNameSalutations>(), It.IsAny<string>(), It.IsAny<DisplayNameModifiers>()))
                .Returns(BuiltDisplayName);

            // Unit of work: hands out a transaction we can inspect
            _transaction = new Mock<IDbContextTransaction>();
            _unitOfWork = new Mock<IUnitOfWork>();
            _unitOfWork.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(_transaction.Object);
            _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            _service = new ApplicationUserService(
                _userManager.Object,
                _displayNameGenerator.Object,
                _profileRepository.Object,
                _unitOfWork.Object,
                NullLogger<ApplicationUserService>.Instance);

            _userManager
                .Setup(m => m.AddToRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);
        }

        // Simulates the database assigning an ID when the user is saved
        private void CaptureCreatedUser(ApplicationUser user)
        {
            user.Id = Guid.NewGuid();
            _createdUser = user;
        }

        private void VerifyUserNeverCreated()
        {
            _userManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
            _userManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        // ================= CreateUserWithProfileAsync =================

        [TestMethod]
        public async Task Create_PasswordWithoutEmail_ReturnsValidation_AndDoesNotCreate()
        {
            var result = await _service.CreateUserWithProfileAsync(null, Password, "Kyle", "Test");

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.Validation, result.ErrorType);
            VerifyUserNeverCreated();
        }

        [TestMethod]
        public async Task Create_EmailAlreadyInUse_ReturnsConflict_AndDoesNotCreate()
        {
            _userManager
                .Setup(m => m.FindByEmailAsync(Email))
                .ReturnsAsync(new ApplicationUser { Email = Email });

            var result = await _service.CreateUserWithProfileAsync(Email, Password, "Kyle", "Test");

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.Conflict, result.ErrorType);
            VerifyUserNeverCreated();
        }

        [TestMethod]
        public async Task Create_IdentityRejectsUser_ReturnsValidation_AndDoesNotAddProfileOrCommit()
        {
            _userManager
                .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(
                    new IdentityError { Code = "PasswordTooShort", Description = "Password is too short." },
                    new IdentityError { Code = "PasswordRequiresDigit", Description = "Password needs a digit." }));

            var result = await _service.CreateUserWithProfileAsync(Email, "abc", "Kyle", "Test");

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.Validation, result.ErrorType);
            StringAssert.Contains(result.Error, "Password is too short.");
            StringAssert.Contains(result.Error, "Password needs a digit.");

            _profileRepository.Verify(r => r.AddUserProfile(It.IsAny<UserProfile>()), Times.Never);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
            _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task Create_WithPassword_UsesPasswordOverload_AndUserNameIsEmail()
        {
            var result = await _service.CreateUserWithProfileAsync(Email, Password, "Kyle", "Test");

            Assert.IsTrue(result.Succeeded);
            _userManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), Password), Times.Once);
            _userManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>()), Times.Never);
            Assert.AreEqual(Email, _createdUser!.UserName);
            Assert.AreEqual(Email, _createdUser.Email);
        }

        [TestMethod]
        public async Task Create_WithoutPassword_UsesNoPasswordOverload_AndUserNameIsPlaceholder()
        {
            var result = await _service.CreateUserWithProfileAsync(null, null, "Kyle", "Test");

            Assert.IsTrue(result.Succeeded);
            _userManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>()), Times.Once);
            _userManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
            StringAssert.StartsWith(_createdUser!.UserName, "placeholder-");
            Assert.IsNull(_createdUser.Email);
        }

        [TestMethod]
        public async Task Create_Success_StagesProfileWithGeneratedDisplayName()
        {
            var result = await _service.CreateUserWithProfileAsync(Email, Password, "Kyle", "Test");

            Assert.IsTrue(result.Succeeded);
            Assert.IsNotNull(_addedProfile);
            Assert.AreEqual(_createdUser!.Id, _addedProfile.ApplicationUserId);
            Assert.AreEqual(DisplayNameSalutations.Knight, _addedProfile.DisplayNameSalutation);
            Assert.AreEqual(DisplayNameModifiers.TheBrave, _addedProfile.DisplayNameModifier);
            Assert.AreEqual(BuiltDisplayName, _addedProfile.DisplayName);

            _displayNameGenerator.Verify(
                g => g.GenerateDisplayName(DisplayNameSalutations.Knight, "Kyle", DisplayNameModifiers.TheBrave), Times.Once);
        }

        [TestMethod]
        public async Task Create_Success_SavesAndCommitsOnce()
        {
            await _service.CreateUserWithProfileAsync(Email, Password, "Kyle", "Test");

            _unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
            _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task Create_TrimsNames()
        {
            await _service.CreateUserWithProfileAsync(Email, Password, "  Kyle  ", "  Test  ");

            Assert.AreEqual("Kyle", _createdUser!.FirstName);
            Assert.AreEqual("Test", _createdUser.LastName);
            _displayNameGenerator.Verify(
                g => g.GenerateDisplayName(It.IsAny<DisplayNameSalutations>(), "Kyle", It.IsAny<DisplayNameModifiers>()), Times.Once);
        }

        [TestMethod]
        public async Task Create_Success_ReturnsCreatedUser()
        {
            var result = await _service.CreateUserWithProfileAsync(Email, Password, "Kyle", "Test");

            Assert.IsTrue(result.Succeeded);
            Assert.AreSame(_createdUser, result.Data);
        }

        // ================= GetCurrentUserAsync =================

        [TestMethod]
        public async Task GetCurrentUser_UserNotFound_ReturnsNotFound()
        {
            var userId = Guid.NewGuid();
            _userManager
                .Setup(m => m.FindByIdAsync(userId.ToString()))
                .ReturnsAsync((ApplicationUser?)null);

            var result = await _service.GetCurrentUserAsync(userId);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.NotFound, result.ErrorType);
        }

        [TestMethod]
        public async Task GetCurrentUser_ProfileMissing_ReturnsUnexpected()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = Email, FirstName = "Kyle", LastName = "Test" };
            _userManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _profileRepository
                .Setup(r => r.GetUserProfileByUserIdAsync(user.Id))
                .ReturnsAsync((UserProfile?)null);

            var result = await _service.GetCurrentUserAsync(user.Id);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ResultErrorType.Unexpected, result.ErrorType);
        }

        [TestMethod]
        public async Task GetCurrentUser_Success_MapsAllFields()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = Email, FirstName = "Kyle", LastName = "Test" };
            var profile = new UserProfile
            {
                ApplicationUserId = user.Id,
                DisplayName = BuiltDisplayName,
                AvatarImagePath = "/avatars/kyle.png"
            };

            _userManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Learner" });
            _profileRepository.Setup(r => r.GetUserProfileByUserIdAsync(user.Id)).ReturnsAsync(profile);

            var result = await _service.GetCurrentUserAsync(user.Id);

            Assert.IsTrue(result.Succeeded);
            var dto = result.Data!;
            Assert.AreEqual(user.Id, dto.Id);
            Assert.AreEqual(Email, dto.Email);
            Assert.AreEqual("Kyle", dto.FirstName);
            Assert.AreEqual("Test", dto.LastName);
            Assert.AreEqual(BuiltDisplayName, dto.DisplayName);
            Assert.AreEqual("/avatars/kyle.png", dto.AvatarImagePath);
            CollectionAssert.AreEqual(new[] { "Learner" }, dto.Roles.ToList());
        }
    }
}
