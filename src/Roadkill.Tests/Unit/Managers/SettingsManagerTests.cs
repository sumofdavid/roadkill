﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Roadkill.Core;
using Roadkill.Core.Configuration;
using Roadkill.Core.Database;

namespace Roadkill.Tests.Unit
{
	[TestFixture]
	public class SettingsManagerTests
	{
		private RepositoryStub _repository;
		private IConfigurationContainer _config;
		private SettingsManager _settingsManager;

		[SetUp]
		public void Setup()
		{
			_config = new ConfigurationContainerStub();
			_config.ApplicationSettings.Installed = true;

			_repository = new RepositoryStub();
			_settingsManager = new SettingsManager(_config, _repository);
		}

		[Test]
		public void ClearPageTables_Should_Remove_All_Pages_And_Content()
		{
			// Arrange
			_repository.AddNewPage(new Page(), "test1", "test1", DateTime.Now);
			_repository.AddNewPage(new Page(), "test2", "test2", DateTime.Now);

			// Act
			_settingsManager.ClearPageTables();

			// Assert
			Assert.That(_repository.AllPages().Count(), Is.EqualTo(0));
			Assert.That(_repository.AllPageContents().Count(), Is.EqualTo(0));
		}

		[Test]
		public void ClearUserTable_Should_Remove_All_Users()
		{
			// Arrange
			_repository.Users.Add(new User() { IsAdmin = true });
			_repository.Users.Add(new User() { IsAdmin = true });
			_repository.Users.Add(new User() { IsEditor = true });
			_repository.Users.Add(new User() { IsEditor = true });

			// Act
			_settingsManager.ClearUserTable();

			// Assert
			Assert.That(_repository.FindAllAdmins().Count(), Is.EqualTo(0)); // need an allusers method
			Assert.That(_repository.FindAllEditors().Count(), Is.EqualTo(0));
		}

		[Test]
		public void MyTest()
		{
			// Arrange


			// Act
			

			// Assert
		}

		//_settingsManager.CreateTables();
		//_settingsManager.SaveCurrentVersionToWebConfig();
		//_settingsManager.SaveSitePreferences();
		//_settingsManager.SaveWebConfigSettings();
		//_settingsManager.UpdateRepository();
	}
}