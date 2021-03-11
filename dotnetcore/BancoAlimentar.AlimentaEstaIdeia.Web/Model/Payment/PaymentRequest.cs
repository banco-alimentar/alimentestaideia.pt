namespace BancoAlimentar.AlimentaEstaIdeia.Web.Model.Payment
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class PaymentRequest
    {
        public string type { get; set; }

        public string expiration_time { get; set; }

        public string currency { get; set; }

        public Customer customer { get; set; }

        public string key { get; set; }

        public float value { get; set; }

        public string method { get; set; }

        public Sdd_Mandate sdd_mandate { get; set; }

        public Capture capture { get; set; }
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

        public string language { get; set; }
    }

    public class Sdd_Mandate
    {
        public string id { get; set; }

        public string iban { get; set; }

        public string key { get; set; }

        public string name { get; set; }

        public string email { get; set; }

        public string phone { get; set; }

        public string account_holder { get; set; }

        public string country_code { get; set; }

        public string max_num_debits { get; set; }
    }

    public class Capture
    {
        public string transaction_key { get; set; }

        public string capture_date { get; set; }

        public Account account { get; set; }

        public Split[] splits { get; set; }

        public string descriptive { get; set; }
    }

    public class Account
    {
        public string id { get; set; }
    }

    public class Split
    {
        public string split_key { get; set; }

        public string split_descriptive { get; set; }

        public int value { get; set; }

        public Account1 account { get; set; }

        public float margin_value { get; set; }

        public Margin_Account margin_account { get; set; }
    }

    public class Account1
    {
        public string id { get; set; }
    }

    public class Margin_Account
    {
        public string id { get; set; }
    }

}
