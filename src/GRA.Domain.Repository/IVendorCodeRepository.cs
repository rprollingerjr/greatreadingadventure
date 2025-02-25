﻿using System.Collections.Generic;
using System.Threading.Tasks;
using GRA.Domain.Model;
using GRA.Domain.Model.Utility;

namespace GRA.Domain.Repository
{
    public interface IVendorCodeRepository : IRepository<VendorCode>
    {
        Task<VendorCode> AssignCodeAsync(int vendorCodeTypeId, int userId);

        Task<VendorCode> AssociateCodeAsync(int vendorCodeTypeId, int userId, string reason, int activeUserId);

        Task<ICollection<string>> GetAllCodesAsync(int vendorCodeTypeId);

        Task<IEnumerable<string>> GetAssociatedVendorCodes(int userId);

        Task<VendorCode> GetByCode(string code);

        Task<ICollection<VendorCode>> GetByPackingSlipAsync(long packingSlipNumber);

        Task<ICollection<VendorCode>> GetEarnedCodesAsync(ReportCriterion criterion);

        Task<ICollection<VendorCode>> GetHoldSlipsAsync(long packingSlipNumber);

        Task<ICollection<VendorCode>> GetPendingHouseholdCodes(int headOfHouseholdId);

        //todo fix type
        Task<IList<ReportVendorCodePending>> GetPendingPrizesPickupBranch();

        Task<ICollection<VendorCode>> GetRemainingPrizesForBranchAsync(int branchId);

        Task<VendorCodeStatus> GetStatusAsync();

        Task<ICollection<VendorCodeEmailAward>> GetUnreportedEmailAwardCodes(int siteId,
            int vendorCodeTypeId);

        Task<VendorCode> GetUserVendorCode(int userId);
    }
}
