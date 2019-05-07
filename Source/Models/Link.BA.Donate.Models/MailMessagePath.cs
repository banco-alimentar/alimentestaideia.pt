namespace Link.BA.Donate.Models
{
    public class MailMessagePath
    {
        public string ReferenceToDonorPath { get; set; }
        public string PaymentToDonorPath { get; set; }
        public string PaymentToBancoAlimentarPath { get; set; }

        public string ReceiptToDonorPath { get; set; }
        public string ReceiptTemplatePath { get; set; }
    }
}