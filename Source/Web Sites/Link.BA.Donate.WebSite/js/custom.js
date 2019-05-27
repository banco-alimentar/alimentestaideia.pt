$( document ).ready(function() {
	$(".langSelector:not(.open)").hover(
		function() {
	    	$( this ).attr( "src", "../img/lingua_hover.png" );
		}, function() {
	    	$( this ).attr( "src", "../img/lingua.png" );
		}
	);
	
	$("body").on("click", ".langSelector:not(.open)", function(event) {
		$( this ).addClass('.open').attr( "src", "../img/lingua_click.png" );
	});
	
	$("body").on("click", ".langSelector.open", function(event) {
		$( this ).removeClass('.open').attr( "src", "../img/lingua_hover.png" );
	});
	
	$("body").on("click", ".text9", function(event) {
		$('.stepOne').hide();
		$('.stepTwo').fadeIn();
	});
	
	$("body").on("click", "span.text3", function(event) {
		$('.modal').fadeIn();
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
			var thisValue = parseFloat( $( this ).parent().find('input').attr('data-value') );
			var newTotal = formatCoin( total + thisValue );
			$('.text8').html( newTotal );
			var thisCart = '.'+$( this ).parent().find('input').attr('data-target');
			$(thisCart).html( value );
			$( this ).parent().find('input').addClass("positive");
		} else {
			$( this ).parent().find('input').removeClass("positive");
		}
		
		$( this ).parent().find('input').val( value );
	});
	
	$("body").on("click", ".less", function(event) {
		var value = parseInt( $( this ).parent().find('input').val() );
		value = value - 1;

		// update totals
		if(parseInt(value) >= 0){
			var total = parseFloat( $('.text8').html() );
			var thisValue = parseFloat( $( this ).parent().find('input').attr('data-value') );
			var newTotal = formatCoin( total - thisValue );
			$('.text8').html( newTotal );
			var thisCart = '.'+$( this ).parent().find('input').attr('data-target');
			$(thisCart).html( value );
			$( this ).parent().find('input').addClass("positive");
		} 
		if(parseInt(value) <= 0){
			$( this ).parent().find('input').removeClass("positive");
		}
		
		if(parseInt(value) < 0){ value = 0; }
		$( this ).parent().find('input').val( value );		
	});
	
	
	
	
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