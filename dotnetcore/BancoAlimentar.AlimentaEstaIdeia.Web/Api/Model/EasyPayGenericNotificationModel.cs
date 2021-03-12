namespace BancoAlimentar.AlimentaEstaIdeia.Web.Api.Model
{
    public class EasyPayGenericNotificationModel
    {
        public string id { get; set; }

        public string key { get; set; }

        public string type { get; set; }

        public string status { get; set; }

        public string[] messages { get; set; }

        public string date { get; set; }
    }
}
