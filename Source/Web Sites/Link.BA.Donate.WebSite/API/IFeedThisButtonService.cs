using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace Link.BA.Donate.WebSite.API
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IFeedThisButtonService" in both code and config file together.
    [ServiceContract]
    public interface IFeedThisButtonService
    {
        [OperationContract]
        DonateResponse Donate(int bancoAlimentar, bool empresa, string nome, string nomeEmpresa, string email, string pais, bool recibo, string morada, string codigoPostal, string nif, string itens, decimal valor, string apipKey);
    }
}
