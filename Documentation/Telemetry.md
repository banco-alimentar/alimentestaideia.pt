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
| Areas\Identity\Pages\Account\Manage\CheckPaymentStatusInvoice.cshtml.cs |  64|  CheckStatus-DonationNotFound | No  |  |
||||||

Kusto:
`customEvents 
| where name == "DonationNotFound" or name == "CheckStatus-DonationNotFound " or name == "EmailAlreadySent" or name == "DonationWrongStatus" or name == "EmailIsNotEanbled"
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

