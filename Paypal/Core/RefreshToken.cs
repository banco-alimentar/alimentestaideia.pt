using System;
using System.Runtime.Serialization;

namespace PayPalCheckoutSdk.Core
{
    [DataContract]
    public class RefreshToken
    {
        [DataMember(Name = "refresh_token")]
        public string Token;

        [DataMember(Name = "token_type")]
        public string TokenType;

        [DataMember(Name = "expires_in")]
        public string ExpiresIn;

        [DataMember(Name = "id_token")]
        public string IdToken;
    }
}
