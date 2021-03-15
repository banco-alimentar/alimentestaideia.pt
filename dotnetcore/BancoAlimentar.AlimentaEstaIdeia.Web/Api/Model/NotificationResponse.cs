namespace BancoAlimentar.AlimentaEstaIdeia.Web.Api.Model
{
    /// <summary>
    /// Represent the response object of the EasyPay notification system.
    /// </summary>
    public class NotificationResponse
    {
        /// <summary>
        /// Gets or sets the status message.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        public string[] Message { get; set; }
    }
}
