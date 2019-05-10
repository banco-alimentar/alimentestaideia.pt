(function ($) {
    var methods = {
        init: function (options) {
            methods.initWithoutShow.apply(this, arguments);
            methods.show.apply(this, arguments);
        },
        initWithoutShow: function (options) {
            var defaults = {
                message: "no message given",
                position: 'top',
                backgroundColor: '#8F8F8F',
                borderColor: '7992B0',
                textColor: '#fff',
                $closeElement: $('<span style="float: right; cursor: pointer">(X)</span>')
            };

            var options = $.extend(defaults, options);

            return this.each(function () {
                var o = options;
                var obj = $(this);
                var msg = o.message;
                var obj_position = obj.position();
                var containerDiv = $('<div class="callout">').append(o.$closeElement).append('<b class="notch"></b>' + msg);
                o.$closeElement.click(function () { methods.hide.apply(obj) });
                containerDiv.css({
                    'background-color': o.backgroundColor,
                    'border-color': o.borderColor,
                    'color': o.textColor
                });

                //var items = $("li", obj);
                switch (o.position) {
                    case "bottom":
                        obj.after(containerDiv);
                        var setLeft = obj_position.left + (obj.width() / 2) - (containerDiv.width() / 2) - 5;
                        var setTop = obj_position.top + obj.height();
                        containerDiv.css({
                            'left': setLeft,
                            'top': setTop
                        });
                        $(".notch", containerDiv).css({
                            'left': (containerDiv.width() / 2) - 5,
                            'border-bottom-color': o.backgroundColor
                        });
                        break;
                    case "right":
                        obj.after(containerDiv);
                        $("b.notch", containerDiv).addClass('notch_right').removeClass('notch');
                        var setLeft = obj_position.left + obj.width() + 15;
                        var setTop = obj_position.top + (obj.height() / 2) - (containerDiv.height() / 2) - 10;
                        containerDiv.css({
                            'left': setLeft,
                            'top': setTop,
                            'margin': '0'
                        });
                        $(".notch_right", containerDiv).css({
                            'left': '-10px',
                            'top': (containerDiv.height() / 2) - 5,
                            'border-right-color': o.backgroundColor
                        });
                        break;
                    case "top":
                        obj.before(containerDiv);
                        $("b.notch", containerDiv).addClass('notch_top').removeClass('notch');
                        var setLeft = obj_position.left + (obj.width() / 2) - (containerDiv.width() / 2) - 10;
                        var setTop = obj_position.top - (containerDiv.height()) - 45;
                        containerDiv.css({
                            'left': setLeft,
                            'top': setTop
                        });
                        $(".notch_top", containerDiv).css({
                            'left': (containerDiv.width() / 2) - 5,
                            'top': containerDiv.height() + 10,
                            'border-top-color': o.backgroundColor
                        });
                        break;
                    case "left":
                        obj.before(containerDiv);
                        $("b.notch", containerDiv).addClass('notch_left').removeClass('notch');
                        var setLeft = obj_position.left - containerDiv.width() - 25;
                        var setTop = obj_position.top + (obj.height() / 2) - (containerDiv.height() / 2) - 20;
                        containerDiv.css({
                            'left': setLeft,
                            'top': setTop
                        });
                        $(".notch_left", containerDiv).css({
                            'right': '-10px',
                            'top': (containerDiv.height() / 2) - 5,
                            'border-left-color': o.backgroundColor
                        });
                        break;
                }
                containerDiv.hide();
            });
        },
        hide: function () {
            return this.each(function () {
                $(this).parent().children('.callout').fadeOut(2000);
            })
        },
        show: function () {
            return this.each(function () {
                $(this).parent().children('.callout').fadeIn(2000);
            })
        },
        update: function (content) { }
    };

    $.fn.jCallout = function (method) {

        // Method calling logic
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        } else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        } else {
            $.error('Method ' + method + ' does not exist on jQuery.jCallout');
        }

    };

})(jQuery);