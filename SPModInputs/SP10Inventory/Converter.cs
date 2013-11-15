using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using Microsoft.SharePoint.Diagnostics;
using Microsoft.SharePoint.MobileMessage;

using Splunk.ModularInputs;

namespace Splunk.SharePoint2010.Inventory
{
    class Converter
    {
        /// <summary>
        /// Get a checksum of a hash
        /// </summary>
        /// <param name="hash">The hash object</param>
        /// <returns>The checksum</returns>
        internal static string ToChecksum(Dictionary<string, string> hash)
        {
            List<string> builder = new List<string>();

            foreach (var key in hash.Keys)
            {
                builder.Add(string.Format("{0}={1}", key, hash[key]));
            }
            builder.Sort();

            return Utility.CalculateMD5Hash(string.Join("\n", builder.ToArray()));
        }

        /// <summary>
        /// Convert an SPFarm object into a hash
        /// </summary>
        /// <param name="farm">The SPFarm object</param>
        /// <returns>A hash of the properties</returns>
        internal static Dictionary<string, string> ToHash(SPFarm farm)
        {
            Dictionary<string, string> emitter = new Dictionary<string, string>();

            SPDiagnosticsService diagService = SPDiagnosticsService.LocalToFarm(farm);
            emitter.Add("Id", farm.Id.ToString());
            emitter.Add("Name", farm.Name);
            emitter.Add("DisplayName", farm.DisplayName);
            emitter.Add("Status", farm.Status.ToString());
            emitter.Add("Version", farm.Version.ToString());
            emitter.Add("BuildVersion", farm.BuildVersion.ToString());
            emitter.Add("DiskSizeRequired", farm.DiskSizeRequired.ToString());
            emitter.Add("PersistedFileChunkSize", farm.PersistedFileChunkSize.ToString());
            emitter.Add("CEIPEnabled", diagService.CEIPEnabled.ToString());
            emitter.Add("DefaultServiceAccount", farm.DefaultServiceAccount.LookupName());
            emitter.Add("DownloadErrorReportingUpdates", diagService.DownloadErrorReportingUpdates.ToString());
            emitter.Add("ErrorReportingAutomaticUpload", diagService.ErrorReportingAutomaticUpload.ToString());
            emitter.Add("IsBackwardsCompatible", farm.IsBackwardsCompatible.ToString());
            emitter.Add("PasswordChangeEmailAddress", farm.PasswordChangeEmailAddress);
            emitter.Add("PasswordChangeGuardTime", farm.PasswordChangeGuardTime.ToString());
            emitter.Add("PasswordChangeMaximumTries", farm.PasswordChangeMaximumTries.ToString());
            emitter.Add("DaysBeforePasswordExpirationsToSendEmail", farm.DaysBeforePasswordExpirationToSendEmail.ToString());

            return emitter;
        }

        /// <summary>
        /// Convert an SPAlternateUrl object into a hash
        /// </summary>
        /// <param name="alternateUrl">The SPAlternateUrl object</param>
        /// <returns>A hash of the properties</returns>
        internal static Dictionary<string, string> ToHash(SPAlternateUrl alternateUrl)
        {
            Dictionary<string, string> emitter = new Dictionary<string, string>();

            emitter.Add("Uri", alternateUrl.Uri.ToString());
            emitter.Add("Zone", alternateUrl.UrlZone.ToString());
            emitter.Add("IncomingUrl", alternateUrl.IncomingUrl.ToString());

            return emitter;
        }

        /// <summary>
        /// Convert an SPServer object into a hash
        /// </summary>
        /// <param name="server">The SPServer object</param>
        /// <returns>A hash of the properties</returns>
        internal static Dictionary<string, string> ToHash(SPServer server)
        {
            Dictionary<string, string> emitter = new Dictionary<string, string>();

            emitter.Add("Id", server.Id.ToString());
            emitter.Add("Name", server.Name);
            emitter.Add("DisplayName", server.DisplayName);
            emitter.Add("Status", server.Status.ToString());
            emitter.Add("Version", server.Version.ToString());
            emitter.Add("Role", server.Role.ToString());

            return emitter;
        }

        /// <summary>
        /// Convert an SPServiceInstance object into a hash
        /// </summary>
        /// <param name="service">The SPServiceInstance object</param>
        /// <returns>A hash of the properties</returns>
        internal static Dictionary<string, string> ToHash(SPServiceInstance service)
        {
            Dictionary<string, string> emitter = new Dictionary<string, string>();

            emitter.Add("Id", service.Id.ToString());
            emitter.Add("Name", service.Name);
            emitter.Add("DisplayName", service.DisplayName);
            emitter.Add("Status", service.Status.ToString());
            emitter.Add("Version", service.Version.ToString());
            emitter.Add("Hidden", service.Hidden.ToString());
            emitter.Add("Instance", service.Instance.ToString());
            // Note: Roles is not a valid thing in SharePoint 2010, so make it blank
            emitter.Add("Roles", "");
            emitter.Add("Server", service.Server.Name);
            emitter.Add("Service", service.ToString());
            emitter.Add("SystemService", service.SystemService.ToString());

            return emitter;
        }

        /// <summary>
        /// Convert an SPDiagnosticsProvider object into a hash
        /// </summary>
        /// <param name="diagProvider">The SPDiagnosticsProvider object</param>
        /// <returns>A hash of the properties</returns>
        internal static Dictionary<string, string> ToHash(SPDiagnosticsProvider diagProvider)
        {
            Dictionary<string, string> emitter = new Dictionary<string, string>();

            emitter.Add("Id", diagProvider.Id.ToString());
            emitter.Add("Name", diagProvider.Name);
            emitter.Add("DisplayName", diagProvider.DisplayName);
            emitter.Add("Title", diagProvider.Title);
            emitter.Add("TableName", diagProvider.TableName);
            emitter.Add("LastRunTime", diagProvider.LastRunTime.ToString("u"));
            emitter.Add("MaxTotalSizeInBytes", diagProvider.MaxTotalSizeInBytes.ToString());
            emitter.Add("RetentiionPeriod", diagProvider.RetentionPeriod.ToString());
            emitter.Add("LockType", diagProvider.LockType.ToString());
            emitter.Add("Schedule", diagProvider.Schedule.ToString());
            emitter.Add("Retry", diagProvider.Retry.ToString());
            emitter.Add("IsDisabled", diagProvider.IsDisabled.ToString());
            emitter.Add("VerboseTracingEnabled", diagProvider.VerboseTracingEnabled.ToString());
            emitter.Add("EnableBackup", diagProvider.EnableBackup.ToString());
            emitter.Add("DiskSizeRequired", diagProvider.DiskSizeRequired.ToString());
            emitter.Add("Status", diagProvider.Status.ToString());
            emitter.Add("Version", diagProvider.Version.ToString());
            emitter.Add("Enabled", diagProvider.IsDisabled ? "False" : "True");
            emitter.Add("Server", diagProvider.Server == null ? "" : diagProvider.Server.Name);

            return emitter;
        }

        /// <summary>
        /// Convert an SPFeatureDefinition object into a hash
        /// </summary>
        /// <param name="featureDefinition">The SPFeatureDefinition object</param>
        /// <returns>A hash of the properties</returns>
        internal static Dictionary<string, string> ToHash(SPFeatureDefinition featureDefinition)
        {
            Dictionary<string, string> emitter = new Dictionary<string, string>();

            emitter.Add("Id", featureDefinition.Id.ToString());
            emitter.Add("Name", featureDefinition.Name);
            emitter.Add("DisplayName", featureDefinition.DisplayName);
            emitter.Add("ActivateOnDefault", featureDefinition.ActivateOnDefault.ToString());
            emitter.Add("AlwaysForceInstall", featureDefinition.AlwaysForceInstall.ToString());
            emitter.Add("AutoActivateInCentralAdmin", featureDefinition.AutoActivateInCentralAdmin.ToString());
            emitter.Add("Hidden", featureDefinition.Hidden.ToString());
            emitter.Add("ReceiverAssembly", Utility.Nullable(featureDefinition.ReceiverAssembly));
            emitter.Add("ReceiverClass", Utility.Nullable(featureDefinition.ReceiverClass));
            emitter.Add("RequireResources", featureDefinition.RequireResources.ToString());
            emitter.Add("RootDirectory", featureDefinition.RootDirectory);
            emitter.Add("Scope", featureDefinition.Scope.ToString());
            emitter.Add("Status", featureDefinition.Status.ToString());
            emitter.Add("UIVersion", Utility.Nullable(featureDefinition.UIVersion));
            emitter.Add("Version", featureDefinition.Version.ToString());

            return emitter;
        }

        /// <summary>
        /// Convert an SPWebTemplate object into a hash
        /// </summary>
        /// <param name="webTemplate">The SPWebTemplate object</param>
        /// <returns>A hash of the properties</returns>
        internal static Dictionary<string, string> ToHash(SPWebTemplate webTemplate)
        {
            Dictionary<string, string> emitter = new Dictionary<string, string>();

            emitter.Add("AllowGlobalFeatureAssociations", webTemplate.AllowGlobalFeatureAssociations.ToString());
            // CompatibilityLevel does not exist in SharePoint 2010
            emitter.Add("CompatibilityLevel", "");
            emitter.Add("Description", webTemplate.Description);
            emitter.Add("DisplayCategory", webTemplate.DisplayCategory);
            emitter.Add("FilterCategories", webTemplate.FilterCategories);
            emitter.Add("Id", webTemplate.ID.ToString());
            emitter.Add("ImageUrl", webTemplate.ImageUrl);
            emitter.Add("IsCustomTemplate", webTemplate.IsCustomTemplate.ToString());
            // IsFarmWideTemplate does not exist in SharePoint 2010
            emitter.Add("IsFarmWideTemplate", "True");
            emitter.Add("IsHidden", webTemplate.IsHidden.ToString());
            emitter.Add("IsRootWebOnly", webTemplate.IsRootWebOnly.ToString());
            emitter.Add("IsSubWebOnly", webTemplate.IsSubWebOnly.ToString());
            emitter.Add("Lcid", webTemplate.Lcid.ToString());
            emitter.Add("Name", webTemplate.Name);
            emitter.Add("ProvisionAssembly", Utility.Nullable(webTemplate.ProvisionAssembly));
            emitter.Add("ProvisionClass", Utility.Nullable(webTemplate.ProvisionClass));
            emitter.Add("ProvisionData", Utility.Nullable(webTemplate.ProvisionData));
            emitter.Add("SupportsMultilingualUI", webTemplate.SupportsMultilingualUI.ToString());
            emitter.Add("Title", webTemplate.Title.ToString());
            // UserLicensingId does not exist in SharePoint 2010
            emitter.Add("UserLicensingId", "");
            emitter.Add("VisibilityFeatureDependencyId", webTemplate.VisibilityFeatureDependencyId.ToString());

            return emitter;
        }

        /// <summary>
        /// Convert an SPWebApplication object into a hash
        /// </summary>
        /// <param name="webApplication">The SPWebApplication object</param>
        /// <returns>A hash of the properties</returns>
        internal static Dictionary<string, string> ToHash(SPWebApplication webApplication)
        {
            Dictionary<string, string> emitter = new Dictionary<string, string>();

            emitter.Add("Id", webApplication.Id.ToString());
            emitter.Add("Name", webApplication.Name);
            emitter.Add("DisplayName", webApplication.DisplayName);
            emitter.Add("Status", webApplication.Status.ToString());
            emitter.Add("Version", webApplication.Version.ToString());
            emitter.Add("Url", webApplication.GetResponseUri(SPUrlZone.Default).AbsoluteUri.ToString());
            // AlertFlags is obsolete in SharePoint 2010
            emitter.Add("AlertFlags", "");
            emitter.Add("AlertsEnabled", webApplication.AlertsEnabled.ToString());
            // AlertsEventBatchSize is obsolete in SharePoint 2010
            emitter.Add("AlertsEventBatchSize", "");
            emitter.Add("AlertsLimited", webApplication.AlertsLimited.ToString());
            emitter.Add("AlertsMaximum", webApplication.AlertsMaximum.ToString());
            emitter.Add("AlertsMaximumQuerySet", webApplication.AlertsMaximumQuerySet.ToString());
            emitter.Add("AllowAccessToWebpartCatalog", webApplication.AllowAccessToWebPartCatalog.ToString());
            emitter.Add("AllowContributorsToEditScriptableParts", webApplication.AllowContributorsToEditScriptableParts.ToString());
            emitter.Add("AllowDesigner", webApplication.AllowDesigner.ToString());
            emitter.Add("AllowInlineDownloadedMimeTypes", webApplication.AllowedInlineDownloadedMimeTypes.ToString());
            emitter.Add("AllowHighCharacterListFolderNames", webApplication.AllowHighCharacterListFolderNames.ToString());
            emitter.Add("AllowMasterPageEditing", webApplication.AllowMasterPageEditing.ToString());
            emitter.Add("AllowOMCodeOverrideThrottleSettings", webApplication.AllowOMCodeOverrideThrottleSettings.ToString());
            emitter.Add("AllowPartToPartCommunication", webApplication.AllowPartToPartCommunication.ToString());
            emitter.Add("AllowRevertFromTemplate", webApplication.AllowRevertFromTemplate.ToString());
            emitter.Add("AllowSilverlightPrompt", webApplication.AllowSilverlightPrompt.ToString());
            emitter.Add("AlwaysProcessDocuments", webApplication.AlwaysProcessDocuments.ToString());
            emitter.Add("ApplicationPool", webApplication.ApplicationPool.Id.ToString());
            emitter.Add("AutomaticallyDeleteUnusedSiteCollections", webApplication.AutomaticallyDeleteUnusedSiteCollections.ToString());
            emitter.Add("BrowserCEIPEnabled", webApplication.BrowserCEIPEnabled.ToString());
            emitter.Add("CascadeDeleteMaximumItemLimit", webApplication.CascadeDeleteMaximumItemLimit.ToString());
            emitter.Add("CascadeDeleteTimeoutMultiplier", webApplication.CascadeDeleteTimeoutMultiplier.ToString());
            emitter.Add("ChangeLogExpirationEnabled", webApplication.ChangeLogExpirationEnabled.ToString());
            emitter.Add("ChangeLogRetentionPeriod", webApplication.ChangeLogRetentionPeriod.ToString());
            emitter.Add("DaysToShowNewIndicator", webApplication.DaysToShowNewIndicator.ToString());
            emitter.Add("DefaultQuotaTemplate", Utility.Nullable(webApplication.DefaultQuotaTemplate));
            emitter.Add("DefaultServerComment", Utility.Nullable(webApplication.DefaultServerComment));
            emitter.Add("DefaultTimeZone", webApplication.DefaultTimeZone.ToString());
            emitter.Add("DisableCoauthoring", webApplication.DisableCoauthoring.ToString());
            emitter.Add("DiskSizeRequired", webApplication.DiskSizeRequired.ToString());
            emitter.Add("DocumentConversionsEnabled", webApplication.DocumentConversionsEnabled.ToString());
            emitter.Add("EmailToNoPermissionWorkflowParticipantsEnabled", webApplication.EmailToNoPermissionWorkflowParticipantsEnabled.ToString());
            // EventHandlersEnabled is obsolete in SharePoint 2010
            emitter.Add("EventHandlersEnabled", "False");
            emitter.Add("EventLogRetentionPeriod", webApplication.EventLogRetentionPeriod.ToString());
            emitter.Add("ExternalUrlZone", Utility.Nullable(webApplication.ExternalUrlZone));
            emitter.Add("ExternalWorkflowParticipantsEnabled", webApplication.ExternalWorkflowParticipantsEnabled.ToString());
            emitter.Add("FileNotFoundPage", webApplication.FileNotFoundPage);
            emitter.Add("IncomingEmailServerAddress", webApplication.IncomingEmailServerAddress);
            emitter.Add("IsAdministrationWebApplication", webApplication.IsAdministrationWebApplication.ToString());
            emitter.Add("MaximumFileSize", webApplication.MaximumFileSize.ToString());
            emitter.Add("MaxItemsPerThrottledOperationOverride", webApplication.MaxItemsPerThrottledOperationOverride.ToString());
            emitter.Add("MaxItemsPerThrottledOperationWarningLevel", webApplication.MaxItemsPerThrottledOperationWarningLevel.ToString());
            emitter.Add("MaxListItemRowStorage", webApplication.MaxListItemRowStorage.ToString());
            emitter.Add("MaxQueryLookupFields", webApplication.MaxQueryLookupFields.ToString());
            emitter.Add("MaxSizePerCellStorageOperation", webApplication.MaxSizePerCellStorageOperation.ToString());
            emitter.Add("MaxUniquePermScopesPerList", webApplication.MaxUniquePermScopesPerList.ToString());
            emitter.Add("OfficialFileUrl", Utility.Nullable(webApplication.OfficialFileUrl));
            emitter.Add("OutboundMailCodePage", webApplication.OutboundMailCodePage.ToString());
            emitter.Add("OutboundMailReplyToAddress", webApplication.OutboundMailReplyToAddress);
            emitter.Add("OutboundMailSenderAddress", webApplication.OutboundMailSenderAddress);
            emitter.Add("OutboundMailServiceInstance", webApplication.OutboundMailServiceInstance == null ? "" : string.Format("{id={0},name={1}}", webApplication.OutboundMailServiceInstance.Id.ToString(), webApplication.OutboundMailServiceInstance.Name));
            emitter.Add("OutboundMmsServiceAccount", Utility.Nullable(MobileMessagingAccountTOJSON(webApplication.OutboundMmsServiceAccount)));
            emitter.Add("OutboundSmsServiceAccount", Utility.Nullable(MobileMessagingAccountTOJSON(webApplication.OutboundSmsServiceAccount)));
            emitter.Add("PresenceEnabled", webApplication.PresenceEnabled.ToString());
            // E-mail Inserts have been removed in SharePoint 2010
            emitter.Add("PublicFolderRootUrl", "");
            emitter.Add("RecycleBinEnabled", webApplication.RecycleBinEnabled.ToString());
            emitter.Add("RecycleBinCleanupEnabled", webApplication.RecycleBinCleanupEnabled.ToString());
            emitter.Add("RecycleBinRetentionPeriod", webApplication.RecycleBinRetentionPeriod.ToString());
            emitter.Add("RenderingFromMetainfoEnabled", webApplication.RenderingFromMetainfoEnabled.ToString());
            emitter.Add("RequireContactForSelfServiceSiteCreation", webApplication.RequireContactForSelfServiceSiteCreation.ToString());
            emitter.Add("ScopeExternalConnectionsToSiteSubscriptions", webApplication.ScopeExternalConnectionsToSiteSubscriptions.ToString());
            emitter.Add("SecondStageRecycleBinQuota", webApplication.SecondStageRecycleBinQuota.ToString());
            emitter.Add("SelfServiceSiteCreationEnabled", webApplication.SelfServiceSiteCreationEnabled.ToString());
            emitter.Add("SendLoginCredentialsByEmail", webApplication.SendLoginCredentialsByEmail.ToString());
            emitter.Add("SendUnusedSiteCollectionNotifications", webApplication.SendUnusedSiteCollectionNotifications.ToString());
            emitter.Add("ShowURLStructure", webApplication.ShowURLStructure.ToString());
            emitter.Add("SyndicationEnabled", webApplication.SyndicationEnabled.ToString());
            emitter.Add("UnusedSiteNotificationPeriod", webApplication.UnusedSiteNotificationPeriod.ToString());
            emitter.Add("UnusedSiteNotificationsBeforeDeletion", webApplication.UnusedSiteNotificationsBeforeDeletion.ToString());
            emitter.Add("UseClaimsAuthentication", webApplication.UseClaimsAuthentication.ToString());
            emitter.Add("UserDefinedWorkflowMaximumComplexity", Utility.Nullable(webApplication.UserDefinedWorkflowMaximumComplexity));
            emitter.Add("UserDefinedWorkflowsEnabled", webApplication.UserDefinedWorkflowsEnabled.ToString());

            return emitter;
        }

        /// <summary>
        /// Convert an SPMobileMessagingAccount object to JSON
        /// </summary>
        /// <param name="instance">The SPMobileMessagingAccount object</param>
        /// <returns>A string</returns>
        private static string MobileMessagingAccountTOJSON(SPMobileMessagingAccount instance)
        {
            if (instance == null) return "";
            return string.Format("{url={0},provider={1},userid={2}}", instance.ServiceUrl.ToString(), instance.ServiceProvider.EnglishName, instance.UserId);
        }

        /// <summary>
        /// Convert an SPApplicationPool object into a hash
        /// </summary>
        /// <param name="applicationPool">The SPApplicationPool object</param>
        /// <returns>A hash of the properties</returns>
        internal static Dictionary<string, string> ToHash(SPApplicationPool applicationPool)
        {
            Dictionary<string, string> emitter = new Dictionary<string, string>();

            emitter.Add("Id", applicationPool.Id.ToString());
            emitter.Add("Name", applicationPool.Name);
            emitter.Add("DisplayName", applicationPool.DisplayName);
            emitter.Add("Status", applicationPool.Status.ToString());
            emitter.Add("Version", applicationPool.Version.ToString());
            emitter.Add("Username", applicationPool.Username);
            emitter.Add("CurrentIdentityType", applicationPool.CurrentIdentityType.ToString());
            emitter.Add("CurrentSecurityIdentifier", applicationPool.CurrentSecurityIdentifier.ToString());
            emitter.Add("IsCredentialUpdateEnabled", applicationPool.IsCredentialUpdateEnabled.ToString());
            emitter.Add("IsCredentialDeploymentEnabled", applicationPool.IsCredentialDeploymentEnabled.ToString());

            return emitter;
        }

        /// <summary>
        /// Convert an SPContentDatabase object into a hash
        /// </summary>
        /// <param name="webAppId">The Web Application Id</param>
        /// <param name="database">The SPContentDatabase object</param>
        /// <returns>A hash of the properties</returns>
        internal static Dictionary<string, string> ToHash(Guid webAppId, SPContentDatabase database)
        {
            Dictionary<string, string> emitter = new Dictionary<string, string>();

            emitter.Add("Id", database.Id.ToString());
            emitter.Add("Name", database.Name);
            emitter.Add("DisplayName", database.DisplayName);
            emitter.Add("Status", database.Status.ToString());
            emitter.Add("Version", database.Version.ToString());
            emitter.Add("CurrentSiteCount", database.CurrentSiteCount.ToString());
            emitter.Add("MaximumSiteCount", database.MaximumSiteCount.ToString());
            emitter.Add("WarningSiteCount", database.WarningSiteCount.ToString());
            emitter.Add("DatabaseConnectionString", database.DatabaseConnectionString);
            emitter.Add("DiskSizeRequired", database.DiskSizeRequired.ToString());
            emitter.Add("Exists", database.Exists.ToString());
            emitter.Add("ExistsInFarm", database.ExistsInFarm.ToString());
            emitter.Add("FailoverServer", database.FailoverServer == null ? "" : database.FailoverServer.Name);
            emitter.Add("FailoverServerInstance", database.FailoverServiceInstance == null ? "" : database.FailoverServiceInstance.Name);
            emitter.Add("IncludeInVssBackup", database.IncludeInVssBackup.ToString());
            emitter.Add("IsAttachedToFarm", database.IsAttachedToFarm.ToString());
            emitter.Add("IsReadOnly", database.IsReadOnly.ToString());
            emitter.Add("NormalizedDataSource", Utility.Nullable(database.NormalizedDataSource));
            emitter.Add("Server", Utility.Nullable(database.Server));
            emitter.Add("WebApplicationId", webAppId.ToString());

            return emitter;
        }

        /// <summary>
        /// Convert an SPFeature object into a hash
        /// </summary>
        /// <param name="webAppId">The Web Application Id for this feature</param>
        /// <param name="feature">The SPFeature object</param>
        /// <returns>A hash of the properties</returns>
        internal static Dictionary<string, string> ToHash(Guid webAppId, SPFeature feature)
        {
            Dictionary<string, string> emitter = new Dictionary<string, string>();

            emitter.Add("Id", feature.DefinitionId.ToString());
            emitter.Add("Version", feature.Version.ToString());
            emitter.Add("FeatureDefinitionScope", feature.FeatureDefinitionScope.ToString());
            emitter.Add("WebApplicationId", webAppId.ToString());

            return emitter;
        }

        /// <summary>
        /// Convert an SPPolicy object into a hash
        /// </summary>
        /// <param name="webAppId">The Web Application Id for this feature</param>
        /// <param name="policy">The SPPolicy object</param>
        /// <returns>A hash of the properties</returns>
        internal static Dictionary<string, string> ToHash(Guid webAppId, SPPolicy policy)
        {
            Dictionary<string, string> emitter = new Dictionary<string, string>();

            SPPolicy.SPPolicyRoleBindingCollection roleBindings = policy.PolicyRoleBindings;
            List<string> roles = new List<string>();
            foreach (SPPolicyRole role in roleBindings)
            {
                roles.Add(role.Name);
            }

            emitter.Add("DisplayName", policy.DisplayName);
            emitter.Add("IsSystemUser", policy.IsSystemUser.ToString());
            emitter.Add("UserName", policy.UserName);
            emitter.Add("PolicyRoleBinding", string.Join(",", roles.ToArray()));
            emitter.Add("WebApplicationId", webAppId.ToString());

            return emitter;
        }

        /// <summary>
        /// Convert an SPPrefix object into a hash
        /// </summary>
        /// <param name="webAppId">The Web Application Id for this feature</param>
        /// <param name="prefix">The SPPrefix object</param>
        /// <returns>A hash of the properties</returns>
        internal static Dictionary<string, string> ToHash(Guid webAppId, SPPrefix prefix)
        {
            Dictionary<string, string> emitter = new Dictionary<string, string>();

            emitter.Add("Name", string.Format("/{0}", prefix.Name == null || prefix.Name.Equals("") ? "" : prefix.Name));
            emitter.Add("PrefixType", prefix.PrefixType.ToString());
            emitter.Add("WebApplicationId", webAppId.ToString());

            return emitter;
        }

        /// <summary>
        /// Convert an SPSite object into a hash
        /// </summary>
        /// <param name="webAppId">The Web Application Id for this feature</param>
        /// <param name="site">The SPSite object</param>
        /// <returns>A hash of the properties</returns>
        internal static Dictionary<string, string> ToHash(Guid webAppId, SPSite site)
        {
            Dictionary<string, string> emitter = new Dictionary<string, string>();

            emitter.Add("Id", site.ID.ToString());
            emitter.Add("Url", site.Url.ToString());
            emitter.Add("AdministrationSiteType", site.AdministrationSiteType.ToString());
            emitter.Add("AllowDesigner", site.AllowDesigner.ToString());
            emitter.Add("AllowMasterPageEditing", site.AllowMasterPageEditing.ToString());
            emitter.Add("AllowRevertFromTemplate", site.AllowRevertFromTemplate.ToString());
            emitter.Add("AllowRssFeeds", site.AllowRssFeeds.ToString());
            emitter.Add("AllowUnsafeUpdates", site.AllowUnsafeUpdates.ToString());
            emitter.Add("Audit", site.Audit == null ? "0" : ((int)site.Audit.AuditFlags).ToString("X"));
            emitter.Add("AuditLogTrimmingCallout", Utility.Nullable(site.AuditLogTrimmingCallout));
            emitter.Add("AuditLogTrimmingRetention", site.AuditLogTrimmingRetention.ToString());
            emitter.Add("AverageResourceUsage", site.AverageResourceUsage.ToString());
            emitter.Add("BrowserDocumentsEnabled", site.BrowserDocumentsEnabled.ToString());
            emitter.Add("CatchAccessDeniedException", site.CatchAccessDeniedException.ToString());
            emitter.Add("CertificationDate", site.CertificationDate.ToUniversalTime().ToString("u"));
            emitter.Add("ContentDatabaseId", site.ContentDatabase.Id.ToString());
            emitter.Add("CurrentResourceUsage", site.CurrentResourceUsage.ToString());
            emitter.Add("DeadWebNotificationCount", site.DeadWebNotificationCount.ToString());
            emitter.Add("HostHeaderIsSiteName", site.HostHeaderIsSiteName.ToString());
            emitter.Add("HostName", Utility.Nullable(site.HostName));
            emitter.Add("IISAllowsAnonymous", site.IISAllowsAnonymous.ToString());
            emitter.Add("Impersonating", site.Impersonating.ToString());
            emitter.Add("LastContentModifiedDate", site.LastContentModifiedDate == null ? "Never" : site.LastContentModifiedDate.ToUniversalTime().ToString("u"));
            emitter.Add("LastSecurityModifiedDate", site.LastSecurityModifiedDate == null ? "Never" : site.LastSecurityModifiedDate.ToUniversalTime().ToString("u"));
            emitter.Add("LockIssue", Utility.Nullable(site.LockIssue));
            // MaintenanceMode is not available in SharePoint 2010
            emitter.Add("MaintenanceMode", "False");
            emitter.Add("Owner", site.Owner == null ? "" : site.Owner.LoginName);
            emitter.Add("Port", site.Port.ToString());
            emitter.Add("PortalName", Utility.Nullable(site.PortalName));
            emitter.Add("PortalUrl", Utility.Nullable(site.PortalUrl));
            emitter.Add("Protocol", Utility.Nullable(site.Protocol));
            // BEGIN: QUOTA
            emitter.Add("Quota", site.Quota == null ? "" : site.Quota.QuotaID.ToString());
            if (site.Quota != null) {
                emitter.Add("InvitedUserMaximumLevel", site.Quota.InvitedUserMaximumLevel.ToString());
                emitter.Add("StorageMaximumLevel", site.Quota.StorageMaximumLevel.ToString());
                emitter.Add("StorageWarningLevel", site.Quota.StorageWarningLevel.ToString());
                emitter.Add("UserCodeMaximumLevel", site.Quota.UserCodeMaximumLevel.ToString());
                emitter.Add("UserCodeWarningLevel", site.Quota.UserCodeWarningLevel.ToString());
            }
            // END: QUOTA
            // IsReadLocked is not available in SharePoint 2010
            emitter.Add("ReadLocked", "False");
            emitter.Add("ReadOnly", site.ReadOnly.ToString());
            emitter.Add("ResourceQuotaExceeded", site.ResourceQuotaExceeded.ToString());
            emitter.Add("ResourceQuotaExceededNotificationSent", site.ResourceQuotaExceededNotificationSent.ToString());
            emitter.Add("ResourceQuotaWarningNotificationSent", site.ResourceQuotaWarningNotificationSent.ToString());
            emitter.Add("RootWebId", site.RootWeb.ID.ToString());
            emitter.Add("SecondaryContact", site.SecondaryContact == null ? "" : site.SecondaryContact.LoginName);
            emitter.Add("ServerRelativeUrl", Utility.Nullable(site.ServerRelativeUrl));
            emitter.Add("ShowURLStructure", site.ShowURLStructure.ToString());
            emitter.Add("SystemAccount", site.SystemAccount == null ? "" : site.SystemAccount.LoginName);
            emitter.Add("SyndicationEnabled", site.SyndicationEnabled.ToString());
            emitter.Add("TrimAuditLog", site.TrimAuditLog.ToString());
            emitter.Add("UIVersionConfigurationEnabled", site.UIVersionConfigurationEnabled.ToString());
            // BEGIN: USAGE
            emitter.Add("Bandwidth", site.Usage.Bandwidth.ToString());
            emitter.Add("DiscussionStorage", site.Usage.DiscussionStorage.ToString());
            emitter.Add("Hits", site.Usage.Hits.ToString());
            emitter.Add("Storage", site.Usage.Storage.ToString());
            emitter.Add("Visits", site.Usage.Visits.ToString());
            // END: USAGE
            // UserAccountDirectoryPath is obsolete in SharePoint 2010
            emitter.Add("UserAccountDirectoryPath", "");
            emitter.Add("UserCodeEnabled", site.UserCodeEnabled.ToString());
            emitter.Add("UserDefinedWorkflowsEnabled", site.UserDefinedWorkflowsEnabled.ToString());
            emitter.Add("WriteLocked", site.WriteLocked.ToString());
            emitter.Add("Zone", site.Zone.ToString());
            emitter.Add("WebApplicationId", webAppId.ToString());

            return emitter;
        }

        /// <summary>
        /// Convert an SPWeb object into a hash
        /// </summary>
        /// <param name="webAppId">The Web Application Id for this feature</param>
        /// <param name="siteId">The SPSite Id for this feature</param>
        /// <param name="web">The SPWeb object</param>
        /// <returns>A hash of the properties</returns>
        internal static Dictionary<string, string> ToHash(Guid webAppId, Guid siteId, SPWeb web)
        {
            Dictionary<string, string> emitter = new Dictionary<string, string>();

            emitter.Add("Id", web.ID.ToString());
            emitter.Add("Title", Utility.Nullable(web.Title));
            emitter.Add("AllowAnonymousAccess", web.AllowAnonymousAccess.ToString());
            emitter.Add("AllowAutomaticASPXPageIndexing", web.AllowAutomaticASPXPageIndexing.ToString());
            emitter.Add("AllowRssFeeds", web.AllowRssFeeds.ToString());
            emitter.Add("AllowUnsafeUpdates", web.AllowUnsafeUpdates.ToString());
            emitter.Add("AllWebTemplatesAllowed", web.AllWebTemplatesAllowed.ToString());
            emitter.Add("AlternateCssUrl", Utility.Nullable(web.AlternateCssUrl));
            emitter.Add("AlternateHeader", Utility.Nullable(web.AlternateHeader));
            emitter.Add("ASPXPageIndexed", web.ASPXPageIndexed.ToString());
            emitter.Add("ASPXPageIndexMode", web.ASPXPageIndexMode.ToString());
            emitter.Add("Audit", web.Audit == null ? "0" : ((int)web.Audit.AuditFlags).ToString("X"));
            // AuthenticationMode is always Windows in SharePoint 2010
            emitter.Add("AuthenticationMode", "Windows");
            emitter.Add("Author", web.Author == null ? "" : web.Author.LoginName);
            emitter.Add("ClientTag", web.ClientTag.ToString());
            emitter.Add("Configuration", web.Configuration.ToString());
            emitter.Add("Created", web.Created.ToUniversalTime().ToString("u"));
            emitter.Add("CurrencyLocaleID", web.CurrencyLocaleID.ToString());
            emitter.Add("CustomJavaScriptFileUrl", Utility.Nullable(web.CustomJavaScriptFileUrl));
            emitter.Add("CustomMasterUrl", Utility.Nullable(web.CustomMasterUrl));
            emitter.Add("CustomUploadPage", Utility.Nullable(web.CustomUploadPage));
            emitter.Add("EffectivePresenceEnabled", web.EffectivePresenceEnabled.ToString());
            // EmailInsertsEnabled is obsolete in SharePoint 2010
            emitter.Add("EmailInsertsEnabled", "False");
            // EventHandlersEnabled is obsolete in SharePoint 2010
            emitter.Add("EventHandlersEnabled", "False");
            emitter.Add("ExecuteUrl", Utility.Nullable(web.ExecuteUrl));
            emitter.Add("Exists", web.Exists.ToString());
            // HasExternalSecurityProvider is obsolete in SharePoint 2010
            emitter.Add("HasExternalSecurityProvider", "False");
            // HasUniquePerm is obsolete in SharePoint 2010
            emitter.Add("HasUniquePerm", "True");
            emitter.Add("HasUniqueRoleAssignments", web.HasUniqueRoleAssignments.ToString());
            emitter.Add("HasUniqueRoleDefinitions", web.HasUniqueRoleDefinitions.ToString());
            emitter.Add("IncludeSupportingFolders", web.IncludeSupportingFolders.ToString());
            emitter.Add("IsADAccountCreationMode", web.IsADAccountCreationMode.ToString());
            emitter.Add("IsADEmailEnabled", web.IsADEmailEnabled.ToString());
            emitter.Add("IsMultiLingual", web.IsMultilingual.ToString());
            emitter.Add("IsRootWeb", web.IsRootWeb.ToString());
            emitter.Add("Language", web.Language.ToString());
            emitter.Add("LastItemModifiedDate", web.LastItemModifiedDate.ToUniversalTime().ToString("u"));
            emitter.Add("Locale", web.Locale == null ? "" : web.Locale.DisplayName);
            emitter.Add("MasterPageReferenceEnabled", web.MasterPageReferenceEnabled.ToString());
            emitter.Add("MasterUrl", Utility.Nullable(web.MasterUrl));
            emitter.Add("NoCrawl", web.NoCrawl.ToString());
            emitter.Add("OverwriteTranslationsOnChange", web.OverwriteTranslationsOnChange.ToString());
            emitter.Add("ParentWebId", web.ParentWeb == null ? "" : web.ParentWeb.ID.ToString());
            emitter.Add("ParserEnabled", web.ParserEnabled.ToString());
            emitter.Add("PortalMember", web.PortalMember.ToString());
            emitter.Add("PortalName", Utility.Nullable(web.PortalName));
            emitter.Add("PortalSubscriptionUrl", Utility.Nullable(web.PortalSubscriptionUrl));
            emitter.Add("PortalUrl", Utility.Nullable(web.PortalUrl));
            emitter.Add("PresenceEnabled", web.PresenceEnabled.ToString());
            emitter.Add("Provisioned", web.Provisioned.ToString());
            // PublicFolderRootUrl is obsolete in SharePoint 2010
            emitter.Add("PublicFolderRootUrl", "");
            emitter.Add("QuickLaunchEnabled", web.QuickLaunchEnabled.ToString());
            emitter.Add("RecycleBinEnabled", web.RecycleBinEnabled.ToString());
            emitter.Add("RequestAccessEmail", Utility.Nullable(web.RequestAccessEmail));
            emitter.Add("RequestAccessEnabled", web.RequestAccessEnabled.ToString());
            emitter.Add("RootFolder", web.RootFolder == null ? "" : web.RootFolder.Name);
            emitter.Add("ServerRelativeUrl", Utility.Nullable(web.ServerRelativeUrl));
            emitter.Add("SiteLogoUrl", Utility.Nullable(web.SiteLogoUrl));
            emitter.Add("SyndicationEnabled", web.SyndicationEnabled.ToString());
            emitter.Add("Theme", Utility.Nullable(web.Theme));
            emitter.Add("ThemeCssUrl", Utility.Nullable(web.ThemeCssUrl));
            emitter.Add("ThemeCssFolderUrl", Utility.Nullable(web.ThemedCssFolderUrl));
            emitter.Add("TreeViewEnabled", web.TreeViewEnabled.ToString());
            emitter.Add("UIVersion", web.UIVersion.ToString());
            emitter.Add("UIVersionConfigurationEnabled", web.UIVersionConfigurationEnabled.ToString());
            emitter.Add("WebTemplate", Utility.Nullable(web.WebTemplate));
            emitter.Add("WebTemplateId", web.WebTemplateId.ToString());
            emitter.Add("WebApplicationId", webAppId.ToString());
            emitter.Add("SiteId", siteId.ToString());

            return emitter;
        }

        /// <summary>
        /// Convert an SPUser object into a hash
        /// </summary>
        /// <param name="webAppId">The Web Application Id for this feature</param>
        /// <param name="siteId">The Site Id</param>
        /// <param name="webId">The Web Id</param>
        /// <param name="user">The SPUser object</param>
        /// <returns>A hash of the properties</returns>
        internal static Dictionary<string, string> ToHash(Guid webAppId, Guid siteId, Guid webId, SPUser user)
        {
            Dictionary<string, string> emitter = new Dictionary<string, string>();

            emitter.Add("Id", user.ID.ToString());
            emitter.Add("LoginName", user.LoginName);
            emitter.Add("Email", Utility.Nullable(user.Email));
            emitter.Add("Sid", Utility.Nullable(user.Sid));
            emitter.Add("DisplayName", Utility.Nullable(user.Name));
            emitter.Add("RequireRequestToken", user.RequireRequestToken.ToString());
            emitter.Add("IsSiteAdmin", user.IsSiteAdmin.ToString());
            emitter.Add("IsSiteAuditor", user.IsSiteAuditor.ToString());
            emitter.Add("IsDomainGroup", user.IsDomainGroup.ToString());
            emitter.Add("IsApplicationPrincipal", user.IsApplicationPrincipal.ToString());
            emitter.Add("WebId", webId.ToString());
            emitter.Add("SiteId", siteId.ToString());
            emitter.Add("WebApplicationId", webAppId.ToString());

            return emitter;
        }

        /// <summary>
        /// Convert an SPList object into a hash
        /// </summary>
        /// <param name="webAppId">The Web Application Id for this feature</param>
        /// <param name="siteId">The Site Id</param>
        /// <param name="web">The SPWeb Object</param>
        /// <param name="list">The SPList object</param>
        /// <returns>A hash of the properties</returns>
        internal static Dictionary<string, string> ToHash(Guid webAppId, Guid siteId, SPWeb web, SPList list)
        {
            Dictionary<string, string> emitter = new Dictionary<string, string>();

            emitter.Add("Id", list.ID.ToString());
            emitter.Add("Title", Utility.Nullable(list.Title));
            emitter.Add("ItemCount", list.ItemCount.ToString());
            emitter.Add("Hidden", list.Hidden.ToString());
            emitter.Add("EmailAlias", Utility.Nullable(list.EmailAlias));
            emitter.Add("Audit", list.Audit == null ? "0" : ((int)list.Audit.AuditFlags).ToString("X"));
            emitter.Add("Author", list.Author == null ? "" : list.Author.LoginName);
            emitter.Add("Created", list.Created.ToUniversalTime().ToString("u"));
            emitter.Add("EnableAttachments", list.EnableAttachments.ToString());
            // EnableDeployingList is deprecated in SharePoint 2010
            emitter.Add("EnableDeployingList", "False");
            emitter.Add("EnableDeployWithDependentList", list.EnableDeployWithDependentList.ToString());
            emitter.Add("EnableFolderCreation", list.EnableFolderCreation.ToString());
            emitter.Add("EnableMinorVersions", list.EnableMinorVersions.ToString());
            emitter.Add("EnableModeration", list.EnableModeration.ToString());
            emitter.Add("EnablePeopleSelector", list.EnablePeopleSelector.ToString());
            emitter.Add("EnableResourceSelector", list.EnableResourceSelector.ToString());
            emitter.Add("EnableSchemaCaching", list.EnableSchemaCaching.ToString());
            emitter.Add("EnableSyndication", list.EnableSyndication.ToString());
            emitter.Add("EnableThrottling", list.EnableThrottling.ToString());
            emitter.Add("EnableVersioning", list.EnableVersioning.ToString());
            emitter.Add("EnforceDataValidation", list.EnforceDataValidation.ToString());
            emitter.Add("ExcludeFromOfflineClient", list.ExcludeFromOfflineClient.ToString());
            emitter.Add("ExcludeFromTemplate", list.ExcludeFromTemplate.ToString());
            emitter.Add("ForceCheckout", list.ForceCheckout.ToString());
            emitter.Add("HasExternalDataSource", list.HasExternalDataSource.ToString());
            emitter.Add("ImageUrl", Utility.Nullable(list.ImageUrl));
            emitter.Add("IrmEnabled", list.IrmEnabled.ToString());
            emitter.Add("IrmExpire", list.IrmExpire.ToString());
            emitter.Add("IrmReject", list.IrmReject.ToString());
            emitter.Add("IsApplicationList", list.IsApplicationList.ToString());
            emitter.Add("IsSiteAssetsLibrary", list.IsSiteAssetsLibrary.ToString());
            emitter.Add("IsThrottled", list.IsThrottled.ToString());
            emitter.Add("LastItemDeletedDate", list.LastItemDeletedDate == null ? "Never" : list.LastItemDeletedDate.ToUniversalTime().ToString("u"));
            emitter.Add("LastItemModifiedDate", list.LastItemModifiedDate == null ? "Never" : list.LastItemModifiedDate.ToUniversalTime().ToString("u"));
            emitter.Add("NoCrawl", list.NoCrawl.ToString());
            emitter.Add("OnQuickLaunch", list.OnQuickLaunch.ToString());
            emitter.Add("Ordered", list.Ordered.ToString());
            emitter.Add("ParentWebId", list.ParentWeb.ID.ToString());
            emitter.Add("ReadSecurity", list.ReadSecurity.ToString());
            emitter.Add("RequestAccessEnabled", list.RequestAccessEnabled.ToString());
            emitter.Add("RootWebOnly", list.RootWebOnly.ToString());
            emitter.Add("SendToLocationName", Utility.Nullable(list.SendToLocationName));
            emitter.Add("SendToLocationUrl", Utility.Nullable(list.SendToLocationUrl));
            emitter.Add("ShowUser", list.ShowUser.ToString());
            emitter.Add("WriteSecurity", list.WriteSecurity.ToString());

            // Compute the size of the list on disk.  This takes into account the
            // versioning of the files and the old versions that are kept around
            Int64 size = 0;
            foreach (SPListItem item in list.Items)
            {
                // There are two types of lists - those backed by file objects and those backed by XML objects
                if (item.File != null)
                {
                    SPFile tFile = web.GetFile(web.Url + "/" + item.File.Url);
                    foreach (SPFileVersion tVersion in tFile.Versions)
                    {
                        size += tVersion.Size;
                    }
                }
                else if (item.Xml != null)
                {
                    size += item.Xml.Length;
                }
                else
                {
                    SystemLogger.Write(LogLevel.Warn, string.Format("Cannot find size of Item ID: {0} in web {1}", item.ID.ToString(), web.ID.ToString()));
                }
            }

            emitter.Add("ItemSize", size.ToString());
            emitter.Add("WebId", web.ID.ToString());
            emitter.Add("SiteId", siteId.ToString());
            emitter.Add("WebApplicationId", webAppId.ToString());

            return emitter;
        }

    }
}
