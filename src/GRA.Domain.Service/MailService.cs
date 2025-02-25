﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GRA.Domain.Model;
using GRA.Domain.Model.Filters;
using GRA.Domain.Repository;
using GRA.Domain.Service.Abstract;
using Microsoft.Extensions.Logging;
using Stubble.Core.Builders;

namespace GRA.Domain.Service
{
    public class MailService : BaseUserService<MailService>
    {
        private readonly IBroadcastRepository _broadcastRepository;
        private readonly GRA.Abstract.IGraCache _cache;
        private readonly IMailRepository _mailRepository;
        private readonly IUserRepository _userRepository;

        public MailService(GRA.Abstract.IDateTimeProvider dateTimeProvider,
            GRA.Abstract.IGraCache cache,
            IBroadcastRepository broadcastRepository,
            ILogger<MailService> logger,
            IMailRepository mailRepository,
            IUserContextProvider userContextProvider,
            IUserRepository userRepository) : base(logger, dateTimeProvider, userContextProvider)
        {
            _broadcastRepository = broadcastRepository
                ?? throw new ArgumentNullException(nameof(broadcastRepository));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _mailRepository = mailRepository
                ?? throw new ArgumentNullException(nameof(mailRepository));
            _userRepository = userRepository
                ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public async Task<Broadcast> AddBroadcastAsync(Broadcast broadcast)
        {
            var authId = GetClaimId(ClaimType.UserId);

            if (HasPermission(Permission.SendBroadcastMail))
            {
                broadcast.SiteId = GetClaimId(ClaimType.SiteId);

                return await _broadcastRepository.AddSaveAsync(authId, broadcast);
            }
            else
            {
                _logger.LogError("User {UserId} doesn't have permission to send broadcasts.",
                    authId);
                throw new GraException("Permission denied");
            }
        }

        public async Task ClearCachedUserMailCountAsync(int userId)
        {
            var cacheKey = UnreadMailCacheKey(GetCurrentSiteId(), userId);
            await _cache.RemoveAsync(cacheKey);
        }

        public async Task<Broadcast> EditBroadcastAsync(Broadcast broadcast)
        {
            var authId = GetClaimId(ClaimType.UserId);

            if (HasPermission(Permission.SendBroadcastMail))
            {
                var currentBroadcast = await _broadcastRepository.GetByIdAsync(broadcast.Id);

                if (currentBroadcast.SendAt <= _dateTimeProvider.Now)
                {
                    throw new GraException("This Broadcast has already been sent.");
                }

                broadcast.SiteId = currentBroadcast.SiteId;

                return await _broadcastRepository.UpdateSaveAsync(authId, broadcast);
            }
            else
            {
                _logger.LogError("User {UserId} doesn't have permission to edit broadcasts.",
                    authId);
                throw new GraException("Permission denied");
            }
        }

        public async Task<int> GetAdminUnreadCountAsync()
        {
            if (HasPermission(Permission.ReadAllMail))
            {
                int siteId = GetClaimId(ClaimType.SiteId);
                var cacheKey = UnhandledMailCount(siteId);
                var cacheUnhandledCount = await _cache.GetIntFromCacheAsync(cacheKey);
                if (!cacheUnhandledCount.HasValue)
                {
                    int unhandledCount = await _mailRepository.GetAdminUnrepliedCountAsync(siteId);
                    await _cache.SaveToCacheAsync(cacheKey, unhandledCount, ExpireInTimeSpan());
                    return unhandledCount;
                }
                else
                {
                    return cacheUnhandledCount.Value;
                }
            }
            else
            {
                var userId = GetClaimId(ClaimType.UserId);
                _logger.LogError("User {UserId} doesn't have permission to get unread mail count.",
                    userId);
                throw new GraException("Permission denied.");
            }
        }

        public async Task<DataWithCount<IEnumerable<Mail>>> GetAllPaginatedAsync(int skip,
            int take)
        {
            int siteId = GetClaimId(ClaimType.SiteId);
            if (HasPermission(Permission.ReadAllMail))
            {
                return new DataWithCount<IEnumerable<Mail>>
                {
                    Data = await _mailRepository.PageAllAsync(siteId, skip, take),
                    Count = await _mailRepository.GetAllCountAsync(siteId)
                };
            }
            else
            {
                var userId = GetClaimId(ClaimType.UserId);
                _logger.LogError("User {UserId} doesn't have permission to get all mails.", userId);
                throw new GraException("Permission denied.");
            }
        }

        public async Task<DataWithCount<IEnumerable<Mail>>> GetAllUnrepliedPaginatedAsync(int skip,
            int take)
        {
            if (HasPermission(Permission.ReadAllMail))
            {
                int siteId = GetClaimId(ClaimType.SiteId);
                return new DataWithCount<IEnumerable<Mail>>
                {
                    Data = await _mailRepository.PageAdminUnrepliedAsync(siteId, skip, take),
                    Count = await _mailRepository.GetAdminUnrepliedCountAsync(siteId)
                };
            }
            else
            {
                var userId = GetClaimId(ClaimType.UserId);
                _logger.LogError("User {UserId} doesn't have permission to get all unread mails.",
                    userId);
                throw new GraException("Permission denied.");
            }
        }

        public async Task<Broadcast> GetBroadcastByIdAsync(int id)
        {
            var authId = GetClaimId(ClaimType.UserId);

            if (HasPermission(Permission.SendBroadcastMail))
            {
                return await _broadcastRepository.GetByIdAsync(id);
            }
            else
            {
                _logger.LogError("User {UserId} doesn't have permission to view broadcasts.",
                    authId);
                throw new GraException("Permission denied");
            }
        }

        public async Task<Mail> GetDetails(int mailId)
        {
            var activeUserId = GetActiveUserId();
            bool canReadAll = HasPermission(Permission.ReadAllMail);
            var mail = await _mailRepository.GetByIdAsync(mailId)
                ?? throw new GraException("The requested mail could not be accessed or does not exist.");

            if (mail.FromUserId == activeUserId || mail.ToUserId == activeUserId || canReadAll)
            {
                return mail;
            }
            _logger.LogError("User {UserId} doesn't have permission to view details for message {MailId}.",
                activeUserId,
                mailId);
            throw new GraException("Permission denied.");
        }

        public async Task<Mail> GetParticipantMailAsync(int mailId)
        {
            var activeUserId = GetActiveUserId();
            try
            {
                var mail = await _mailRepository.GetByIdAsync(mailId);
                if (mail.ToUserId == activeUserId)
                {
                    return mail;
                }
                else
                {
                    throw new GraException($"Mail to ID {mail.ToUserId} does not match active user ID {activeUserId}");
                }
            }
            catch (Exception ex)
            {
                throw new GraException("The requested mail could not be accessed or does not exist.", ex);
            }
        }

        public async Task<List<Mail>> GetThreadAsync(int threadId)
        {
            VerifyPermission(Permission.ReadAllMail);
            return await _mailRepository.GetThreadAsync(threadId);
        }

        public async Task<DataWithCount<IEnumerable<Mail>>> GetUserInboxPaginatedAsync(int skip,
            int take)
        {
            var activeUserId = GetActiveUserId();
            return new DataWithCount<IEnumerable<Mail>>
            {
                Data = await _mailRepository.PageUserInboxAsync(activeUserId, skip, take),
                Count = await _mailRepository.GetUserInboxCountAsync(activeUserId)
            };
        }

        public async Task<DataWithCount<IEnumerable<Mail>>> GetUserPaginatedAsync(
            int getMailForUserId,
            int skip,
            int take)
        {
            if (HasPermission(Permission.ReadAllMail))
            {
                return new DataWithCount<IEnumerable<Mail>>
                {
                    Data = await _mailRepository.PageUserAsync(getMailForUserId, skip, take),
                    Count = await _mailRepository.GetUserCountAsync(getMailForUserId)
                };
            }
            else
            {
                var requestingUser = GetClaimId(ClaimType.UserId);
                _logger.LogError("User {RequestingUserId} doesn't have permission to view messages for {UserId}.",
                    requestingUser,
                    getMailForUserId);
                throw new GraException("Permission denied.");
            }
        }

        public async Task<int> GetUserUnreadCountAsync()
        {
            var activeUserId = GetActiveUserId();
            var siteId = GetCurrentSiteId();
            var cacheKey = UnreadMailCacheKey(siteId, activeUserId);
            int? cachedUnreadCount = null;

            try
            {
                cachedUnreadCount = await _cache.GetIntFromCacheAsync(cacheKey);
            }
            catch (GraException gex)
            {
                _logger.LogWarning(gex,
                    "Problem looking up unread count for {UserId}: {ErrorMessage}",
                    activeUserId,
                    gex.Message);
            }

            if (!cachedUnreadCount.HasValue)
            {
                await SendUserBroadcastsAsync(activeUserId, true);

                int unreadCount = await _mailRepository.GetUserUnreadCountAsync(activeUserId);
                await _cache.SaveToCacheAsync(cacheKey, unreadCount, ExpireInTimeSpan());
                return unreadCount;
            }
            else
            {
                return cachedUnreadCount.Value;
            }
        }

        public async Task MarkAsReadAsync(int mailId)
        {
            var activeUserId = GetActiveUserId();
            var mail = await _mailRepository.GetByIdAsync(mailId);
            if (mail.ToUserId == activeUserId)
            {
                await _mailRepository.MarkAsReadAsync(mailId);
                await _cache.RemoveAsync(UnreadMailCacheKey(mail.SiteId, activeUserId));
                return;
            }
            _logger.LogError("User {UserId} doesn't have permission mark mail {MailId} as read.",
                activeUserId,
                mailId);
            throw new GraException("Permission denied.");
        }

        public async Task MarkHandled(int mailId)
        {
            var authId = GetClaimId(ClaimType.UserId);
            if (HasPermission(Permission.MailParticipants))
            {
                var mail = await _mailRepository.GetByIdAsync(mailId);
                if (mail.ToUserId == null)
                {
                    await _mailRepository.MarkAdminReplied(mailId);
                    var siteId = GetCurrentSiteId();
                    await _cache.RemoveAsync(UnhandledMailCount(siteId));
                }
                else
                {
                    throw new GraException("Cannot mark participant mail as handled");
                }
            }
            else
            {
                _logger.LogError("User {UserId} doesn't have permission to mark mail as handled.",
                    authId);
                throw new GraException("Permission denied");
            }
        }

        public async Task MCMarkAsReadAsync(int mailId)
        {
            if (HasPermission(Permission.ReadAllMail))
            {
                var mail = await _mailRepository.GetByIdAsync(mailId);
                if (mail.ToUserId == null)
                {
                    await _mailRepository.MarkAsReadAsync(mailId);
                    return;
                }
            }
            var authId = GetClaimId(ClaimType.UserId);
            _logger.LogError("User {UserId} doesn't have permission mark mail {MailId} as read.",
                authId,
                mailId);
            throw new GraException("Permission denied.");
        }

        public async Task<Mail> MCSendAsync(Mail mail)
        {
            var authId = GetClaimId(ClaimType.UserId);

            if (HasPermission(Permission.MailParticipants))
            {
                var user = await _userRepository.GetByIdAsync(mail.ToUserId.Value);
                if (user != null)
                {
                    mail.FromUserId = 0;
                    mail.CanParticipantDelete = true;
                    mail.IsNew = true;
                    mail.IsDeleted = false;
                    mail.SiteId = GetClaimId(ClaimType.SiteId);

                    await _cache.RemoveAsync(UnreadMailCacheKey(mail.SiteId, (int)mail.ToUserId));

                    return await _mailRepository.AddSaveAsync(authId, mail);
                }
                else
                {
                    throw new GraException("User doesn't exist");
                }
            }
            else
            {
                _logger.LogError("User {CurrentUserId} doesn't have permission to send a mail to {UserId}.",
                    authId,
                    mail.ToUserId);
                throw new GraException("Permission denied");
            }
        }

        public async Task<Mail> MCSendReplyAsync(Mail mail)
        {
            var authId = GetClaimId(ClaimType.UserId);
            var siteId = GetCurrentSiteId();
            if (mail.InReplyToId != null
               && HasPermission(Permission.MailParticipants))
            {
                var inReplyToMail = await _mailRepository.GetByIdAsync(mail.InReplyToId.Value);
                mail.ThreadId = inReplyToMail?.ThreadId ?? mail.InReplyToId.Value;
                mail.FromUserId = 0;
                mail.ToUserId = inReplyToMail.FromUserId;
                mail.CanParticipantDelete = true;
                mail.IsNew = true;
                mail.IsDeleted = false;
                mail.SiteId = siteId;

                await _cache.RemoveAsync(UnreadMailCacheKey(siteId, (int)mail.ToUserId));
                if (!inReplyToMail.IsRepliedTo)
                {
                    await _mailRepository.MarkAdminReplied(inReplyToMail.Id);
                    await _cache.RemoveAsync(UnhandledMailCount(siteId));
                }
                return await _mailRepository.AddSaveAsync(authId, mail);
            }
            else
            {
                _logger.LogError("User {UserId} doesn't have permission to reply to mail {MailId}.",
                    authId,
                    mail.InReplyToId);
                throw new GraException("Permission denied");
            }
        }

        public async Task<DataWithCount<ICollection<Broadcast>>> PageBroadcastsAsync(BroadcastFilter filter)
        {
            var authId = GetClaimId(ClaimType.UserId);

            if (HasPermission(Permission.SendBroadcastMail))
            {
                filter.SiteId = GetClaimId(ClaimType.SiteId);

                return new DataWithCount<ICollection<Broadcast>>
                {
                    Data = await _broadcastRepository.PageAsync(filter),
                    Count = await _broadcastRepository.CountAsync(filter)
                };
            }
            else
            {
                _logger.LogError("User {UserId} doesn't have permission to view broadcasts.",
                    authId);
                throw new GraException("Permission denied");
            }
        }

        public async Task RemoveAsync(int mailId)
        {
            var authId = GetClaimId(ClaimType.UserId);
            var activeId = GetActiveUserId();
            bool canDeleteAll = HasPermission(Permission.DeleteAnyMail);
            var mail = await _mailRepository.GetByIdAsync(mailId);
            if (mail == null)
            {
                _logger.LogInformation("User {UserId} tried to remove {MailId} which doesn't exist.",
                    activeId,
                    mailId);
                return;
            }
            else
            {
                if (mail.ToUserId == activeId || canDeleteAll)
                {
                    var siteId = GetCurrentSiteId();
                    if (mail.ToUserId != null)
                    {
                        await _cache.RemoveAsync(UnreadMailCacheKey(siteId, (int)mail.ToUserId));
                    }
                    else
                    {
                        await _cache.RemoveAsync(UnhandledMailCount(siteId));
                    }
                    await _mailRepository.RemoveSaveAsync(authId, mailId);
                    return;
                }
            }
            _logger.LogError("User {UserId} doesn't have permission remove mail {MailId}.",
                activeId,
                mailId);
            throw new GraException("Permission denied.");
        }

        public async Task RemoveBroadcastAsync(int id)
        {
            var authId = GetClaimId(ClaimType.UserId);

            if (HasPermission(Permission.SendBroadcastMail))
            {
                var broadcast = await _broadcastRepository.GetByIdAsync(id);

                if (broadcast.SendAt <= _dateTimeProvider.Now)
                {
                    throw new GraException("This Broadcast has already been sent.");
                }

                await _broadcastRepository.RemoveSaveAsync(authId, id);
            }
            else
            {
                _logger.LogError("User {UserId} doesn't have permission to delete broadcasts.",
                    authId);
                throw new GraException("Permission denied");
            }
        }

        public async Task<Mail> SendAsync(Mail mail)
        {
            var siteId = GetClaimId(ClaimType.SiteId);
            var authId = GetClaimId(ClaimType.UserId);
            var activeUserId = GetActiveUserId();
            if (mail.ToUserId == null)
            {
                mail.FromUserId = activeUserId;
                mail.IsNew = true;
                mail.IsDeleted = false;
                mail.SiteId = siteId;

                await _cache.RemoveAsync(UnhandledMailCount(siteId));

                return await _mailRepository.AddSaveAsync(authId, mail);
            }
            else
            {
                _logger.LogError("User {CurrentUserId} doesn't have permission to send a mail to {UserId}.",
                    activeUserId,
                    mail.ToUserId);
                throw new GraException("Permission denied");
            }
        }

        public async Task<Mail> SendReplyAsync(Mail mail)
        {
            var siteId = GetClaimId(ClaimType.SiteId);
            var authId = GetClaimId(ClaimType.UserId);
            var activeUserId = GetActiveUserId();
            var inReplyToMail = await _mailRepository.GetByIdAsync(mail.InReplyToId.Value);
            if (inReplyToMail.ToUserId == activeUserId)
            {
                mail.ThreadId = inReplyToMail.ThreadId ?? mail.InReplyToId.Value;
                mail.FromUserId = activeUserId;
                mail.ToUserId = null;
                mail.IsNew = true;
                mail.IsDeleted = false;
                mail.SiteId = siteId;
                if (!inReplyToMail.IsRepliedTo)
                {
                    inReplyToMail.IsRepliedTo = true;
                    await _mailRepository.UpdateAsync(authId, inReplyToMail);
                }

                await _cache.RemoveAsync(UnhandledMailCount(siteId));

                return await _mailRepository.AddSaveAsync(authId, mail);
            }
            else
            {
                _logger.LogError("User {CurrentUserId} doesn't have permission to reply to a mail sent to {UserId}.",
                    activeUserId,
                    mail.ToUserId);
                throw new GraException("Permission Denied");
            }
        }

        public async Task<Mail> SendSystemMailAsync(Mail mail, int? siteId = null)
        {
            var user = await _userRepository.GetByIdAsync(mail.ToUserId.Value);
            if (user != null)
            {
                mail.FromUserId = 0;
                mail.IsNew = true;
                mail.IsDeleted = false;
                mail.CreatedAt = _dateTimeProvider.Now;
                mail.SiteId = siteId ?? GetClaimId(ClaimType.SiteId);

                await _cache.RemoveAsync(UnreadMailCacheKey(mail.SiteId, (int)mail.ToUserId));

                if (mail.TemplateDictionary?.Count > 0)
                {
                    var stubble = new StubbleBuilder().Build();
                    mail.Body = await stubble.RenderAsync(mail.Body, mail.TemplateDictionary);
                }

                return await _mailRepository.AddSaveNoAuditAsync(mail);
            }
            else
            {
                throw new GraException("User doesn't exist");
            }
        }

        public async Task SendUserBroadcastsAsync(int userId, bool includeHousehold,
            bool newUser = false, bool userIdIsCurrentUser = false)
        {
            var authUserId = userIdIsCurrentUser ? userId : GetClaimId(ClaimType.UserId);

            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                _logger.LogError("Unable to find user {UserId} to send broadcasts.",
                    userId);
            }
            else
            {
                var newBroadcasts = await _broadcastRepository.GetNewBroadcastsAsync(user.SiteId,
                    user.LastBroadcast);

                if (newBroadcasts?.Any() == true)
                {
                    var lastBroadcastDate = newBroadcasts.Last().SendAt;
                    if (newUser)
                    {
                        newBroadcasts = newBroadcasts.Where(_ => _.SendToNewUsers);
                    }
                    foreach (var broadcast in newBroadcasts)
                    {
                        var mail = new Mail()
                        {
                            Subject = broadcast.Subject,
                            Body = broadcast.Body,
                            FromUserId = 0,
                            CanParticipantDelete = true,
                            IsNew = true,
                            IsDeleted = false,
                            SiteId = user.SiteId,
                            IsBroadcast = true,
                            ToUserId = userId
                        };

                        await _mailRepository.AddSaveAsync(authUserId, mail);
                    }
                    user.LastBroadcast = lastBroadcastDate.Value;
                    await _userRepository.UpdateSaveAsync(authUserId, user);

                    await _cache.RemoveAsync(UnreadMailCacheKey(user.SiteId, userId));
                }
            }

            if (includeHousehold)
            {
                var householdMembers = await _userRepository.GetHouseholdAsync(userId);
                foreach (var member in householdMembers)
                {
                    await SendUserBroadcastsAsync(member.Id, false);
                }
            }
        }

        public async Task<bool> UserHasUnreadAsync(int userId)
        {
            var authId = GetClaimId(ClaimType.UserId);
            if (userId == authId || HasPermission(Permission.ReadAllMail))
            {
                return await _mailRepository.UserHasUnreadAsync(userId);
            }
            else
            {
                _logger.LogError("User {CurrentUserId} doesn't have permission to view messages for {UserId}.",
                    authId,
                    userId);
                throw new GraException("Permission denied.");
            }
        }

        private static string UnhandledMailCount(int siteId)
        {
            return string.Format(CultureInfo.InvariantCulture,
                CacheKey.UnhandledMailCount,
                siteId);
        }

        private static string UnreadMailCacheKey(int siteId, int userId)
        {
            return string.Format(CultureInfo.InvariantCulture,
                CacheKey.UserUnreadMailCount,
                siteId,
                userId);
        }
    }
}
