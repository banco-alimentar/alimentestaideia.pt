import { cart, foobBankToDonate, isShoppingCartComplete } from '../store/Cart';
import { acceptedPrivacyPolicy, name, company, country, email, isPersonalDataFormValid, needsReceipt, receiptEmail, receiptZipCode, receptTaxNumber, securityCode } from '../store/PersonalData';
import { amount, entity, reference } from '../store/Multibanco';
import { loading, errorMessage, executionMode } from '../store/Global';

import { generateMultibancoReference as generateMultibancoReferenceSvc } from './api';
import { get } from 'svelte/store';
import type { AxiosError } from 'axios';

const genericErrorMessage = 'Ocorreu um erro inesperado, por favor tente mais tarde';

export async function generateMultibancoReference() {
    let nsEnv = 'http://schemas.xmlsoap.org/soap/envelope/';
    let nsBa = 'http://schemas.datacontract.org/2004/07/Link.BA.Donate.WebSite.API';

    loading.set(true);
    try {
        const execMode = get(executionMode);

        if (execMode === 'mock') {
            amount.set(Number(get(cart).getTotalAmount()));
            entity.set('20641');
            reference.set('123 456 789');
            new Promise(resolve => setTimeout(resolve, 500));
        } else {
            let resp = await generateMultibancoReferenceSvc( get(cart),
            get(foobBankToDonate).value,
            get(company),
            get(name),
            get(email),
            get(country),
            get(securityCode),
            get(needsReceipt),
            get(receiptEmail) ?? '',
            get(receiptZipCode) ?? '',
            get(receptTaxNumber) ?? '',
            get(acceptedPrivacyPolicy));

            let parser = new DOMParser();
            let doc = parser.parseFromString(resp.data, 'text/xml');
            let respXml = doc.firstElementChild
            .getElementsByTagNameNS(nsEnv, 'Body')[0]
            .firstElementChild
            .firstElementChild;
            amount.set(Number(respXml.getElementsByTagNameNS(nsBa, 'ServiceAmount')[0].innerHTML));
            entity.set(respXml.getElementsByTagNameNS(nsBa, 'ServiceEntity')[0].innerHTML);
            reference.set(respXml.getElementsByTagNameNS(nsBa, 'ServiceReference')[0].innerHTML);
        }
    } catch (error) {
        errorMessage.set(genericErrorMessage);
        logSoapFault(error);
        throw error;
    } finally {
        loading.set(false);
    }
    
}


function logSoapFault(error: any): void {
    if(!error?.response) // not an axios error
        return;
    let ns = 'http://schemas.xmlsoap.org/soap/envelope/';
    let parser = new DOMParser();
    let doc = parser.parseFromString(error.response, 'text/xml');
    let message = doc.firstElementChild
        .getElementsByTagNameNS(ns, 'Body')[0]
        .getElementsByTagNameNS(ns, 'Fault')[0]
        .getElementsByTagName('faultstring')[0].innerHTML;
    console.log(`Service error: ${message}`);
}