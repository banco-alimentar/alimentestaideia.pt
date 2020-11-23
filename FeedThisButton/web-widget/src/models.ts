import type { reference } from "./store/Multibanco";

export type ImageName = "heart"| "arroz"| "atum"| "azeite"| "leite"| "oleo"| "salsichas"| "seta"|"cartaocredito"|"paypal"|"mbway"|"multibanco";

export class ShoppingCart {
    constructor() {
        this.addItem({id:1, key: 'azeite', label: 'Azeite', quantity: 0, price: 2.3});
        this.addItem({id:2, key: 'oleo', label: 'Óleo', quantity: 0, price: 1});
        this.addItem({id:4, key: 'atum', label: 'Atum', quantity: 0, price: 0.7});
        this.addItem({id:3, key: 'leite', label: 'Leite', quantity: 0, price: 0.4});
        this.addItem({id:5, key: 'salsichas', label: 'Salsichas', quantity: 0, price: 0.4});
        this.addItem({id:6, key: 'arroz', label: 'Arroz', quantity: 0, price: 0.6});
    }

    private itemKeys :Array<string> = [];
    // cannot be private, we use it for binding
    items :{ [key: string] :ShoppingCartItem} = {};

    isEmpty() {
        return this.getTotalAmount() == 0;
    }
    getLabel(item: string): string {
        return this.items[item]?.label;
    }

    getQuantity(item: string): number {
        return this.items[item]?.quantity;
    }

    getAmount(item: string): number {
        let itemObj = this.items[item];
        return this.round(itemObj.quantity * itemObj.price);
    }

    getTotalAmount() :number {
        return this.round(this.getItemKeys().map(x => this.getAmount(x)).reduce((total, amt) => total + amt));
    }

    getItemKeys(): Array<string> {
        return this.itemKeys;
    }

    addItem(item :ShoppingCartItem): void {
        this.items[item.key] = item;
        this.itemKeys.push(item.key);
    }

    private round(val: number): number {
        return Math.round( ( val + Number.EPSILON ) * 100 ) / 100
    }
}

export class MultibancoReference {
    entity:string;
    reference: string;
    amount:number;

    constructor(entity:string, reference:string, amount:number) {
        this.entity = entity;
        this.reference = reference;
        this.amount = amount;
    }
}

export class ShoppingCartItem {
    id: number;
    key :string;
    label :string;
    price :number;
    quantity :number;
}

export type ItemKey = "arroz"|"atum"|"azeite"|"leite"|"oleo"|"salsichas" ;

export enum NavigationStep {
    ShoppingCart = 1,
    PersonalData = 2,
    PaymentMethod = 3,
    MultibancoConfirmation = 4,
}

export class Country {
    code :string;
    label :string;

    constructor(code, label) {
        this.code = code;
        this.label = label;
    }
}

export class Option {
    label:string;
    image:ImageName;
    disabled:boolean;
    constructor(label :string, image :ImageName = null, disabled:boolean = false) {
        this.label = label;
        this.image = image;
        this.disabled = disabled;
    }
}

