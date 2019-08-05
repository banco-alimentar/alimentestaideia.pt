using Link.BA.Donate.Business;
using Link.BA.Donate.Models;
using Link.PT.Telegramas.CommonLibrary.Template;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReenviarRecibos
{
    class Program
    {
        static void Main(string[] args)
        {
            var donation = new Link.BA.Donate.Business.Donation();
            var doacoes = donation.GetAllDonors();

            var recibos = new List<AllDonorsEntity>();

            foreach(var doacao in doacoes)
            {
                if(doacao.Coluna2 != null && doacao.Coluna2 <= 498)
                {
                    recibos.Add(doacao);
                }
            }

            foreach(var recibo in recibos)
            {
                string subject = "Banco Alimentar: Anulação de recibo";
                string body = File.ReadAllText("Data/ReceiptToDonor.htm");
                string mailTo = recibo.Email;

                var nRecibo = recibo.Coluna2.ToString().PadLeft(4, '0');

                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                dictionary.Add("<Ano>", DateTime.Now.Year.ToString());
                dictionary.Add("<Recibo>", nRecibo);
                dictionary.Add("<Nome>", recibo.Name);
                dictionary.Add("<NIF>", recibo.NIF);
                dictionary.Add("<Quantia>", recibo.ServiceAmount.ToString());
                dictionary.Add("<Extenso>", Converstion.MoneyToString(recibo.ServiceAmount.ToString()));
                dictionary.Add("<Data>", DateTime.Now.ToString(""));

                var destFilename = string.Format("C{0}-{1}.docx", DateTime.Now.Year, nRecibo);
                var destFile = string.Format("{0}{1}.docx", "Invoices/", destFilename);
                var invoice = "Data/ReceiptTemplate.docx";

                TemplateService.ApplyDocxTemplate(invoice, dictionary, destFile);

                bool mailResult;

                using (Stream stream = new FileStream(destFile, FileMode.Open))
                {
                    var bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, (int)stream.Length);
                    stream.Position = 0;
                    mailResult = Mail.SendMail(body, subject, mailTo/*, stream, destFilename*/);
                }

                File.Delete(destFile);
            }
        }
    }
}
