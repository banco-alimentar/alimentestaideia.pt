(function () {
    'use strict';

    const form = document.getElementById('donation2-form');
    if (!form) {
        return;
    }

    const currencyFormatter = new Intl.NumberFormat(document.documentElement.lang || 'pt-PT', {
        style: 'currency',
        currency: 'EUR'
    });
    const totalOutput = form.querySelector('[data-donation2-total]');
    const summaryItems = form.querySelector('[data-donation2-summary-items]');

    function normalizeQuantity(input) {
        const parsed = Number.parseInt(input.value, 10);
        const quantity = Number.isFinite(parsed) ? Math.min(9999, Math.max(0, parsed)) : 0;
        input.value = quantity.toString();
        return quantity;
    }

    function updateSummary() {
        let total = 0;
        summaryItems.replaceChildren();

        form.querySelectorAll('[data-donation2-product]').forEach(function (product) {
            const input = product.querySelector('.donation2-quantity__input');
            const quantity = normalizeQuantity(input);
            const cost = Number.parseFloat(product.dataset.cost);
            const name = product.querySelector('h3').textContent.trim();
            total += quantity * cost;

            if (quantity > 0) {
                const item = document.createElement('li');
                const itemName = document.createElement('span');
                const itemValue = document.createElement('span');
                itemName.textContent = `${quantity} × ${name}`;
                itemValue.textContent = currencyFormatter.format(quantity * cost);
                item.append(itemName, itemValue);
                summaryItems.appendChild(item);
            }
        });

        if (!summaryItems.childElementCount) {
            const emptyItem = document.createElement('li');
            emptyItem.className = 'donation2-summary__empty';
            emptyItem.textContent = summaryItems.dataset.emptyMessage;
            summaryItems.appendChild(emptyItem);
        }

        totalOutput.value = currencyFormatter.format(total);
        totalOutput.textContent = currencyFormatter.format(total);
    }

    form.addEventListener('click', function (event) {
        const button = event.target.closest('[data-donation2-increase], [data-donation2-decrease]');
        if (!button) {
            return;
        }

        const input = button.parentElement.querySelector('.donation2-quantity__input');
        const change = button.hasAttribute('data-donation2-increase') ? 1 : -1;
        input.value = (normalizeQuantity(input) + change).toString();
        normalizeQuantity(input);
        updateSummary();
    });

    form.addEventListener('input', function (event) {
        if (event.target.matches('.donation2-quantity__input')) {
            updateSummary();
        }
    });

    function bindDisclosure(toggleSelector, fieldsSelector) {
        const toggle = form.querySelector(toggleSelector);
        const fields = form.querySelector(fieldsSelector);
        if (!toggle || !fields) {
            return;
        }

        function updateDisclosure() {
            fields.classList.toggle('donation2--hidden', !toggle.checked);
        }

        toggle.addEventListener('change', updateDisclosure);
        updateDisclosure();
    }

    bindDisclosure('[data-donation2-receipt-toggle]', '[data-donation2-receipt-fields]');
    bindDisclosure('[data-donation2-subscription-toggle]', '[data-donation2-subscription-fields]');
    updateSummary();

    if (typeof window.fbq === 'function') {
        window.fbq('track', 'Donate');
    }
}());