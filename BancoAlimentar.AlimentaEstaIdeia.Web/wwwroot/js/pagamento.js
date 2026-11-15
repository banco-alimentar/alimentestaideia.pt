$(document).ready(function () {

    $("body").on("submit", "form.payment-form", function (event) {
        const page = $("body");
        if (page.data("paymentSubmitting")) {
            event.preventDefault();
            return;
        }

        page.data("paymentSubmitting", true);
        $("form.payment-form").each(function () {
            $(this).find("button[type='submit'], input[type='submit']").prop("disabled", true);
        });
        $(".pmethod-container").prop("disabled", true);
    });

    function lockPaymentMethodSelection() {
        $(".pmethod-container").prop("disabled", true);
    }

    $("body").on("click", "#pagamentomb", function (event) {
        lockPaymentMethodSelection();
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
        lockPaymentMethodSelection();
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
        lockPaymentMethodSelection();
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
        lockPaymentMethodSelection();
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
