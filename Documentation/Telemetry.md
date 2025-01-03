#List of Telemetry Methods that are called

## TrackMetric

| File      | Line | Event      | Normal | Reason |
| ------------- | ------------- | ------------- | ------------- | ------------- |
| Api\EasyPayControllerBase.cs |73| DonationNotFound | No  | Donation was not found|
| Api\EasyPayControllerBase.cs| 81  | SendInvoiceEmail | Yes  |  |
| Api\EasyPayControllerBase.cs| 93| EmailAlreadySent | No  |  |
| Api\EasyPayControllerBase.cs| 104 | DonationWrongStatus | No  |  |
| Api\EasyPayControllerBase.cs| 119 | DonationNotFound | No  |  |
| Api\EasyPayControllerBase.cs|  129|  EmailIsNotEanbled | No  |  |
| CheckPaymentStatusInvoice.cshtml.cs |  64|  CheckStatus-DonationNotFound | No  |  |
|DownloadPersonalData.cshtml.cs||DownloadPersonalData |||
|GenerateInvoice.cshtml.cs||GenerateInvoiceErrorMessage |||
|Delete.cshtml.cs||  WhenDeletingSubscripionUserIsNotValidGet|||
|Delete.cshtml.cs||  SubscriptionNotDeleted|||
|Delete.cshtml.cs||  WhenDeletingSubscripionUserIsNotValid|||
|||  |||
|||  |||
|||  |||
|||  |||
|||  |||
|||  |||
|||  |||
|||  |||
|||  |||
|||  |||
|||  |||
|||  |||
|||  |||
|||  |||
|||  |||
|||  |||
|||  |||


### Kusto:
#### Events that are not normal
`customEvents
| where name != "EmailSent" and name != "AppInsightsSnapshotCollectorLogs" and name != "FindInvoiceByPublicId"  and name != "CreateSinglePayment" and name != "SendInvoiceEmail" and name != "SendInvoiceEmailWantsReceipt" and name != "ThanksOnGetSuccess" and name != "SendInvoiceEmailNoReceipt"
| summarize failedCount=sum(itemCount), impactedUsers=dcount(user_Id) by name
| order by failedCount desc`

#### Summarize some events
`customEvents
| where name == "DonationNotFound" or name == "CheckStatus-DonationNotFound " or name == "EmailAlreadySent" or name == "DonationWrongStatus" or name == "EmailIsNotEanbled" or name == "CheckStatus-DonationNotFound " or name == "DownloadPersonalData" or name == "GenerateInvoiceErrorMessage" or name == "WhenDeletingSubscripionUserIsNotValidGet"
| summarize failedCount=sum(itemCount), impactedUsers=dcount(user_Id) by name
| order by failedCount desc
`
#### All customEvents
`customEvents
| summarize failedCount=sum(itemCount), impactedUsers=dcount(user_Id) by name
| order by failedCount desc
`
## TrackEvent

## TrackException
- Search "TrackException" (31 hits in 17 files of 2147 searched) [Normal]
- 

## TrackDependency
none

## TrackAvailability
none

## TrackTrace
  source\repos\alimentestaideia.pt\BancoAlimentar.AlimentaEstaIdeia.Web\Areas\Identity\Pages\Account\Manage\Subscriptions\Delete.cshtml.cs (1 hit)
	Line 135:                         this.telemetryClient.TrackTrace(response.RawContent);
 
## TrackRequest
none

## TrackPageView
  
  - `source\repos\alimentestaideia.pt\BancoAlimentar.AlimentaEstaIdeia.Web\Pages\Shared\_Layout.cshtml (2 hits)
	Line  28: 			!function(T,l,y){var S=T.l...
	Line 225: 			_gaq.push(['_trackPageview']);`
  
  - `source\repos\alimentestaideia.pt\BancoAlimentar.AlimentaEstaIdeia.Web\Pages\Tenants\alimentestaideia\Pages\_Layout.cshtml (2 hits)
	Line  28: 			!function(T,l,y){var S=T.locatio...
	Line 225: 			_gaq.push(['_trackPageview']);`
  
  - `source\repos\alimentestaideia.pt\BancoAlimentar.AlimentaEstaIdeia.Web\Pages\Tenants\BancoAlimentar\Pages\_Layout.cshtml (2 hits)
	Line  34:             !function(T,l,y){var S=T.l...
	Line 319:             _gaq.push(['_trackPageview']);`

