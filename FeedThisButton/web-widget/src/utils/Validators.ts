export enum ValidatorType {
    Email,
    ZipCode,
    TaxNumber,
    Name,
    NotEmpty,
    None
}

export function getValidator(type :ValidatorType) :(val:string) => boolean {
    switch(type) {
        case ValidatorType.Email:
            return isValidEmail;
        case ValidatorType.ZipCode:
            return isValidZipCode;
        case ValidatorType.TaxNumber:
            return isValidTaxNumber;
        case ValidatorType.Name:
            return isValidName;
        case ValidatorType.NotEmpty:
            return isNotEmpty;
        case ValidatorType.None:
        default:
            return (val) => true;
    }
}

const emailRegex = /^\s*\S+@\S+\.\S+\s*$/;
function isValidEmail(email :string):boolean {
    return emailRegex.test(email);
}

const zipCodeRegex = /^\s*\d{4}-\d{3}\s*$/;
function isValidZipCode(zipCode :string):boolean {
    return zipCodeRegex.test(zipCode);
}

const taxNumberRegex = /^\s*\d{9}\s*$/;
function isValidTaxNumber(taxNumber :string):boolean {
    return taxNumberRegex.test(taxNumber);
}

function isValidName(name :string):boolean {
    return name != null && name.length >= 3;
}

function isNotEmpty(value:string):boolean {
    return value !== null && value.length > 0;
}

