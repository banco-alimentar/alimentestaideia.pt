using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Acn.BA.Gamification.Business
{
    class GamificationException : Exception
    {
        public string FriendlyUserError { get; private set; }

        public GamificationException(string message, string friendlyErrorMessage, Exception innerException)
            :base(message, innerException)
        {
            this.FriendlyUserError = friendlyErrorMessage;
        }

        public GamificationException(string message, string friendlyErrorMessage)
            : base(message)
        {
            this.FriendlyUserError = friendlyErrorMessage;
        }
    }
}
