$.validator.setDefaults({ ignore: null });

$(document).ready(function () {
    $(".langSelector:not(.open)").hover(
        function () {
            $(this).attr("src", "../img/lingua_hover.png");
        }, function () {
            $(this).attr("src", "../img/lingua.png");
        }
    );

    $("body").on("click", ".langSelector:not(.open)", function (event) {
        $(this).addClass('.open').attr("src", "../img/lingua_click.png");
    });

    $("body").on("click", ".langSelector.open", function (event) {
        $(this).removeClass('.open').attr("src", "../img/lingua_hover.png");
    });

    $("body").on("click", ".text9", function (event) {
        $('.stepOne').hide();
        $('.stepTwo').fadeIn();
    });

    $("body").on("click", "#donation-button", function (event) {
        fbq('track', 'submitApplication');
        $('#donation-form').fadeIn();
        $('body').addClass('still');
    });
    $("body").on("click", ".close", function (event) {
        $('.modal').fadeOut();
        $('body').removeClass('still');
    });

    $("body").on("click", ".close2", function (event) {
        $('.stepOne').fadeIn();
        $('.stepTwo').hide();
    });

    var update_donation_items = function () {
        var donatedItems = "";

        for (pos = 0; pos < 6; pos++) {
            itemCount = parseInt($('#field' + (pos + 1)).val());
            if (itemCount > 0) {
                donatedItems += ($('#field' + (pos + 1)).attr('data-dbid')) + ":" + $('#field' + (pos + 1)).val() + ";";
            }
        }

        $("#DonatedItems").val(donatedItems);
    };
    function debounce(func, timeout = 300) {
        let timer;
        return (...args) => {
            clearTimeout(timer);
            timer = setTimeout(() => { func.apply(this, args); }, timeout);
        };
    }

    var calculate_price = function (e) {
        try {
            var newTotal = 0;
            for (var i = 1; i < 7; i++) {
                var value = parseInt($('#field' + i).val());
                if (!isNaN(value)) {
                    var thisValue = parseFloat($('#field' + i).attr('data-value')) * value;
                    var thisQuantity = parseFloat($('#field' + i).attr('data-quantity'));
                    newTotal = newTotal + thisValue;
                    if (value > 0) {
                        $('#field' + i).addClass("positive");
                    } else {
                        $('#field' + i).removeClass("positive");
                    }
                    var thisCart = '.' + $($('#field' + i)).attr('data-target');
                    $(thisCart).html((value * thisQuantity).toFixed(2));
                }
            }
            $('.text8').html(formatCoin(newTotal));
            $('#Amount').val(newTotal);

            update_donation_items();
        }
        catch (e) { }
    };

    var format_input = function () {
        var n = parseInt($(this).val().replace(/\D/g, ''), 10);
        $(this).val(n.toLocaleString());
        calculate_price();
    };

    $('#field1').on('input', format_input);
    $('#field2').on('input', format_input);
    $('#field3').on('input', format_input);
    $('#field4').on('input', format_input);
    $('#field5').on('input', format_input);
    $('#field6').on('input', format_input);
    calculate_price(null);

    var textTotal = $('#total')[0]
    function getTotalValue() {
        let textTotal = $('#total')[0]
        total = parseFloat(textTotal.innerText.replace(/[^0-9.]/g, ''))
        return total;
    }
    function shuffle(array) {
        var currentIndex = array.length, temporaryValue, randomIndex;
        while (0 !== currentIndex) {
            randomIndex = Math.floor(Math.random() * currentIndex);
            currentIndex -= 1;
            temporaryValue = array[currentIndex];
            array[currentIndex] = array[randomIndex];
            array[randomIndex] = temporaryValue;
        }
        return array;
    }

    async function calculateChange(evt) {
        textTotal.removeEventListener("input", calculateChange, false);
        let ele = $('#text8')[0]
        let items = $('input.donation-item')
        console.log(ele)
        total = getTotalValue();
        await new Promise(r => setTimeout(r, 1000));
        if (total !== getTotalValue()) {
            calculateChange(evt)
            return;
        }
        shuffle(items)
        let notChangedCounter = 0;
        if (total > 0) {
            // Clear the current amounts
            for (var i = 0; i < items.length; i++) {
                items[i].value = 0
            }
            while (total > 0) {
                itemValue = parseFloat($(items[0]).attr('data-value'))
                if (total - itemValue > 0) {
                    items[0].value = parseInt(items[0].value) + 1;
                    total = total - itemValue;
                } else {
                    notChangedCounter++;
                }
                if (notChangedCounter > 50) {
                    break;
                }
                shuffle(items)
            }
            calculate_price(evt)
        }
        
        textTotal.addEventListener("input", calculateChange, false);
    }

    if (textTotal !== undefined && textTotal.addEventListener !== undefined) {
        $(textTotal).on("input")
        textTotal.addEventListener("input", calculateChange, false);
    }

    $("body").on("click", ".more", function (event) {
        var value = parseInt($(this).parent().find('input').val());
        value = value + 1;

        // update totals
        if (value > 0) {
            var total = parseFloat($('.text8').html());
            var thisValue = parseFloat($(this).parent().find('input').attr('data-value'));
            var thisQuantity = parseFloat($(this).parent().find('input').attr('data-quantity'));
            var newTotal = total + thisValue;
            $('.text8').html(formatCoin(newTotal));
            $('#Amount').val(newTotal);
            var thisCart = '.' + $(this).parent().find('input').attr('data-target');
            $(thisCart).html((value * thisQuantity).toFixed(2));
            $(this).parent().find('input').addClass("positive");
        } else {
            $(this).parent().find('input').removeClass("positive");
        }

        $(this).parent().find('input').val(value);
    });

    $("body").on("click", ".less", function (event) {
        var value = parseInt($(this).parent().find('input').val());
        value = value - 1;

        // update totals
        if (parseInt(value) >= 0) {
            var total = parseFloat($('.text8').html());
            var thisValue = parseFloat($(this).parent().find('input').attr('data-value'));
            var thisQuantity = parseFloat($(this).parent().find('input').attr('data-quantity'));
            var newTotal = total - thisValue;
            $('.text8').html(formatCoin(newTotal));
            $('#Amount').val(newTotal);
            var thisCart = '.' + $(this).parent().find('input').attr('data-target');
            $(thisCart).html((value * thisQuantity).toFixed(2));
            $(this).parent().find('input').addClass("positive");
        }
        if (parseInt(value) <= 0) {
            $(this).parent().find('input').removeClass("positive");
        }

        if (parseInt(value) < 0) { value = 0; }
        $(this).parent().find('input').val(value);
    });

    $('body').on('click', '.more,.less', update_donation_items);

    //$('#Address').val('-');
    //$('#PostalCode').val('-');
    //$('#Nif').val('000000000');

    //$('#WantsReceipt').val('false');
    //$('#AcceptsTerms').val('false');

    $('#WantsReceiptCheckBox').click(function () {
        if ($(this).is(':checked') || $(this).is('on')) {
            $('.recibo').show();
            $('#Address').attr('data-val', true);
            $('#PostalCode').attr('data-val', true);
            $('#Nif').attr('data-val', true);
            $('#Address').rules("add", "required")
            $('#PostalCode').rules("add", "required")
            $('#Nif').rules("add", "required")
        }
        else {
            $('.recibo').hide();
            $('#Address').attr('data-val', false);
            $('#PostalCode').attr('data-val', false);
            $('#Nif').attr('data-val', false);
            $('#Nif').attr('data-val', true);
            $('#Address').rules("remove", "required")
            $('#PostalCode').rules("remove", "required")
            $('#Nif').rules("remove", "required")
        }

        $('#WantsReceipt').val($(this).is(':checked'));
    });

    $('#AcceptsSubscriptionCheckBox').click(function () {
        if ($('#AcceptsSubscriptionCheckBox').is(':checked') || $('#AcceptsSubscriptionCheckBox').is('on')) {
            $('#divSubscriptionFrequency').show();
        }
        else {
            $('#divSubscriptionFrequency').hide();
        }

        $('#WantsReceipt').val($('#WantsReceiptCheckBox').is(':checked'));
    });

    if ($('#WantsReceiptCheckBox').is(':checked') || $('#WantsReceiptCheckBox').is('on')) {
        $('.recibo').show();
    }

    $('#AcceptsTermsCheckBox').click(function () {
        $('#AcceptsTerms').val($('#AcceptsTermsCheckBox').is(':checked'));
    });

    $('#Amount').addClass('amount');
    $.validator.addMethod('amount', function () {
        return $('#Amount').val() > 0;
    }, 'Deve seleccionar alimentos para doar.');

    $('#AcceptsTerms').addClass('acceptsTerms');
    $.validator.addMethod('acceptsTerms', function () {
        return $('#AcceptsTerms').val() == 'true';
    }, 'Deve aceitar a Pol&iacute;tica de Privacidade.');

    $('#Nif').addClass('nif');
    $.validator.addMethod('nif', function (nif) {
        var allZero = true;
        for (i = 0; i < nif.length; i++) {
            if (nif.charAt(i) != '0') {
                allZero = false;
            }
        }

        if (nif == "") {
            return true;
        }

        if (allZero) {
            return false;
        }

        if (nif == '000000000') {
            return false;
        }

        var c;
        var checkDigit = 0;
        //Check if is not null, is numeric and if has 9 numbers
        if (nif != null && /^\d+$/.test(nif) && nif.length == 9) {
            //Get the first number of NIF
            c = nif.charAt(0);
            //Check firt number is (1, 2, 5, 6, 8 or 9)
            if (c == '1' || c == '2' || c == '5' || c == '6' || c == '8' || c == '9') {
                //Perform CheckDigit calculations
                checkDigit = c * 9;
                var i = 0;
                for (i = 2; i <= 8; i++) {
                    checkDigit += nif.charAt(i - 1) * (10 - i);
                }
                checkDigit = 11 - (checkDigit % 11);
                //if checkDigit is higher than ten set it to zero
                if (checkDigit >= 10)
                    checkDigit = 0;
                //Compare checkDigit with the last number of NIF
                //If equal the NIF is Valid.
                if (checkDigit == nif.charAt(8))
                    return true;
            }
        }
        return false;
    }, 'NIF inv&aacute;lido.');

    if ($('.validation-summary-errors').length > 0) {
        $('#donation-button').click();
    }
});

function formatCoin(value) {
    value = formatter.format(value);
    value = value.replace(" ", "");
    value = value.replace(",", ".");
    return value;
}

const formatter = new Intl.NumberFormat('pt-PT', {
    style: 'currency',
    currency: 'EUR',
    minimumFractionDigits: 2
})