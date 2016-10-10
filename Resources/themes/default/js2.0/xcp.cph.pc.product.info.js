/*!
 *pc端产品详情页面操作脚本
 * Requires jQuery v1.7 or later
 * Created by liu mixoaxin on 2016-08-10
 */
; +(function ($, doc) {
    "use strict";

    var productInfo = function (options) {
        var rqAll = false,
          rqStar1 = false,
          rqStar2 = false,
          rqStar3 = false,
          rqStar4 = false,
          state = 0,
          star1 = 0,
          star2 = 0,
          index = 0,
        getCommentRate = function () {
            var url = Cnaws.getUrl('/comment/Statistics/1/' + options.productId);
            $.get(url, function (result) {
                if (result.code === -200) {
                    $('span.jsRate').text(result.data.rate3);

                    $('span.jsRate1').text(result.data.rate1);
                    $('span.jsRate2').text(result.data.rate2);
                    $('span.jsRate3').text(result.data.rate3);

                    $('#rate1').css('width', result.data.rate1 + '%');
                    $('#rate2').css('width', result.data.rate2 + '%');
                    $('#rate3').css('width', result.data.rate3 + '%');

                    $('em.jsStar1').text(result.data.star1);
                    $('em.jsStar2').text(result.data.star2);
                    $('em.jsStar3').text(result.data.star3);
                    $('em.jsStar4').text(result.data.star4);
                }
            }, 'json');
        },
        getCommentCount = function () {
            var url = Cnaws.getUrl('/comment/count/1/' + options.productId);
            $.get(url, function (result) {
                if (result.code === -200) {
                    $('.jsCommentCount').text(result.data);
                }
            }, 'json');
        },
        getCommentList = function (page, callback) {
            var url = Cnaws.getUrl('/comment/list/1/'
                + options.productId + '/'
                + state + '/'
                + star1 + '/'
                + star2 + '/'
                + page);

            $.get(url, function (html) {
                $('#comment' + index).empty().html(html);
                $.isFunction(callback) && callback();
            }, 'html');
        };

        window.getComment = function (page) {
            Cnaws.showInfo('请稍后...');
            getCommentList(page);
        };

        $('#plusBtnContainer').plusMinusBtn({
            max: options.inventory,
            plus: '#btnPlus',
            num: '#number',
            minus: '#btnMinus',
            onChange: function (pBtn) {
                $('a.btnCart').data('count', pBtn.number.value);
            }
        });

        $('#btnBuy').click(function () {
            var cartInstance = new Cnaws.Cart(),
                id = $('input[name="Id"]').val(),
                count = $('input[name="Count"]').val();

            cartInstance.buy(id, count)
        });

        $('#mynav').navfix(0, 1025);

        $("ul.ul-tab").find("li").each(function () {
            $(this).mouseover(function () {
                $(this).css("color", "#e31939");
            }).mouseout(function () {
                $(this).css("color", "");
            }).click(function () {
                var seft = $(this);
                index = seft.parent().find("li").index(this);

                seft.removeClass().addClass("current");
                var tab = seft.parent().parent().parent().find(".tab-con>#comment" + index);
                tab.show();
                tab.siblings().hide();
                seft.siblings().removeClass();
                switch (index) {
                    case 0:
                        state = 0;
                        star1 = 0,
                        star2 = 0;
                        if (!rqAll) {
                            Cnaws.showInfo('请稍后...');
                            getCommentList(1, function () {
                                rqAll = true;
                            });
                        }
                        break;
                    case 1:
                        state = 2;
                        star1 = 0,
                        star2 = 0;
                        if (!rqStar4) {
                            Cnaws.showInfo('请稍后...');
                            rqStar4 || getCommentList(1, function () {
                                rqStar4 = true;
                            });
                        }
                        break;
                    case 2:
                        state = 1;
                        star1 = 4,
                        star2 = 6;
                        if (!rqStar3) {
                            Cnaws.showInfo('请稍后...');
                            rqStar3 || getCommentList(1, function () {
                                rqStar3 = true;
                            });
                        }
                        break;
                    case 3:
                        state = 1;
                        star1 = 2,
                      star2 = 4;
                        if (!rqStar2) {
                            Cnaws.showInfo('请稍后...');
                            rqStar2 || getCommentList(1, function () {
                                rqStar2 = true;
                            });
                        }
                        break;
                    case 4:
                        state = 1;
                        star1 = 0,
                    star2 = 2;
                        if (!rqStar1) {
                            Cnaws.showInfo('请稍后...');
                            rqStar1 || getCommentList(1, function () {
                                rqStar1 = true;
                            });
                        }
                        break;
                    default:
                        break;
                }
            });
        });

        $(".yScrollListTitle h1").click(function () {
            var index = $(this).index(".yScrollListTitle h1");
            $(this).addClass("yth1click").siblings().removeClass("yth1click");
            $($(".yScrollListInList")[index]).show().siblings().hide();
        });

        getCommentCount();
        getCommentRate();
        getCommentList(1, function () {
            rqAll = true;
        });
    };

    window.product = productInfo;
})($, document);