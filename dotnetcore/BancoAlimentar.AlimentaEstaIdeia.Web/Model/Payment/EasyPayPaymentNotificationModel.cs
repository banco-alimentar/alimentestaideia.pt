namespace BancoAlimentar.AlimentaEstaIdeia.Web.Model.Payment
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class EasyPayPaymentNotificationModel
    {
        public string id { get; set; }

        public string key { get; set; }

        public string type { get; set; }

        public string status { get; set; }

        public string[] messages { get; set; }

        public string date { get; set; }
    }
}
