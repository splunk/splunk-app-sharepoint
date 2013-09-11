function Count-Object
{
	BEGIN {
		$count = 0
	}
	PROCESS {
		$count += 1
	}
	END {
		$count
	}
}

function Load-PsSnapIn
{
	[CmdletBinding()]
	param(
		[Parameter(Mandatory=$true)]
		$SnapIn
	)

	PROCESS {
		$Count = Get-PsSnapIn | Where-Object { $_.Name -eq $SnapIn } | Count-Object
		if ($Count -eq 0) {
			Write-Verbose "Loading SnapIn $SnapIn"
			Add-PsSnapIn $SnapIn
		}
	}
}

function Out-Splunk
{
	[CmdletBinding()]
	param(
		[Parameter(Mandatory=$True,ValueFromPipeline=$True,ValueFromPipelineByPropertyName=$True)]
		[PSObject[]] $Objects
	)

	PROCESS {
		foreach ($object in $Objects)
		{
			$arr = New-Object System.Collections.ArrayList
			
			[void]$arr.Add("$(Get-Date -format 'yyyy-MM-ddTHH:mm:sszzz')")
			[void]$arr.Add("Type=`"$($object.TypeName)`"")

			foreach ($p in $object.PSObject.Properties)
			{
				if ($p.Name -eq "TypeName") {
					continue
				}
				
				switch ($p.TypeNameOfValue.ToString())
				{
				"Microsoft.SharePoint.Administration.SPApplicationPool" {
					[void]$arr.Add("$($p.Name)=`"$($p.Value.Id)`"")
					break
				}
				"Microsoft.SharePoint.SPAudit" {
					if ($p.Value.AuditFlags -ne $null) {
						[void]$arr.Add("AuditFlags=`"$($p.Value.AuditFlags.value__.ToString("X"))`"")
					} else {
						[void]$arr.Add("AuditFlags=`"0`"")
					}
					[void]$arr.Add("UseAuditFlagCache=`"$($p.Value.UseAuditFlagCache)`"")
					if ($p.Value.EffectiveAuditMask -ne $null) {
						[void]$arr.Add("EffectiveAuditMask=`"$($p.Value.EffectiveAuditMask.value__.ToString("X"))`"")
					} else {
						[void]$arr.Add("EffectiveAuditMask=`"0`"")
					}
					break
				}
				"Microsoft.SharePoint.Administration.SPContentDatabase" {
					[void]$arr.Add("$($p.Name)Id=`"$($p.Value.Id)`"")
					break
				}
				"Microsoft.SharePoint.Administration.SPFarm" {
					[void]$arr.Add("$($p.Name)Id=`"$($p.Value.Id)`"")
					break
				}
				"Microsoft.SharePoint.Administration.SPQuota" {
					[void]$arr.Add("QuotaID=`"$($p.Value.QuotaID)`"")
					[void]$arr.Add("InvitedUserMaximumLevel=`"$($p.Value.InvitedUserMaximumLevel)`"")
					[void]$arr.Add("StorageMaximumLevel=`"$($p.Value.StorageMaximumLevel)`"")
					[void]$arr.Add("StorageWarningLevel=`"$($p.Value.StorageWarningLevel)`"")
					[void]$arr.Add("UserCodeMaximumLevel=`"$($p.Value.UserCodeMaximumLevel)`"")
					[void]$arr.Add("UserCodeWarningLevel=`"$($p.Value.UserCodeWarningLevel)`"")
					break
				}
				"Microsoft.SharePoint.Administration.SPServer" {
					[void]$arr.Add("$($p.Name)=`"$($p.Value.Name)`"")
					break
				}
				"Microsoft.SharePoint.SPSite" {
					[void]$arr.Add("$($p.Name)Id=`"$($p.Value.Id)`"")
					break
				}
				"Microsoft.SharePoint.SPSite+UsageInfo" {
					[void]$arr.Add("Bandwidth=`"$($p.Value.Bandwidth)`"")
					[void]$arr.Add("DiscussionStorage=`"$($p.Value.DiscussionStorage)`"")
					[void]$arr.Add("Hits=`"$($p.Value.Hits)`"")
					[void]$arr.Add("Storage=`"$($p.Value.Storage)`"")
					[void]$arr.Add("Visits=`"$($p.Value.Visits)`"")
					break
				}
				"Microsoft.SharePoint.SPWeb" {
					[void]$arr.Add("$($p.Name)Id=`"$($p.Value.Id)`"")
					break
				}
				"Microsoft.SharePoint.Administration.SPWebApplication" {
					[void]$arr.Add("$($p.Name)Id=`"$($p.Value.Id)`"")
					break
				}
				default {
					if ($p.Name -eq "ID") {
						[void]$arr.Add("Id=`"$($p.Value)`"")
					} else {
						[void]$arr.Add("$($p.Name)=`"$($p.Value)`"")
					}
				}
				}
			}

			Write-Host ($arr -join " ")
		}
	}
}

#
#################################################################################################
#
# Start of Main Program
#
Load-PsSnapIn Microsoft.SharePoint.PowerShell
$farmId = (Get-SPFarm).Id

#
# SPFarm
#
Get-SPFarm `
| Select-Object TypeName, Id, Name, DisplayName, Status, Version, BuildVersion, `
		DiskSizeRequired, PersistedFileChunkSize, CEIPEnabled, DefaultServiceAccount, 
		DownloadErrorReportingUpdates, ErrorReportingAutomaticUpload, ErrorReportingEnabled, `
		IsBackwardsCompatible, PasswordChangeEmailAddress, PassswordChangeGuardTime, `
		PasswordChangeMaximumTries, DaysBeforePasswordExpirationToSendEmail `
| Out-Splunk

#
# SPAlternateUrl
#
(Get-SPFarm).AlternateUrlCollections `
| Select-Object Uri, Zone, IncomingUrl, PublicUrl `
| Add-Member -PassThru -MemberType NoteProperty -Name TypeName -Value "AlternateUrl" `
| Add-Member -PassThru -MemberType NoteProperty -Name FarmId -Value $farmId `
| Out-Splunk

#
# SPServer
#
Get-SPServer `
| Select-Object TypeName, Id, Name, DisplayName, Status, Version, Role `
| Add-Member -PassThru -MemberType NoteProperty -Name FarmId -Value $farmId `
| Out-Splunk

#
# SPServiceInstance
#
Get-SPServiceInstance `
| Select-Object Id, DisplayName, TypeName, Status, Version, Hidden, Instance, Roles, Server, Service, SystemService `
| Foreach-Object { $_ | Add-Member -PassThru -MemberType NoteProperty -Name Name -Value $_.TypeName } `
| Select-Object Id, DisplayName, Name, Status, Version, Hidden, Instance, Roles, Server, Service, SystemService `
| Add-Member -PassThru -MemberType NoteProperty -Name TypeName -Value "ServiceInstance" `
| Add-Member -PassThru -MemberType NoteProperty -Name FarmId -Value $farmId `
| Out-Splunk

# 
# SPDiagnosticProvider
#
Get-SPDiagnosticsProvider `
| Select-Object Id, Name, DisplayName, Title, TableName, LastRunTime, MaxTotalSizeInBytes, `
		RetentionPeriod, LockType, Schedule, Retry, IsDisabled, VerboseTracingEnabled, `
		EnableBackup, DiskSizeRequired, Status, Version, Retention, Enabled `
| Add-Member -PassThru -MemberType NoteProperty -Name TypeName -Value "DiagnosticsProvider" `
| Add-Member -PassThru -MemberType NoteProperty -Name FarmId -Value $farmId `
| Out-Splunk

#
# SPFeatureDefinition
#
Get-SPFeature -Limit All `
| Select-Object Id, Name, DisplayName, ActivateOnDefault, AlwaysForceInstall, `
		AutoActivateInCentralAdmin, Hidden, ReceiverAssembly, ReceiverClass, RequireResources, `
		RootDirectory, Scope, SolutionId, Status, UIVersion, Version `
| Add-Member -PassThru -MemberType NoteProperty -Name TypeName -Value "FeatureDefinition" `
| Add-Member -PassThru -MemberType NoteProperty -Name FarmId -Value $farmId `
| Out-Splunk

#
# SPWebTemplate
#
Get-SPWebTemplate `
| Select-Object Id, Name, Title, IsCustomTemplate, IsHidden, IsRootWebOnly, IsSubWebOnly, IsUnique, `
		ProvisionAssembly, ProvisionClass, ProvisionData, LcId, SupportsMultiLingualUI `
| Add-Member -PassThru -MemberType NoteProperty -Name TypeName -Value "WebTemplate" `
| Add-Member -PassThru -MemberType NoteProperty -Name FarmId -Value $farmId `
| Out-Splunk

################################################################################################################
#
# Farms contain Web Applications
#
################################################################################################################
#
# Web Applications
# 
Get-SPWebApplication -IncludeCentralAdministration `
| Select-Object Id, Name, DisplayName, Status, Version, Url, `
		AlertFlags, AlertsEnabled, AlertsEventBatchSize, AlertsLimited, AlertsMaximum, `
		AlertsMaximumQuerySet, AllowAccessToWebpartCatalog, AllowContributorsToEditScriptableParts, `
		AllowDesigner, AllowInlineDownloadedMimeTypes, AllowHighCharacterListFolderNames, `
		AllowMasterPageEditing, AllowOMCodeOverrideThrottleSettings, AllowPartToPartCommunication, `
		AllowRevertFromTemplate, AllowSilverlightPrompt, AlwaysProcessDocuments, ApplicationPool, `
		AutomaticallyDeleteUnusedSiteCollections, BrowserCEIPEnabled, CascadeDeleteMaximumItemLimit, `
		CascadeDeleteTimeoutMultiplier, ChangeLogExpirationEnabled, ChangeLogRetentionPeriod, `
		DaysToShowNewIndicator, DefaultQuotaTemplate, DefaultServerComment, DefaultTimeZone, `
		DisableCoathoring, DiskSizeRequired, DocumentConversionsEnabled, `
		EmailToNoPermissionWorkflowParticipantsEnabled, EventHandlersEnabled, EventLogRetentionPeriod, `
		ExternalUrlZone, ExternalWorkflowParticipantsEnabled, FileNotFoundPage, `
		IncomingEmailServerAddress, IsAdministrationWebApplication, `
		MaxDiscussionBoardItemsForSiteDataFolderQuery, MaximumFileSize, MaxItemsPerThrottledOperation, `
		MaxItemsPerThrottledOperationOverride, MaxItemsPerThrottledOperationWarningLevel, `
		MaxListItemRowStorage, MaxQueryLookupFields, MaxSizePerCellStorageOperations, `
		MaxUniquePermScopesPerList, OfficialFileUrl, OutboundMailCodepage, OutboundMailReplyToAddress, `
		OutboundMailSenderAddress, OutboundMailServiceInstance, OutboundMmsServiceAccount, `
		OutboundSmsServiceAccount, PresenceEnabled, PublicFolderRootUrl, RecycleBinEnabled, `
		RecycleBinCleanupEnabled, RecycleBinRetentionPeriod, RenderingFromMetaInfoEnabled, `
		RequireContactForSelfServiceSiteCreation, ScopeExternalConnectionsToSiteSubscriptions, `
		SecondStageRecycleBinQuota, SelfServiceSiteCreationEnabled, SendLoginCredentialsByEmail, `
		SendUnusedSiteCollectionNotifications, ShowURLStructure, SyndicationEnabled, `
		UnusedSiteNotificationPeriod, UnusedSiteNotificationsBeforeDeletion, UseClaimsAuthentication, `
		UserDefinedWorkflowMaximumComplexity, UserDefinedWorkflowsEnabled `
| Add-Member -PassThru -MemberType NoteProperty -Name TypeName -Value "WebApplication" `
| Add-Member -PassThru -MemberType NoteProperty -Name FarmId -Value $farmId `
| Out-Splunk

foreach ($webapp in Get-SPWebApplication -IncludeCentralAdministration)
{
	$webapp.ApplicationPool `
	| Select-Object Id, Name, DisplayName, Status, Version, Username, `
			CurrentIdentityType, CurrentSecurityIdentifier, `
			IsCredentialUpdateEnabled, IsCredentialDeploymentEnabled `
	| Add-Member -PassThru -MemberType NoteProperty -Name TypeName -Value "ApplicationPool" `
	| Add-Member -PassThru -MemberType NoteProperty -Name FarmId -Value $farmId `
	| Add-Member -PassThru -MemberType NoteProperty -Name WebApplicationId -Value $webapp.Id `
	| Out-Splunk

	$webapp.Features `
	| Select-Object DefinitionId, Version, FeatureDefinitionScope `
	| Foreach-Object { $_ | Add-Member -PassThru -MemberType NoteProperty -Name Id -Value $_.DefinitionId } `
	| Select-Object Id, Version, FeatureDefinitionScope `
	| Add-Member -PassThru -MemberType NoteProperty -Name TypeName -Value "Feature" `
	| Add-Member -PassThru -MemberType NoteProperty -Name FarmId -Value $farmId `
	| Add-Member -PassThru -MemberType NoteProperty -Name WebApplicationId -Value $webapp.Id `
	| Out-Splunk

	foreach ($policy in $webapp.Policies) {
		$p_arr = New-Object System.Collections.ArrayList
		$policy.PolicyRoleBindings | %{ [void]$p_arr.Add($_.Name) }
		$PolicyRole = $p_arr -join ","

		$policy | Select-Object DisplayName, IsSystemUser, UserName `
		| Add-Member -PassThru -MemberType NoteProperty -Name PolicyRoleBinding -Value $PolicyRole `
		| Add-Member -PassThru -MemberType NoteProperty -Name TypeName -Value "Policy" `
		| Add-Member -PassThru -MemberType NoteProperty -Name FarmId -Value $farmId `
		| Add-Member -PassThru -MemberType NoteProperty -Name WebApplicationId -Value $webapp.Id `
		| Out-Splunk
	}

	foreach ($prefix in $webapp.Prefixes) {
		$uri = $webapp.Url + $prefix.Name
		$prefix | Select-Object Name, PrefixType `
		| Add-Member -PassThru -MemberType NoteProperty -Name Url -Value $uri `
		| Add-Member -PassThru -MemberType NoteProperty -Name TypeName -Value "Policy" `
		| Add-Member -PassThru -MemberType NoteProperty -Name FarmId -Value $farmId `
		| Add-Member -PassThru -MemberType NoteProperty -Name WebApplicationId -Value $webapp.Id `
		| Out-Splunk
	}	
}

Get-SPContentDatabase `
| Select-Object Id, Name, DisplayName, Status, Version, CurrentSiteCount, MaximumSiteCount, WarningSiteCount, `
		DatabaseConnectionString, DiskSizeRequired, Exists, ExistsInFarm, FailoverServer, `
		FailoverServiceInstance, IncludeInVssBackup, IsAttachedToFarm, IsReadOnly, NormalizedDataSource, `
		Server, WebApplication, Farm `
| Add-Member -PassThru -MemberType NoteProperty -Name TypeName -Value "ContentDatabase" `
| Out-Splunk

#
##############################################################################################################
#
# Within Each SPWebApplication is a number of sites. 
# Within Each SPSite is a number of SPWebs.
# Within Each SPWeb is a bunch of lists, folders, users, etc.
#
foreach ($site in Get-SPSite -Limit All)
{
	$o_arr = New-Object System.Collections.ArrayList
	$site.Owner | %{ [void]$o_arr.Add($_.UserLogin) }
	$Owner = $o_arr -join ","

	$o_arr = New-Object System.Collections.ArrayList
	$site.SecondaryContact | %{ [void]$o_arr.Add($_.UserLogin) }
	$SecondaryContact = $o_arr -join ","

	$myID = "{0}\{1}" -f $env:USERDOMAIN, $env:USERNAME

			
	$site `
	| Select-Object Id, Url, AdministrationSiteType, AllowDesigner, AllowMasterPageEditing, `
			AllowRevertFromTemplate, AllowRssFeeds, AllowUnsafeUpdates, Audit, `
			AuditLogTrimmingCallout, AuditLogTrimmingRetention, AverageResourceUsage, `
			BrowserDocumentsEnabled, CatchAccessDeniedException, CertificationDate, `
			ContentDatabase, CurrentResourceUsage, DeadWebNotificationCount, `
			HostHeaderIsSiteName, HostName, IISAllowsAnonymous, Impersonating, `
			LastContentModifiedDate, LastSecurityModifiedDate, LockIssue, Port, PortalName, `
			PortalUrl, Protocol, Quota, ReadLocked, ReadOnly, ResourceQuotaExceeded, `
			ResourceQuotaExceededNotificationSent, ResourceQuotaWarningNotificationSent, `
			RootWeb, ServerRelativeUrl, ShowURLStructure, SystemAccount, SyndicationEnabled, `
			TrimAuditLog, UIVersionConfigurationEnabled, Usage, UserAccountDirectoryPath, `
			UserCodeEnabled, UserDefinedWorkflowsEnabled, WebApplication, WriteLocked, Zone `
	| Add-Member -PassThru -MemberType NoteProperty -Name TypeName -Value "Site" `
	| Add-Member -PassThru -MemberType NoteProperty -Name Owner -Value $Owner `
	| Add-Member -PassThru -MemberType NoteProperty -Name SecondaryContact -Value $SecondaryContact `
	| Add-Member -PassThru -MemberType NoteProperty -Name FarmId -Value $farmId `
	| Out-Splunk

	try {
		foreach ($spweb in Get-SPWeb -Site $site -Limit All)
		{
			$spweb `
			| Select-Object Id, Title, Site, AllowAnonymousAccess, AllowAutomaticASPXPageIndexing, `
					AllowRssFeeds, AllowUnsafeUpdates, AllWebTemplatesAllowed, AlternateCssUrl, `
					AlternateHeader, ASPXPageIndexer, ASPXPageIndexMode, Audit, AuthenticationMode, `
					Author, ClientTag, Configuration, Created, CurrencyLocaleID, CustomJavaScriptFileUrl, `
					CustomMasterUrl, CustomUploadPage, EffectivePresenceEnabled, EmailInsertsEnabled, `
					EventHandlersEnabled, ExecuteUrl, Exists, HasExternalSecurityProvider, `
					HasUniquePerm, HasUniqueRoleAssignments, HasUniqueRoleDefinitions, `
					IncludeSupportingFolders, IsADAccountCreationMode, IsADEmailEnabled, `
					IsMultiLingual, IsRootWeb, Language, LastItemModifiedDate, Locale, `
					MasterPageReferenceEnabled, MasterUrl, NoCrawl, OverwriteTranslationsOnChange, `
					ParentWeb, ParserEnabled, PortalMember, PortalName, PortalSubscriptionUrl, `
					PortalUrl, PresenceEnabled, Provisioned, PublicFolderRootUrl, QuickLaunchEnabled, `
					RecycleBinEnabled, RequestAccessEmail, RequestAccessEnabled, RootFolder, `
					ServerRelativeUrl, SiteLogoUrl, SyndicationEnabled, Theme, ThemeCssUrl, `
					ThemeCssFolderUrl, TreeViewEnabled, UIVersion, UIVersionConfigurationEnabled, `
					WebTemplate, WebTemplateId `
			| Add-Member -PassThru -MemberType NoteProperty -Name TypeName -Value "Web" `
			| Add-Member -PassThru -MemberType NoteProperty -Name FarmId -Value $farmId `
			| Out-Splunk

			$spweb.AllUsers `
			| Select-Object Id, LoginName, Email, Sid, DisplayName, RequireRequestToken, `
					IsSiteAdmin, IsSiteAuditor, IsDomainGroup, IsApplicationPrincipal `
			| Add-Member -PassThru -MemberType NoteProperty -Name WebId -Value $spweb.Id `
			| Add-Member -PassThru -MemberType NoteProperty -Name TypeName -Value "User" `
			| Add-Member -PassThru -MemberType NoteProperty -Name FarmId -Value $farmId `
			| Out-Splunk

			
			foreach ($list in $spweb.Lists)
			{
				$size = 0
				$list.Items.GetDataTable() | %{ $size += $_.FileSizeDisplay }

				$list | Select-Object Id, Title, ItemCount, Hidden, EmailAlias, Audit, Author, Created, `
					EmailInsertsFolder, EnableAssignToEmail, EnableAttachments, EnableDeployingList, `
					EnableDeployWithDependentList, EnableFolderCreation, EnableMinorVersions, `
					EnableModeration, EnablePeopleSelector, EnableResourceSelector, `
					EnableSchemaCaching, EnableSyndication, EnableThrottling, EnableVersioning, `
					EnforceDataValidation, ExcludeFromOfflineClient, ExcludeFromTemplate, `
					ForceCheckout, HasExternalDataSource, ImageUrl, IrmEnabled, IrmExpire, IrmReject, `
					IsApplicationList, IsCatalog, IsSiteAssetsLibrary, IsThrottled, `
					LastItemDeletedDate, LastItemModifiedDate, NoCrawl, OnQuickLaunch, Ordered, `
					ParentWeb, ReadSecurity, RequestAccessEnabled, RootWebOnly, SendToLocationName, `
					SendToLocationUrl, ShowUser, Version, WriteSecurity `
				| Add-Member -PassThru -MemberType NoteProperty -Name ItemSize -Value $size `
				| Add-Member -PassThru -MemberType NoteProperty -Name WebId -Value $spweb.Id `
				| Add-Member -PassThru -MemberType NoteProperty -Name TypeName -Value "List" `
				| Add-Member -PAssThru -MemberType NoteProperty -Name FarmId -Value $farmId `
				| Out-Splunk
			}
		}
	} catch [Exception] {
		New-Object PSObject `
		| Add-Member -PassThru -MemberType NoteProperty -Name TypeName -Value "Error" `
		| Add-Member -PassThru -MemberType NoteProperty -Name FarmId -Value $farmId `
		| Add-Member -PassThru -MemberType NoteProperty -Name Exception -Value $_.Exception.GetType() `
		| Add-Member -PassThru -MemberType NoteProperty -Name Message -Value "Error processing $($site.Url) with ${myID}: $($_.Exception.Message)" `
		| Out-Splunk
	}
}
