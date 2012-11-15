﻿using System;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using MVCForum.Domain.Constants;
using MVCForum.Domain.Interfaces.Services;
using MVCForum.Domain.Interfaces.UnitOfWork;
using MVCForum.Utilities;
using MVCForum.Website.Application;
using MVCForum.Website.Areas.Admin.ViewModels;
using MVCForum.Website.ViewModels.Mapping;

namespace MVCForum.Website.Areas.Admin.Controllers
{
    [Authorize(Roles = AppConstants.AdminRoleName)]
    public class SettingsController : BaseAdminController
    {
        private readonly IRoleService _roleService;

        public SettingsController(ILoggingService loggingService, IUnitOfWorkManager unitOfWorkManager,
            ILocalizationService localizationService,
            IMembershipService membershipService,
            IRoleService roleService,
            ISettingsService settingsService)
            : base(loggingService, unitOfWorkManager, membershipService, localizationService, settingsService)
        {
            _roleService = roleService;
        }

        public ActionResult Index()
        {
            using (UnitOfWorkManager.NewUnitOfWork())
            {
                CacheUtils.Clear(AppConstants.SettingsCacheName);   
                var currentSettings = SettingsService.GetSettings(false);
                var settingViewModel = ViewModelMapping.SettingsToSettingsViewModel(currentSettings);
                settingViewModel.NewMemberStartingRole = _roleService.GetRole(SettingsService.GetSettings(false).NewMemberStartingRole.Id).Id;
                settingViewModel.DefaultLanguage = LocalizationService.DefaultLanguage.Id;
                settingViewModel.Roles = _roleService.AllRoles().ToList();
                settingViewModel.Languages = LocalizationService.AllLanguages.ToList();
                return View(settingViewModel);
            }
        }

        [HttpPost]
        public ActionResult Index(EditSettingsViewModel settingsViewModel)
        {
            if (ModelState.IsValid)
            {
                using (var unitOfWork = UnitOfWorkManager.NewUnitOfWork())
                {
                    try
                    {
                        
                        var existingSettings = SettingsService.GetSettings(false);
                        var updatedSettings = ViewModelMapping.SettingsViewModelToSettings(settingsViewModel, existingSettings);

                        // Map over viewModel from 
                        if (settingsViewModel.NewMemberStartingRole != null)
                        {
                            updatedSettings.NewMemberStartingRole =
                                _roleService.GetRole(settingsViewModel.NewMemberStartingRole.Value);
                        }
                        
                        if (settingsViewModel.DefaultLanguage != null)
                        {
                            updatedSettings.DefaultLanguage =
                                LocalizationService.Get(settingsViewModel.DefaultLanguage.Value);
                        }

                        var culture = new CultureInfo(updatedSettings.DefaultLanguage.LanguageCulture);

                        unitOfWork.Commit();

                        // Set the culture session too
                        Session["Culture"] = culture;
                    }
                    catch (Exception ex)
                    {
                        unitOfWork.Rollback();
                        LoggingService.Error(ex);
                    }
                }

                // All good clear cache and get reliant lists
                using (UnitOfWorkManager.NewUnitOfWork())
                {
                    TempData[AppConstants.MessageViewBagName] = new GenericMessageViewModel
                    {
                        Message = "Settings Updated",
                        MessageType = GenericMessages.success
                    };
                    settingsViewModel.Themes = AppHelpers.GetThemeFolders();
                    settingsViewModel.Roles = _roleService.AllRoles().ToList();
                    settingsViewModel.Languages = LocalizationService.AllLanguages.ToList();
                    CacheUtils.Clear(AppConstants.SettingsCacheName);                    
                }
            }
            return View(settingsViewModel);
        }
    }
}
