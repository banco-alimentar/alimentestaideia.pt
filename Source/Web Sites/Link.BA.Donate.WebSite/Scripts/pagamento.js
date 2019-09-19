$(document).ready(function() {

  $("body").on("click", ".half1pagamento .half1", function(event) {
    $('.half1.half1pagamento').stop().animate({
      opacity: 1
    }, 500);
    $('.half2.half2pagamento').stop().animate({
      opacity: 0.5
    }, 500);
    $('.pay0').hide();
    $('.pay1').fadeIn();
    $('.pay2').hide();
  });
  $("body").on("click", ".half2pagamento .half2", function(event) {
    $('.half2.half2pagamento').stop().animate({
      opacity: 1
    }, 500);
    $('.half1.half1pagamento').stop().animate({
      opacity: 0.5
    }, 500);
    $('.pay0').hide();
    $('.pay1').hide();
    $('.pay2').fadeIn();
  });

});