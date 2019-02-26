using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Web.Mvc;
using CaptchaMVC.HtmlHelpers;
using CaptchaMVC.Models;

namespace CaptchaMVC.Controllers
{

    public sealed class CaptchaImageController : Controller
    {

        private const string ReturnScript =
            @"
$('#CaptchaDeText').attr(""value"", ""{0}"");
$('#CaptchaImage').attr(""src"", ""{1}"");";

        /// <summary>
        /// Generation CAPTCHA
        /// </summary>
        /// <param name="encryptText">The text for captcha</param>
        public void Create(string encryptText)
        {
            try
            {
                if (Request.UrlReferrer.AbsolutePath == Request.Url.AbsolutePath)
                    throw new Exception();

                var encryptorModel = CaptchaHelper.GetEncryptorModel();
                var generateImage = CaptchaHelper.GetGenerateImage();

                if (string.IsNullOrEmpty(encryptText)  || (encryptorModel == null))
                    throw new ArgumentException();

                var captchaText = Encryption.Decrypt(encryptText, encryptorModel.Password, encryptorModel.Salt);
                var capthaBmp = generateImage.Generate(captchaText);

                Response.ContentType = "image/gif";
                capthaBmp.Save(Response.OutputStream, ImageFormat.Gif);
            }
            catch (Exception)
            {

                Response.ContentType = "image/gif";
                ErrorBitmap().Save(Response.OutputStream, ImageFormat.Gif);
            }
        }

        /// <summary>
        /// Asynchronous update of the new CAPTCHA.
        /// </summary>
        /// <param name="l">length</param>
        /// <returns></returns>
        public ActionResult NewCaptcha(int l)
        {
            if (l < 4)
                l = 4;
            if (Request.IsAjaxRequest())
            {
                var captcha = CaptchaHelper.GeneratePartialCaptcha(Request.RequestContext, l);
                return JavaScript(string.Format(ReturnScript, captcha.Code, captcha.Image));
            }

            return Redirect(Request.UrlReferrer.AbsolutePath);
        }

        private static Bitmap ErrorBitmap()
        {
            var errorBmp = new Bitmap(200, 70);
            var gr = Graphics.FromImage(errorBmp);
            gr.DrawLine(Pens.Red, 0, 0, 200, 70);
            gr.DrawLine(Pens.Red, 0, 70, 200, 0);
            gr.Dispose();
            return errorBmp;

        }
    }
}

