/*!
 *城品惠pc端首页脚本
 * Requires jQuery v1.7 or later
 * Created by liu mixoaxin on 2016-08-01
 */
; +(function ($, doc) {
    var index = function (errPic) {
        $(function () {
            //主页轮播
            var mySwiper = new Swiper('.swiper-container', {
                pagination: '.pagination',
                paginationClickable: true,
                moveStartThreshold: 100,
                autoplay: 2000,
                loop: true,
                speed: 600,
            }),
            arrawLeft = $('a.arrow-left'),
            arrawRight = $('.arrow-right');

            arrawLeft.click(function (e) {
                e.preventDefault();
                mySwiper.swipePrev();
            }).css("display", "none");

            arrawRight.click(function (e) {
                e.preventDefault();
                mySwiper.swipeNext();
            }).css("display", "none");

            //轮播鼠标悬停
            $("div.swiper-container").hover(
                function () {
                    mySwiper.stopAutoplay();
                    arrawRight.css({ "display": "block", "color": "#fff" });
                    arrawLeft.css({ "display": "block", "color": "#fff" });

                }, function () {
                    mySwiper.startAutoplay();
                    arrawLeft.css("display", "none");
                    arrawRight.css("display", "none");
                }
            );

            $("img.jsProductImg").scaling(150, 150, errPic);
            //$(".load-img2").scaling(150, 150, errPic);
            //$(".load-img").scaling(130, 130, errPic);
            $("img[data-original]").lazyload();

            var AllHet = $(window).height();
            var mainHet = $('.floatCtro').height();
            var fixedTop = (AllHet - mainHet) / 2
            $('div.floatCtro p').click(function () {
                var ind = $('div.floatCtro p').index(this) + 1;
                var topVal = $('#float' + ind).offset().top;
                $('body,html').animate({ scrollTop: topVal }, 1000)
            })
            $('div.floatCtro a').click(function () {
                $('body,html').animate({ scrollTop: 0 }, 1000)
            });

            $('div.centerHome_classifyzhongT a').mouseover(function () {
                $(this).find(".goodsMsHover").stop().animate({ "height": "80px" }, 200);
            }).mouseout(function () {
                $(this).find(".goodsMsHover").stop().animate({ "height": "0px" }, 200);
            });

            var t = new Toolbar({
                onMoveComplete: function (shopCart) {
                    setTimeout(function () {
                        shopCart.flowDrop.show();
                    }, 1000);
                    setTimeout(function () {
                        shopCart.flowDrop.hide();
                    }, 3000)
                },
                cartInstance: new Cnaws.Cart({
                    getPaymentUrl: function () {
                        return Cnaws.getUrl('/buy/perfect/' + orderId);
                    }
                })
            });
        });
    };
    window.index = index;
})($, document);