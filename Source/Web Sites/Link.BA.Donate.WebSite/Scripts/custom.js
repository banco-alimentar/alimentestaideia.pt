$.validator.setDefaults({ ignore: null });

$(document).ready(function () {
	$(".langSelector:not(.open)").hover(
		function() {
            $(this).attr("src", "../Content/img/lingua_hover.png" );
		}, function() {
            $(this).attr("src", "../Content/img/lingua.png" );
		}
	);
	
	$("body").on("click", ".langSelector:not(.open)", function(event) {
		$( this ).addClass('.open').attr( "src", "../Content/img/lingua_click.png" );
	});
	
	$("body").on("click", ".langSelector.open", function(event) {
        $(this).removeClass('.open').attr("src", "../Content/img/lingua_hover.png" );
	});
	
	$("body").on("click", ".text9", function(event) {
		$('.stepOne').hide();
		$('.stepTwo').fadeIn();
	});
	
    $("body").on("click", "#donation-button", function (event) {
        fbq('track', 'submitApplication');
		$('#donation-form').fadeIn();
		$('body').addClass('still');
	});
	$("body").on("click", ".close", function(event) {
		$('.modal').fadeOut();
		$('body').removeClass('still');
	});
	
	$("body").on("click", ".close2", function(event) {
		$('.stepOne').fadeIn();
		$('.stepTwo').hide();
	});
	
	$("body").on("click", ".more", function(event) {
		var value = parseInt( $( this ).parent().find('input').val() );
        value = value + 1;
		
		// update totals
		if(value > 0){
			var total = parseFloat( $('.text8').html() );
            var thisValue = parseFloat($(this).parent().find('input').attr('data-value'));
            var thisQuantity = parseFloat($(this).parent().find('input').attr('data-quantity'));
			var newTotal =  total + thisValue ;
            $('.text8').html(formatCoin(newTotal));
            $('#Amount').val(newTotal);
            var thisCart = '.' + $(this).parent().find('input').attr('data-target');
            $(thisCart).html((value * thisQuantity).toFixed(2));
			$( this ).parent().find('input').addClass("positive");
		} else {
			$( this ).parent().find('input').removeClass("positive");
        }
		
        $(this).parent().find('input').val(value);
	});
	
	$("body").on("click", ".less", function(event) {
		var value = parseInt( $( this ).parent().find('input').val() );
		value = value - 1;

		// update totals
		if(parseInt(value) >= 0){
			var total = parseFloat( $('.text8').html() );
            var thisValue = parseFloat($(this).parent().find('input').attr('data-value'));
            var thisQuantity = parseFloat($(this).parent().find('input').attr('data-quantity'));
			var newTotal =  total - thisValue ;
            $('.text8').html(formatCoin(newTotal));
            $('#Amount').val(newTotal);
			var thisCart = '.'+$( this ).parent().find('input').attr('data-target');
            $(thisCart).html((value * thisQuantity).toFixed(2));
			$( this ).parent().find('input').addClass("positive");
		} 
		if(parseInt(value) <= 0){
			$( this ).parent().find('input').removeClass("positive");
		}
		
		if(parseInt(value) < 0){ value = 0; }
        $(this).parent().find('input').val(value);
	});
	
    $('body').on('click', '.more,.less', function () {
        var donatedItems = "";

        for (pos = 0; pos < 6; pos++) {
            itemCount = parseInt($('#field' + (pos + 1)).val());
            if (itemCount > 0) {
                donatedItems += (pos + 1) + ":" + $('#field' + (pos + 1)).val() + ";";
            }
        }

        $("#DonatedItems").val(donatedItems);
    });

    $('#Address').val('-');
    $('#PostalCode').val('-');
    $('#Nif').val('000000000');

    $('#WantsReceipt').val('false');
    $('#AcceptsTerms').val('false');

    $('#WantsReceiptCheckBox').click(function () {
        if ($('#WantsReceiptCheckBox').is(':checked')) {
            $('.recibo').show();
            $('#Address').val('');
            $('#PostalCode').val('');
            $('#Nif').val('');
        }
        else {
            $('.recibo').hide();
            $('#Address').val('-');
            $('#PostalCode').val('-');
            $('#Nif').val('000000000');
        }

        $('#WantsReceipt').val($('#WantsReceiptCheckBox').is(':checked'));
    });

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
});

function formatCoin(value){
	value = formatter.format(value);
	value = value.replace(" ","");
	value = value.replace(",",".");
	return value;
}

const formatter = new Intl.NumberFormat('pt-PT', {
	style: 'currency',
	currency: 'EUR',
	minimumFractionDigits: 2
})
