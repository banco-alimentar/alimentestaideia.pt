jQuery.validator.addMethod("minvalue",
    function (value, element, param) {
        var amount = parseFloat(value);
        var minimumValue = parseFloat(element.attributes["data-val-minvalue-minvalue"].nodeValue);
        if (amount < minimumValue) {
            return false;
        }
        else {
            return true;
        }
    });

jQuery.validator.unobtrusive.adapters.addBool("minvalue");