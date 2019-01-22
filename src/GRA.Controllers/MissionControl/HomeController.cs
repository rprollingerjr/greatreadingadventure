﻿using GRA.Abstract;
using GRA.Controllers.ViewModel.MissionControl.Home;
using GRA.Domain.Model;
using GRA.Domain.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GRA.Controllers.MissionControl
{
    [Area("MissionControl")]
    public class HomeController : Base.MCController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AuthenticationService _authenticationService;
        private readonly ReportService _reportService;
        private readonly SampleDataService _sampleDataService;
        private readonly UserService _userService;
        private readonly SiteService _siteService;

        private readonly ICodeSanitizer _codeSanitizer;

        public HomeController(ILogger<HomeController> logger,
            AuthenticationService authenticationService,
            ReportService reportService,
            SampleDataService sampleDataService,
            UserService userService,
            SiteService siteService,
            ServiceFacade.Controller context,
            ICodeSanitizer codeSanitizer)
            : base(context)
        {
            _logger = Require.IsNotNull(logger, nameof(logger));
            _authenticationService = Require.IsNotNull(authenticationService,
                nameof(authenticationService));
            _reportService = Require.IsNotNull(reportService, nameof(reportService));
            _sampleDataService = Require.IsNotNull(sampleDataService,
                nameof(sampleDataService));
            _userService = Require.IsNotNull(userService, nameof(userService));
            _siteService = Require.IsNotNull(siteService, nameof(siteService));
            _codeSanitizer = Require.IsNotNull(codeSanitizer, nameof(codeSanitizer));

            PageTitle = "Mission Control";
        }

        public async Task<IActionResult> Index()
        {
            if (!AuthUser.Identity.IsAuthenticated)
            {
                // not logged in, redirect to login page
                return RedirectToRoute(new
                {
                    area = string.Empty,
                    controller = "SignIn",
                    ReturnUrl = "/MissionControl"
                });
            }

            if (!UserHasPermission(Permission.AccessMissionControl))
            {
                // not authorized for Mission Control, redirect to authorization code

                return RedirectToRoute(new
                {
                    area = "MissionControl",
                    controller = "Home",
                    action = "AuthorizationCode"
                });
            }
            Site site = await GetCurrentSiteAsync();

            var newsPosts = new List<object>();
            newsPosts.Add("Howdy");

            PageTitle = $"Mission Control: {site.Name}";

            // show the at-a-glance report
            var atAGlance = await GetAtAGlanceAsync();

            return View(new AtAGlanceViewModel
            {
                AtAGlanceReport = atAGlance,
                NewsPosts = newsPosts
            });
        }

        private async Task<AtAGlanceReport> GetAtAGlanceAsync()
        {
            int currentUserBranchId = GetId(ClaimType.BranchId);

            var siteStatus = await _reportService.GetCurrentStatsAsync(new ReportCriterion());
            var branchStatus = await _reportService.GetCurrentStatsAsync(new ReportCriterion
            {
                BranchId = currentUserBranchId
            });

            var branchName = await _siteService.GetBranchName(currentUserBranchId);

            return new AtAGlanceReport
            {
                FilteredBranchDescription = $"Your branch ({branchName})",
                SiteStatus = siteStatus,
                FilteredStatus = branchStatus
            };
        }

        [HttpGet]
        public async Task<IActionResult> AuthorizationCode()
        {
            var site = await GetCurrentSiteAsync();
            string siteLogoUrl = site.SiteLogoUrl
                ?? Url.Content(Defaults.SiteLogoPath);

            return View(new AuthorizationCodeViewModel
            {
                SiteLogoUrl = siteLogoUrl
            });
        }

        [HttpPost]
        public async Task<IActionResult> AuthorizationCode(AuthorizationCodeViewModel viewmodel)
        {
            if (!AuthUser.Identity.IsAuthenticated)
            {
                // not logged in, redirect to login page
                return RedirectToRoute(new
                {
                    area = string.Empty,
                    controller = "SignIn",
                    ReturnUrl = "/MissionControl"
                });
            }

            if (ModelState.IsValid)
            {
                string sanitized = _codeSanitizer.Sanitize(viewmodel.AuthorizationCode, 255);

                try
                {
                    string role
                        = await _userService.ActivateAuthorizationCode(sanitized);

                    if (!string.IsNullOrEmpty(role))
                    {
                        var auth = await _authenticationService
                            .RevalidateUserAsync(GetId(ClaimType.UserId));
                        auth.AuthenticationMessage = $"Code applied, you are now a member of the role: <strong>{role}</strong>.";
                        await LoginUserAsync(auth);
                        return RedirectToRoute(new
                        {
                            area = "MissionControl",
                            controller = "Home",
                            action = "Index"
                        });
                    }
                    else
                    {
                        ShowAlertDanger("Invalid code. This request was logged.");
                    }
                }
                catch (GraException gex)
                {
                    ShowAlertDanger("Unable to activate code: ", gex);
                }
            }
            var site = await GetCurrentSiteAsync();
            string siteLogoUrl = site.SiteLogoUrl
                ?? Url.Content(Defaults.SiteLogoPath);

            return View(new AuthorizationCodeViewModel
            {
                SiteLogoUrl = siteLogoUrl
            });
        }

        public async Task<IActionResult> LoadSampleData()
        {
            await _sampleDataService.InsertSampleData(GetId(ClaimType.UserId));
            AlertSuccess = "Inserted sample data.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Signout()
        {
            if (AuthUser.Identity.IsAuthenticated)
            {
                await LogoutUserAsync();
            }
            return RedirectToRoute(new { area = string.Empty, action = "Index" });
        }
    }
}
