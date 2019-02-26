using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Link.BA.Donate.Models
{
    public partial class DonationStatus
    {
        public enum Status
        {
            Payed=1,
            NotPayed=2,
            WaitingPayment=3
        }
    }
}
