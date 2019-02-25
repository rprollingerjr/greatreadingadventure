﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GRA.Controllers.ViewModel.Events;
using GRA.Controllers.ViewModel.Shared;
using GRA.Domain.Model;
using GRA.Domain.Model.Filters;
using GRA.Domain.Service;
using GRA.Domain.Service.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;

namespace GRA.Controllers
{
    public class EventsController : Base.UserController
    {
        private readonly ILogger<EventsController> _logger;
        private readonly EventService _eventService;
        private readonly SiteService _siteService;
        private readonly SpatialService _spatialService;
        private readonly UserService _userService;

        public EventsController(ILogger<EventsController> logger,
            ServiceFacade.Controller context,
            EventService eventService,
            SiteService siteService,
            SpatialService spatialService,
            UserService userService)
            : base(context)
        {
            _logger = Require.IsNotNull(logger, nameof(logger));
            _eventService = Require.IsNotNull(eventService, nameof(eventService));
            _siteService = Require.IsNotNull(siteService, nameof(SiteService));
            _spatialService = spatialService
                ?? throw new ArgumentNullException(nameof(spatialService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            PageTitle = "Events";
        }

        public async Task<IActionResult> CommunityExperiences(int page = 1,
            string sort = null,
            string search = null,
            string near = null,
            int? system = null,
            int? branch = null,
            int? location = null,
            int? program = null,
            string StartDate = null,
            string EndDate = null)
        {
            var site = await GetCurrentSiteAsync();
            if (!string.IsNullOrEmpty(site.ExternalEventListUrl))
            {
                return new RedirectResult(site.ExternalEventListUrl);
            }

            PageTitle = "Community Experiences";
            return await Index(page, sort, search, near, system, branch, location, program, StartDate, EndDate, true);
        }

        public async Task<IActionResult> Index(int page = 1,
            string sort = null,
            string search = null,
            string near = null,
            int? system = null,
            int? branch = null,
            int? location = null,
            int? program = null,
            string StartDate = null,
            string EndDate = null,
            bool CommunityExperiences = false)
        {
            var site = await GetCurrentSiteAsync();
            if (!string.IsNullOrEmpty(site.ExternalEventListUrl))
            {
                return new RedirectResult(site.ExternalEventListUrl);
            }

            ModelState.Clear();
            var filter = new EventFilter(page)
            {
                Search = search,
                EventType = CommunityExperiences ? 1 : 0
            };

            var nearSearchEnabled = await _siteLookupService
                .IsSiteSettingSetAsync(site.Id, SiteSettingKey.Events.GoogleMapsAPIKey);

            if (!string.IsNullOrWhiteSpace(sort) && Enum.IsDefined(typeof(SortEventsBy), sort))
            {
                filter.SortBy = (SortEventsBy)Enum.Parse(typeof(SortEventsBy), sort);
            }
            else
            {
                if (nearSearchEnabled && !string.IsNullOrWhiteSpace(near))
                {
                    filter.SortBy = SortEventsBy.Distance;
                }
            }

            if (nearSearchEnabled)
            {
                if (!string.IsNullOrWhiteSpace(near))
                {
                    var geocodeResult = await _spatialService.GetGeocodedAddressAsync(near);
                    if (geocodeResult.Status == ServiceResultStatus.Success)
                    {
                        filter.SpatialDistanceHeaderId = await _spatialService
                            .GetSpatialDistanceIdForGeolocationAsync(geocodeResult.Data);
                    }
                }
            }
            else
            {
                // ignore location if branch has value
                if (branch.HasValue)
                {
                    filter.BranchIds = new List<int>() { branch.Value };
                }
                else if (system.HasValue)
                {
                    filter.SystemIds = new List<int>() { system.Value };
                }
                else if (location.HasValue)
                {
                    filter.LocationIds = new List<int?>() { location.Value };
                }
            }

            if (program.HasValue)
            {
                filter.ProgramIds = new List<int?>() { program.Value };
            }

            if (!string.Equals(StartDate, "False", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(StartDate))
                {
                    if (DateTime.TryParse(StartDate, out var startDate))
                    {
                        filter.StartDate = startDate.Date;
                    }
                }
                else
                {
                    filter.StartDate = _dateTimeProvider.Now.Date;
                }
            }

            if (!string.IsNullOrWhiteSpace(EndDate) && DateTime.TryParse(EndDate, out var endDate))
            {
                filter.EndDate = endDate.Date;
            }

            var eventList = await _eventService.GetPaginatedListAsync(filter);

            var paginateModel = new PaginateViewModel
            {
                ItemCount = eventList.Count,
                CurrentPage = page,
                ItemsPerPage = filter.Take.Value
            };

            if (paginateModel.PastMaxPage)
            {
                return RedirectToRoute(
                    new
                    {
                        page = paginateModel.LastPage ?? 1
                    });
            }

            var viewModel = new EventsListViewModel
            {
                Events = eventList.Data,
                PaginateModel = paginateModel,
                Sort = filter.SortBy.ToString(),
                Search = search,
                StartDate = filter.StartDate,
                EndDate = filter.EndDate,
                ProgramId = program,
                ProgramList = new SelectList(await _siteService.GetProgramList(), "Id", "Name"),
                CommunityExperiences = CommunityExperiences,
                ShowNearSearch = nearSearchEnabled
            };

            if (nearSearchEnabled)
            {
                viewModel.Near = near?.Trim();

                if (HttpContext.User.Identity.IsAuthenticated)
                {
                    var user = await _userService.GetDetails(GetActiveUserId());
                    if (!string.IsNullOrWhiteSpace(user.PostalCode))
                    {
                        viewModel.UserZipCode = user.PostalCode;
                    }
                }
            }
            else
            {
                viewModel.SystemList = new SelectList(
                    await _siteService.GetSystemList(), "Id", "Name");
                viewModel.LocationList = new SelectList(
                    await _eventService.GetLocations(), "Id", "Name");

                if (branch.HasValue)
                {
                    var selectedBranch = await _siteService.GetBranchByIdAsync(branch.Value);
                    viewModel.SystemId = selectedBranch.SystemId;
                    viewModel.BranchList = new SelectList(
                        await _siteService.GetBranches(selectedBranch.SystemId),
                        "Id", "Name", branch.Value);
                }
                else if (system.HasValue)
                {
                    viewModel.SystemId = system;
                    viewModel.BranchList = new SelectList(
                        await _siteService.GetBranches(system.Value), "Id", "Name");
                }
                else
                {
                    viewModel.BranchList = new SelectList(await _siteService.GetAllBranches(),
                        "Id", "Name");
                }

                if (location.HasValue && !branch.HasValue)
                {
                    viewModel.LocationId = location.Value;
                    viewModel.UseLocation = true;
                }
            }

            var (descriptionTextSet, communityExperienceDescription) = await _siteLookupService
                .GetSiteSettingStringAsync(site.Id,
                    SiteSettingKey.Events.CommunityExperienceDescription);

            if (descriptionTextSet)
            {
                viewModel.CommunityExperienceDescription = communityExperienceDescription;
            }

            return View("Index", viewModel);
        }

        [HttpPost]
        public IActionResult Index(EventsListViewModel model)
        {
            string startDate = "False";
            string endDate = null;
            bool? isCommunityExperience = null;

            if (model.UseLocation == true)
            {
                model.SystemId = null;
                model.BranchId = null;
            }
            else
            {
                model.LocationId = null;
            }

            if (model.BranchId.HasValue)
            {
                model.SystemId = null;
            }

            if (model.StartDate.HasValue)
            {
                startDate = model.StartDate.Value.ToString("MM-dd-yyyy");
            }

            if (model.EndDate.HasValue
                && (!model.StartDate.HasValue || model.EndDate >= model.StartDate))
            {
                endDate = model.EndDate.Value.ToString("MM-dd-yyyy");
            }

            if (model.CommunityExperiences)
            {
                isCommunityExperience = true;
            }

            return RedirectToAction("Index", new { model.Sort, model.Search, model.Near, System = model.SystemId, Branch = model.BranchId, Location = model.LocationId, Program = model.ProgramId, StartDate = startDate, EndDate = endDate, CommunityExperiences = isCommunityExperience });
        }

        public async Task<IActionResult> Detail(int id)
        {
            var site = await GetCurrentSiteAsync();
            if (!string.IsNullOrEmpty(site.ExternalEventListUrl))
            {
                return new RedirectResult(site.ExternalEventListUrl);
            }

            try
            {
                var viewModel = new EventsDetailViewModel
                {
                    Event = await _eventService.GetDetails(id)
                };

                PageTitle = $"{viewModel.Event.Name} @ {viewModel.Event.EventLocationName}";
                if (!string.IsNullOrEmpty(viewModel.Event.EventLocationName)
                    && !string.IsNullOrEmpty(viewModel.Event.EventLocationAddress))
                {
                    viewModel.ShowStructuredData = true;
                    if (viewModel.Event.AllDay)
                    {
                        viewModel.EventStart = viewModel.Event.StartDate.ToString("yyyy-MM-dd",
                            CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        viewModel.EventStart = viewModel.Event.StartDate.ToString("s",
                            CultureInfo.InvariantCulture);
                        if (viewModel.Event.EndDate != null)
                        {
                            var endDate = (DateTime)viewModel.Event.EndDate;
                            viewModel.EventEnd = endDate.ToString("s",
                                CultureInfo.InvariantCulture);
                        }
                    }
                }

                if (viewModel.Event.ProgramId.HasValue)
                {
                    var program = await _siteService.GetProgramByIdAsync(viewModel.Event.ProgramId.Value);
                    viewModel.ProgramString = $"This event is limited to the {program.Name} program.";
                }
                return View(viewModel);
            }
            catch (GraException gex)
            {
                ShowAlertWarning("Unable to view event: ", gex);
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> GetDetails(int eventId)
        {
            try
            {
                var viewModel = new EventsDetailViewModel
                {
                    Event = await _eventService.GetDetails(eventId)
                };

                if (viewModel.Event.ProgramId.HasValue)
                {
                    var program = await _siteService.GetProgramByIdAsync(viewModel.Event.ProgramId.Value);
                    viewModel.ProgramString = $"This event is limited to the {program.Name} program.";
                }

                return PartialView("_DetailPartial", viewModel);
            }
            catch (GraException gex)
            {
                Response.StatusCode = (int)HttpStatusCode.NotFound;
                return Content($"Unable to view event: {gex.Message}");
            }
        }
    }
}