import axios, { AxiosResponse } from 'axios';
import { get } from 'svelte/store';
import config from '../../ba-config';
import type { MultibancoReference, ShoppingCart } from '../models';
import { executionMode } from '../store/Global';


const soapClient = axios.create({
    timeout: 30000,
    headers: {'Content-Type': 'text/xml'},
});


export async function generateMultibancoReference (
    cart:ShoppingCart,
    foobBankToDonate:string,
    company:boolean,
    name:string,
    email:string,
    country:string,
    securityCode:string,
    needsReceipt:boolean,
    receiptEmail:string,
    receiptZipCode:string,
    receptTaxNumber:string,
    acceptedPrivacyPolicy:boolean,
    ): Promise<AxiosResponse<string>> {
    const execMode = get(executionMode);
    const
        endpointUrl = execMode === 'prod' ? config.baseUrlProd : config.baseUrlDev,
        apiKey = execMode === 'prod' ? config.apiKeyProd : config.apiKeyDev;


    let soapEnv = `<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:alim="http://alimentestaideia.pt/">
    <soapenv:Header/>
    <soapenv:Body>
       <alim:Donate>
          <alim:bancoAlimentar>${foobBankToDonate}</alim:bancoAlimentar>
          <alim:empresa>${company}</alim:empresa>
          <alim:nome>${company ? '' : name}</alim:nome>
          <alim:nomeEmpresa>${company ? name : ''}</alim:nomeEmpresa>
          <alim:email>${email}</alim:email>
          <alim:pais>${country}</alim:pais>
          <alim:recibo>${needsReceipt}</alim:recibo>
          <alim:morada>${receiptZipCode}</alim:morada>
          <alim:codigoPostal>${receiptZipCode}</alim:codigoPostal>
          <alim:nif>${receptTaxNumber}</alim:nif>
          <alim:itens>${getItemsString(cart)}</alim:itens>
          <alim:valor>${cart.getTotalAmount()}</alim:valor>
          <alim:apipKey>${apiKey}</alim:apipKey>
       </alim:Donate>
    </soapenv:Body>
 </soapenv:Envelope>`;
    return soapClient.post<string>(endpointUrl, soapEnv, {headers: {'SOAPAction': 'http://alimentestaideia.pt/FeedThisButtonService/Donate'}});
}

function getItemsString(cart:ShoppingCart):string {
    return Object.keys(cart.items)
        .map(key => cart.items[key])
        .reduce<string>((prev, curr, idx, arr) => prev + ';' + `${curr.id}:${curr.quantity}`, '' );
}