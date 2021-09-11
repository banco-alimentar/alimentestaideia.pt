$(document).ready(function () {

    $("body").on("click", "#pagamentomb", function (event) {
        $('#pagamentomb').stop().animate({
            opacity: 1
        }, 500);
        $('#pagamentounicre,#pagamentopaypal,#pagamentombway').stop().animate({
            opacity: 0.5
        }, 500);
        $('.pay0').hide();
        $('.pay1').fadeIn();
        $('.pay2').hide();
        $('.pay3').hide();
        $('.pay4').hide();
    });
    $("body").on("click", "#pagamentounicre", function (event) {
        $('#pagamentounicre').stop().animate({
            opacity: 1
        }, 500);
        $('#pagamentomb,#pagamentopaypal,#pagamentombway').stop().animate({
            opacity: 0.5
        }, 500);
        $('.pay0').hide();
        $('.pay1').hide();
        $('.pay2').fadeIn();
        $('.pay3').hide();
        $('.pay4').hide();
    });
    $("body").on("click", "#pagamentopaypal", function (event) {
        $('#frmPayPal').submit();
        //$('#pagamentopaypal').stop().animate({
        //    opacity: 1
        //}, 500);
        //$('#pagamentomb,#pagamentounicre,#pagamentombway').stop().animate({
        //    opacity: 0.5
        //}, 500);
        //$('.pay0').hide();
        //$('.pay1').hide();
        //$('.pay2').hide();
        //$('.pay3').fadeIn();
        //$('.pay4').hide();

        
    });
    $("body").on("click", "#pagamentombway", function (event) {
        $('#pagamentombway').stop().animate({
            opacity: 1
        }, 500);
        $('#pagamentomb,#pagamentounicre,#pagamentopaypal').stop().animate({
            opacity: 0.5
        }, 500);
        $('.pay0').hide();
        $('.pay1').hide();
        $('.pay2').hide();
        $('.pay3').hide();
        $('.pay4').fadeIn();
    });
});