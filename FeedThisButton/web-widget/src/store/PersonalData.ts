import { writable, derived } from 'svelte/store';
import type {Writable} from 'svelte/store';
import { getValidator, ValidatorType } from '../utils/Validators';

export const company :Writable<boolean> = writable(false);
export const name :Writable<string> = writable(null);
export const email :Writable<string> = writable(null);
export const country :Writable<string> = writable(null);
export const securityCode :Writable<string> = writable(null);

export const needsReceipt :Writable<boolean> = writable(true);
export const receiptEmail :Writable<string> = writable(null);
export const receiptZipCode :Writable<string> = writable(null);
export const receptTaxNumber :Writable<string> = writable(null);

export const acceptedPrivacyPolicy :Writable<boolean> = writable(false);

export const isPersonalDataFormValid = derived(
    [name, email, country, needsReceipt, receiptEmail, receiptZipCode, receptTaxNumber, acceptedPrivacyPolicy],
    ([$name, $email, $country, $needsReceipt, $receiptEmail, $receiptZipCode, $receptTaxNumber, $acceptedPrivacyPolicy]) => {
        let personalDataPart = getValidator(ValidatorType.Name)($name)
            && getValidator(ValidatorType.Email)($email)
            && getValidator(ValidatorType.NotEmpty)($country);
        let receiptPart = !$needsReceipt ||
         (  getValidator(ValidatorType.Email)($receiptEmail)
            && getValidator(ValidatorType.ZipCode)($receiptZipCode)
            && getValidator(ValidatorType.TaxNumber)($receptTaxNumber)
         );
         return personalDataPart && receiptPart && $acceptedPrivacyPolicy
    }
);