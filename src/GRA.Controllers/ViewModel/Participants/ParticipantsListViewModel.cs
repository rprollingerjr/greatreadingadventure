﻿using GRA.Controllers.ViewModel.Shared;
using System.Collections.Generic;

namespace GRA.Controllers.ViewModel.Participants
{
    public class ParticipantsListViewModel
    {
        public IEnumerable<GRA.Domain.Model.User> Users { get; set; }
        public PaginateViewModel PaginateModel { get; set; }
        public string Search { get; set; }
        public bool CanRemoveParticipant { get; set; }
        public bool CanViewDetails { get; set; }
    }
}
