using Link.BA.Donate.Business;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var donation = new Donation();

            var entity = donation.GetDonationByReference("004 234 378");
            var items = donation.GetDonationItemsByDonationId(entity[0].DonationId);
            var messageBodyPath = @"C:\Users\fabio.ferreira\Downloads\BancoAlimentar\Content\ReceiptToDonor.htm";
            var receiptTemplatePath = @"C:\Users\fabio.ferreira\Downloads\BancoAlimentar\Content\ReceiptTemplate.docx";

            Mail.SendReceiptMailToDonor(entity[0], items, messageBodyPath, receiptTemplatePath);
        }
    }
}
