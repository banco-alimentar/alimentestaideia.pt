using System;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Ajax;
using System.Web.Mvc.Html;
using System.Web.Routing;
using CaptchaMVC.Models;

namespace CaptchaMVC.HtmlHelpers
{
    public static class CaptchaHelper
    {
        private const string CaptchaFormat = @"<img id=""CaptchaImage"" src=""{0}""/>{1}";

        #region Working with the captcha

        /// <summary>
        /// Helper method to create captcha.
        /// </summary>
        /// <param name="htmlHelper"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static MvcHtmlString Captcha(this HtmlHelper htmlHelper, int length)
        {
            return GenerateFullCaptcha(htmlHelper, length);
        }

        /// <summary>
        /// Create full captcha
        /// </summary>
        /// <param name="htmlHelper"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        internal static MvcHtmlString GenerateFullCaptcha(HtmlHelper htmlHelper, int length)
        {
            EncryptorModel encryptorModel = GetEncryptorModel();
            string captchaText = RandomText.Generate(length);
            string encryptText = Encryption.Encrypt(captchaText, encryptorModel.Password, encryptorModel.Salt);

            var urlHelper = new UrlHelper(htmlHelper.ViewContext.RequestContext);
            string url = urlHelper.Action("Create", "CaptchaImage", new {encryptText});

            var ajax = new AjaxHelper(htmlHelper.ViewContext, htmlHelper.ViewDataContainer);
            //var refresh = ajax.ActionLink("Refresh", "NewCaptcha", "CaptchaImage", new {l = length}, new AjaxOptions { UpdateTargetId = "CaptchaDeText", OnSuccess = "Success" });

            // LINK (atl): workaround for the problem with the automatic model binding 
            string hiddenField =
                String.Format("<input id=\"{0}\" name=\"{0}\" type=\"hidden\" value=\"{1}\" />",
                              "CaptchaDeText", encryptText);

            string textField =
                String.Format("<input id=\"{0}\" name=\"{0}\" type=\"text\" value=\"{1}\" />",
                              "CaptchaInputText", String.Empty);

            string html =
                string.Format(
                    "{0}<div>" +
                    HttpContext.GetLocalResourceObject("~/Views/Donation/Index.cshtml", "EscrevaAPalavraAcima") +
                    "</div>{1}",
                    string.Format(CaptchaFormat, url, hiddenField), textField);

            return MvcHtmlString.Create(html);
        }

        /// <summary>
        /// Create full captcha
        /// </summary>
        /// <param name="htmlHelper"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        internal static MvcHtmlString OriginalGenerateFullCaptcha(HtmlHelper htmlHelper, int length)
        {
            EncryptorModel encryptorModel = GetEncryptorModel();
            string captchaText = RandomText.Generate(length);
            string encryptText = Encryption.Encrypt(captchaText, encryptorModel.Password, encryptorModel.Salt);

            var urlHelper = new UrlHelper(htmlHelper.ViewContext.RequestContext);
            string url = urlHelper.Action("Create", "CaptchaImage", new {encryptText});

            var ajax = new AjaxHelper(htmlHelper.ViewContext, htmlHelper.ViewDataContainer);
            MvcHtmlString refresh = ajax.ActionLink("Refresh", "NewCaptcha", "CaptchaImage", new {l = length},
                                                    new AjaxOptions
                                                        {UpdateTargetId = "CaptchaDeText", OnSuccess = "Success"});

            return
                MvcHtmlString.Create(
                    string.Format(CaptchaFormat, url, htmlHelper.Hidden("CaptchaDeText", encryptText)) +
                    refresh.ToHtmlString() +
                    HttpContext.GetLocalResourceObject("~/Views/Donation/Index.cshtml", "EscrevaAPalavraAcima") +
                    htmlHelper.TextBox("CaptchaInputText"));
        }

        /// <summary>
        /// Create partial captcha
        /// </summary>
        /// <param name="requestContext"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        internal static RefreshModel GeneratePartialCaptcha(RequestContext requestContext, int length)
        {
            EncryptorModel encryptorModel = GetEncryptorModel();
            string captchaText = RandomText.Generate(length);
            string encryptText = Encryption.Encrypt(captchaText, encryptorModel.Password, encryptorModel.Salt);

            var urlHelper = new UrlHelper(requestContext);
            string url = urlHelper.Action("Create", "CaptchaImage", new {encryptText});


            return new RefreshModel {Code = encryptText, Image = url};
        }

        /// <summary>
        /// Check for proper input captcha
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static bool Verify(CaptchaModel model)
        {
            try
            {
                EncryptorModel encryptorModel = GetEncryptorModel();

                if (encryptorModel == null)
                {
                    return false;
                }

                string textDecrypt = Encryption.Decrypt(model.CaptchaDeText, encryptorModel.Password,
                                                        encryptorModel.Salt);
                return textDecrypt == model.CaptchaInputText;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the model for decoding from the web.config
        /// </summary>
        /// <returns></returns>
        internal static EncryptorModel GetEncryptorModel()
        {
            string pass = ConfigurationManager.AppSettings["CaptchaPass"];
            string salt = ConfigurationManager.AppSettings["CaptchaSalt"];
            if ((string.IsNullOrEmpty(pass) || string.IsNullOrEmpty(salt)))
                throw new ConfigurationErrorsException("In the web.config file, there are no options for Captcha.");
            try
            {
                var encryptorModel = new EncryptorModel {Password = pass, Salt = Convert.FromBase64String(salt)};
                return encryptorModel;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Returns the implementation IGenerateImage custom or default.
        /// </summary>
        /// <returns></returns>
        internal static IGenerateImage GetGenerateImage()
        {
            string nameType = ConfigurationManager.AppSettings["CaptchaIGenerate"];

            if (!string.IsNullOrEmpty(nameType))
            {
                Assembly[] allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                Type typeImage = null;
                foreach (Assembly assembly in allAssemblies.Where(assembl => !assembl.FullName.Contains("System")))
                {
                    typeImage = (from type in assembly.GetTypes()
                                 where type.IsClass &&
                                       (type.GetInterface("IGenerateImage") != null) && type.FullName == nameType
                                 select type).FirstOrDefault();
                    if (typeImage != null)
                        break;
                }

                if (typeImage != null)
                {
                    var result = (IGenerateImage) typeImage.Assembly.CreateInstance(typeImage.FullName, true);
                    return result;
                }
            }

            return new GenerateImage();
        }

        #endregion

        /*
        private const string CaptchaFormat = @"
<img id=""CaptchaImage"" src=""{0}""/>
{1}
<br/>
";*/
    }
}