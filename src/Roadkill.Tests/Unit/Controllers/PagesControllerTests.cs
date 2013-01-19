﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using Moq;
using NUnit.Framework;
using Roadkill.Core;
using Roadkill.Core.Configuration;
using Roadkill.Core.Controllers;
using Roadkill.Core.Converters;
using Roadkill.Core.Localization.Resx;
using Roadkill.Core.Search;

namespace Roadkill.Tests.Unit
{
	[TestFixture]
	[Category("Unit")]
	public class PagesControllerTests
	{
		private IConfigurationContainer _config;
		private IRoadkillContext _context;
		private IRepository _repository;

		private UserManager _userManager;
		private PageManager _pageManager;
		private HistoryManager _historyManager;
		private SettingsManager _settingsManager;
		private SearchManager _searchManager;
		private PagesController _pagesController;
		private MvcMockContainer _mocksContainer;
		private RoadkillContextStub _contextStub;

		private List<Page> _pages;
		private List<PageContent> _pagesContent;

		[SetUp]
		public void Setup()
		{
			_pages = new List<Page>();
			_pagesContent = new List<PageContent>();

			_config = new RoadkillSettings();
			_config.ApplicationSettings = new ApplicationSettings();
			_config.SitePreferences = new SitePreferences() { AllowedFileTypes = "png, jpg" };
			_config.ApplicationSettings.AttachmentsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "attachments");

			// Dependencies for PageManager
			Mock<IRepository> repositoryMock = new Mock<IRepository>();
			repositoryMock.Setup(x => x.Pages).Returns(_pages.AsQueryable());
			repositoryMock.Setup(x => x.PageContents).Returns(_pagesContent.AsQueryable());
			repositoryMock.Setup(x => x.GetLatestPageContent(It.IsAny<int>())).Returns<int>((id) => _pagesContent.FirstOrDefault(p => p.Page.Id == id));
			repositoryMock.Setup(x => x.Delete<Page>(It.IsAny<Page>())).Callback<Page>(page => _pages.Remove(_pages.First(p => p.Id == page.Id)));

			_repository = repositoryMock.Object;
			_userManager = new Mock<UserManager>(_config, _repository).Object;
			_historyManager = new HistoryManager(_config, _repository, _context);
			_settingsManager = new SettingsManager(_config, _repository);
			_searchManager = new SearchManager(_config, _repository);
			_pageManager = new PageManager(_config, _repository, _searchManager, _historyManager, _context);

			_contextStub = new RoadkillContextStub();
			_pagesController = new PagesController(_config, _userManager, _settingsManager, _pageManager, _searchManager, _historyManager, _contextStub);
			_mocksContainer = _pagesController.SetFakeControllerContext();
		}

		private Page AddDummyPage1()
		{
			Page page1 = new Page() { Id = 1, Tags = "tag1,tag2", Title = "Welcome to the site", CreatedBy = "admin" };
			PageContent page1Content = new PageContent() { Id = Guid.NewGuid(), Page = page1, Text = "Hello world 1" };
			_pages.Add(page1);
			_pagesContent.Add(page1Content);

			return page1;
		}

		private Page AddDummyPage2()
		{
			Page page2 = new Page() { Id = 50, Tags = "anothertag", Title = "Page 2" };
			PageContent page2Content = new PageContent() { Id = Guid.NewGuid(), Page = page2, Text = "Hello world 2" };
			_pages.Add(page2);
			_pagesContent.Add(page2Content);

			return page2;
		}

		[Test]
		public void AllPages_Should_Return_Model_And_Pages()
		{
			// Arrange
			Page page1 = AddDummyPage1();
			Page page2 = AddDummyPage2();
			PageContent page1Content = _pagesContent.First(p => p.Page.Id == page1.Id);

			// Act
			ActionResult result = _pagesController.AllPages();

			// Assert
			Assert.That(result, Is.TypeOf<ViewResult>(), "ViewResult");

			IEnumerable<PageSummary> model = result.ModelFromActionResult<IEnumerable<PageSummary>>();
			Assert.NotNull(model, "Null model");

			List<PageSummary> summaryList = model.OrderBy(p => p.Id).ToList();
			Assert.That(summaryList.Count, Is.EqualTo(2));
			Assert.That(summaryList[0].Title, Is.EqualTo(page1.Title));
			Assert.That(summaryList[0].Content, Is.EqualTo(page1Content.Text));
		}

		[Test]
		public void AllTags_Should_Return_Model_And_Tags()
		{
			// Arrange
			Page page1 = AddDummyPage1();
			page1.Tags = "a-tag,b-tag";

			Page page2 = AddDummyPage2();
			page2.Tags = "z-tag,a-tag";

			// Act
			ActionResult result = _pagesController.AllTags();

			// Assert
			Assert.That(result, Is.TypeOf<ViewResult>(), "ViewResult");

			IEnumerable<TagSummary> model = result.ModelFromActionResult<IEnumerable<TagSummary>>();
			Assert.NotNull(model, "Null model");

			List<TagSummary> summaryList = model.OrderBy(p => p.Name).ToList();
			Assert.That(summaryList.Count, Is.EqualTo(3));
			Assert.That(summaryList[0].Name, Is.EqualTo("a-tag"));
			Assert.That(summaryList[0].Count, Is.EqualTo(2));
		}

		[Test]
		public void AllTagsAsJson_Should_Return_Model_And_Tags()
		{
			// Arrange
			Page page1 = AddDummyPage1();
			page1.Tags = "a-tag,b-tag";

			Page page2 = AddDummyPage2();
			page2.Tags = "z-tag,a-tag";

			// Act
			ActionResult result = _pagesController.AllTagsAsJson();		

			// Assert
			Assert.That(result, Is.TypeOf<JsonResult>(), "ViewResult");

			JsonResult jsonResult = result as JsonResult;
			Assert.NotNull(jsonResult, "Null jsonResult");

			Assert.That(jsonResult.JsonRequestBehavior, Is.EqualTo(JsonRequestBehavior.AllowGet));
			Assert.That(jsonResult.Data, Is.TypeOf<Dictionary<string, object>>());
		}

		[Test]
		public void ByUser_Should_Contains_ViewData_And_Return_Model_And_Pages()
		{
			// Arrange
			string username = "amazinguser";

			Page page1 = AddDummyPage1();
			page1.CreatedBy = username;
			PageContent page1Content = _pagesContent.First(p => p.Page.Id == page1.Id);

			Page page2 = AddDummyPage2();
			page2.CreatedBy = username;

			// Act
			ActionResult result = _pagesController.ByUser(username, false);

			// Assert
			Assert.That(_pagesController.ViewData.Keys.Count, Is.GreaterThanOrEqualTo(1));

			Assert.That(result, Is.TypeOf<ViewResult>(), "ViewResult");
			IEnumerable<PageSummary> model = result.ModelFromActionResult<IEnumerable<PageSummary>>();
			Assert.NotNull(model, "Null model");

			List<PageSummary> summaryList = model.OrderBy(p => p.Id).ToList();
			Assert.That(summaryList.Count, Is.EqualTo(2));
			Assert.That(summaryList[0].CreatedBy, Is.EqualTo(username));
			Assert.That(summaryList[1].CreatedBy, Is.EqualTo(username));
			Assert.That(summaryList[0].Title, Is.EqualTo(page1.Title));
			Assert.That(summaryList[0].Content, Is.EqualTo(page1Content.Text));
		}

		[Test]
		public void Delete_Should_Contains_Redirect_And_Remove_Page()
		{
			// Arrange
			Page page1 = AddDummyPage1();
			Page page2 = AddDummyPage2();

			// Act
			ActionResult result = _pagesController.Delete(50);

			// Assert
			Assert.That(result, Is.TypeOf<RedirectToRouteResult>(), "ViewResult");
			RedirectToRouteResult redirectResult = result as RedirectToRouteResult;
			Assert.NotNull(redirectResult, "Null RedirectToRouteResult");

			Assert.That(redirectResult.RouteValues["action"], Is.EqualTo("AllPages"));
			Assert.That(_pages.Count, Is.EqualTo(1));
		}

		[Test]
		public void Edit_GET_Should_Redirect_With_Invalid_Page_Id()
		{
			// Arrange

			// Act
			ActionResult result = _pagesController.Edit(1);

			// Assert
			Assert.That(result, Is.TypeOf<RedirectToRouteResult>(), "ViewResult");
			RedirectToRouteResult redirectResult = result as RedirectToRouteResult;
			Assert.NotNull(redirectResult, "Null RedirectToRouteResult");

			Assert.That(redirectResult.RouteValues["action"], Is.EqualTo("New"));
		}

		[Test]
		public void Edit_GET_As_Editor_With_Locked_Page_Should_Return_403()
		{
			// Arrange
			_contextStub.IsAdmin = false;
			Page page1 = AddDummyPage1();
			page1.IsLocked = true;

			// Act
			ActionResult result = _pagesController.Edit(page1.Id);

			// Assert
			Assert.That(result, Is.TypeOf<HttpStatusCodeResult>(), "ViewResult");
			HttpStatusCodeResult statusResult = result as HttpStatusCodeResult;
			Assert.NotNull(statusResult, "Null RedirectToRouteResult");

			Assert.That(statusResult.StatusCode, Is.EqualTo(403));
		}

		[Test]
		public void Edit_GET_Should_Return_ViewResult()
		{
			// Arrange
			Page page = AddDummyPage1();
			PageContent pageContent = _pagesContent.First(p => p.Page.Id == page.Id);

			// Act
			ActionResult result = _pagesController.Edit(page.Id);

			// Assert
			Assert.That(result, Is.TypeOf<ViewResult>(), "ViewResult");
			ViewResult viewResult = result as ViewResult;
			Assert.NotNull(viewResult, "Null viewResult");

			PageSummary model = result.ModelFromActionResult<PageSummary>();
			Assert.NotNull(model, "Null model");
			Assert.That(model.Id, Is.EqualTo(page.Id));
			Assert.That(model.Content, Is.EqualTo(pageContent.Text));
		}

		
		// controller.Edit(); POST
		// controller.GetPreview();
		// controller.History();
		// controller.New(); x2
		// controller.Revert();
		// controller.Tag()
		// controller.Version();
		// Document PrincipalWrapper
	}
}
