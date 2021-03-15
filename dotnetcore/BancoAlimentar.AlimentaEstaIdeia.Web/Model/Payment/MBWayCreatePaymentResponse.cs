namespace BancoAlimentar.AlimentaEstaIdeia.Web.Model.Payment
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class MBWayCreatePaymentResponse
    {
        public string status { get; set; }
        public string[] message { get; set; }
        public string id { get; set; }
        public Method method { get; set; }
        public Customer customer { get; set; }
    }

    public class Method
    {
        public string type { get; set; }
        public string status { get; set; }
        public string alias { get; set; }
    }
}
