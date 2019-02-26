namespace CaptchaMVC.Models
{
    /// <summary>
    /// Model to describe the data Captcha.
    /// </summary>
    public class CaptchaModel
    {
        public string CaptchaInputText { get; set; }

        public string CaptchaDeText { get; set; }
    }

    /// <summary>
    /// Model to describe the data for encryption.
    /// </summary>
    public class EncryptorModel
    {
        public string Password { get; set; }

        public byte[] Salt { get; set; }
    }

     internal class RefreshModel
    {
        public string Image { get; set; }

        public string Code  { get; set; }
    }
}
