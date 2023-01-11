// -----------------------------------------------------------------------
// <copyright file="QrCodeGenerator.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using System;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using QRCoder;

    /// <summary>
    /// QR Code Generator Page Model.
    /// </summary>
    public class QrCodeGeneratorModel : PageModel
    {
        /// <summary>
        /// Execute the GET operation.
        /// </summary>
        public void OnGet(string nifCustomer, string invoiceNumber, double invoiceValue)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode($"A:504615947*B:{nifCustomer}*C:PT*D:FT*E:N*F:{GetFormatedDateTime(DateTime.Now)}*G:FT {invoiceNumber}*H:JFF66VKK-782548767*I1:PT*I7:{invoiceValue}*I8:0*N:9.45*O:50.55*Q:kGvK*R:2386", QRCodeGenerator.ECCLevel.Q);

            PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeImage = qrCode.GetGraphic(20);

            Response.ContentType = "image/png";
            Response.Body.Write(qrCodeImage, 0, qrCodeImage.Length);
        }

        private string GetFormatedDateTime(DateTime value)
        {
            return string.Concat(value.Year, value.Month.ToString("D2"), value.Day.ToString("D2"));
        }
    }
}
