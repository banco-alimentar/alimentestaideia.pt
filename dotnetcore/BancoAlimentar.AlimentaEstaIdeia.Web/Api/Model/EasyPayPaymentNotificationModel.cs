namespace BancoAlimentar.AlimentaEstaIdeia.Web.Api.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class EasyPayPaymentNotificationModel
    {
        public string id { get; set; }

        public string value { get; set; }

        public string currency { get; set; }

        public string key { get; set; }

        public string expiration_time { get; set; }

        public string method { get; set; }

        public Customer customer { get; set; }

        public Account account { get; set; }

        public Transaction transaction { get; set; }
    }

    public class Customer
    {
        public string id { get; set; }

        public string name { get; set; }

        public string email { get; set; }

        public string phone { get; set; }

        public string phone_indicative { get; set; }

        public string fiscal_number { get; set; }

        public string key { get; set; }
    }

    public class Account
    {
        public string id { get; set; }
    }

    public class Transaction
    {
        public string id { get; set; }

        public string key { get; set; }

        public string type { get; set; }

        public string date { get; set; }

        public string transfer_date { get; set; }

        public string document_number { get; set; }

        public Values values { get; set; }
    }

    public class Values
    {
        public float requested { get; set; }

        public float paid { get; set; }

        public float fixed_fee { get; set; }

        public float variable_fee { get; set; }

        public float tax { get; set; }

        public float transfer { get; set; }
    }

}
