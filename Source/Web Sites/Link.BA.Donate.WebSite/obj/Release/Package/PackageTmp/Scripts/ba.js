function AddProduct(counter) {
    var current = parseInt($('#val' + counter).html());
    $('#val' + counter).text(current + 1);
    CalculateTotal();
}

function AddProductTen(counter) {
    var current = parseInt($('#val' + counter).html());
    $('#val' + counter).text(current + 10);
    CalculateTotal();
}

function RemoveProductTen(counter) {
    var current = parseInt($('#val' + counter).html());
    $('#val' + counter).text(current > 10 ? current - 10 : 0);
    CalculateTotal();
}

function RemoveProduct(counter) {
    var current = parseInt($('#val' + counter).html());
    if (current == '0') return;
    $('#val' + counter).text(current - 1);
    CalculateTotal();
}

function CalculateTotal() {
    var pos = 0;
    var total = 0;
    var itemCount = 0;
    var donatedItems = "";

    for (pos = 0; pos < totalProducts; pos++) {
        itemCount = parseInt($('#val' + ids[pos]).html());
        if (itemCount > 0) {
            total = total + (itemCount * parseFloat(amounts[pos].replace(/,/g, '.')));
            donatedItems = donatedItems + ids[pos] + ":" + $('#val' + ids[pos]).html() + ";";
        }
    }
    total = ("000000" + total.toFixed(2)).slice(-8);

    $('#amountDiv').text(total);
    $("#Amount").val(total);
    $("#DonatedItems").val(donatedItems);
}

function getIdCounter(id) {
    return id.replace(/(lessten|more|less|ten)/, '');
}

$(document).ready(function () {
    //CalculateTotal();
    $('img.less').click(function (e) {
        RemoveProduct(getIdCounter($(e.target).attr("id")));
    });
    $('img.more').click(function (e) {
        AddProduct(getIdCounter($(e.target).attr("id")));
    });
    $('img.ten').click(function (e) {
        AddProductTen(getIdCounter($(e.target).attr("id")));
    });
    $('img.lessten').click(function (e) {
        RemoveProductTen(getIdCounter($(e.target).attr("id")));
    });
    $('img.donor').dblclick(function (e) {
        if (confirm('Deseja denunciar a imagem como abusiva?'))
            $.ajax({
                url: '../../Donation/ReportBadPicture',
                data: 'id=' + getIdCounter($(e.target).attr("id")),
                dataType: 'json',
                type: 'post',
                success: alert('Imagem denunciada com sucesso.')
            });
    });
    $('#Nif').addClass('nif');
    $.validator.addMethod('nif', function (nif) {
        if (nif == '000000000') {
            return true;
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
    $('.rightFormObs').hide();
    $('.text-box').focus(function () {
        $(this).parent().next('.rightFormObs').show();
    });
    $('.text-box').blur(function () {
        $(this).parent().next('.rightFormObs').hide();
    });
    /*
    $("input[name='Picture']").click(function () {
    $(this).parent().next('.rightFormObs').show();
    });
    */
    $("input[name='Private']").click(function () {
        if ($("#Private:checked").val() == 'true') {
            $("label[for='Picture']").text("Foto");
        }
        else {
            $("label[for='Picture']").text("Logo");
        }
    });
    $('#Hidden').val('IndexFB');

    //$('#WantsReceipt').attr('checked', 'true');
    $('#receiptForm').hide();
    $('#Address').val('-');
    $('#PostalCode').val('-');
    $('#Nif').val('000000000');
    $('#WantsReceipt').click(function () {
        if ($('#WantsReceipt').is(':checked')) {
            $('#receiptForm').show();
            $('#Address').val('');
            $('#PostalCode').val('');
            $('#Nif').val('');
        }
        else {
            $('#receiptForm').hide();
            $('#Address').val('-');
            $('#PostalCode').val('-');
            $('#Nif').val('000000000');
        }
    });

    $('#AcceptsTerms').addClass('acceptsTerms');
    $.validator.addMethod('acceptsTerms', function () {
        return $('#AcceptsTerms').attr('checked');
    }, 'Deve aceitar a Pol&iacute;tica de Privacidade.');
});

//DO NOT DELETE, USED BY CAPTCHA
function Success() {

}

function Basket() {
    for (counter = 0; counter < totalProducts; counter++) {
        if (ids[counter] == 3) {
            AddProduct(ids[counter]);
            AddProduct(ids[counter]);
            AddProduct(ids[counter]);
            AddProduct(ids[counter]);
            AddProduct(ids[counter]);
            AddProduct(ids[counter]);
        }
        else if (ids[counter] == 5) {
            AddProduct(ids[counter]);
            AddProduct(ids[counter]);
            AddProduct(ids[counter]);
            AddProduct(ids[counter]);
        }
        else {
            AddProduct(ids[counter]);
        }
    }
    //AddProduct("Basket");
}

function RemoveBasket() {
    for (counter = 0; counter < totalProducts; counter++) {
        if (ids[counter] == 3) {
            RemoveProduct(ids[counter]);
            RemoveProduct(ids[counter]);
            RemoveProduct(ids[counter]);
            RemoveProduct(ids[counter]);
            RemoveProduct(ids[counter]);
            RemoveProduct(ids[counter]);
        }
        else {
            RemoveProduct(ids[counter]);
        }
    }
    RemoveProduct("Basket");
}

function AddBasketTen() {
    for (var i = 0; i < 10; i++) {
        Basket();
    }
}