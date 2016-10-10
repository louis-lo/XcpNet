/*!
 *城品惠pc端首页脚本
 * Requires jQuery v1.7 or later
 * Created by liu mixoaxin on 2016-08-01
 */
; +(function ($, doc) {
    var index = function (errPic) {
        var scrolls = function () {
            var f1, f2, f3, f4, f5, f6, f7, f8, f9, f10, f11, f12, f13, f14, f15, bck;
            var fixRight = $('div.floatCtro p');
            var blackTop = $('div.floatCtro a')
            var sTop = $(window).scrollTop();
            if (sTop > 1630) {
                $('.floatCtro').show();
            } else {
                $('.floatCtro').hide();
            }

            fl = $('#float1').offset().top;
            f2 = $('#float2').offset().top;
            f3 = $('#float3').offset().top;
            f4 = $('#float4').offset().top;
            f5 = $('#float5').offset().top;
            f6 = $('#float6').offset().top;
            f7 = $('#float7').offset().top;
            f8 = $('#float8').offset().top;
            f9 = $('#float9').offset().top;
            f10 = $('#float10').offset().top;
            f11 = $('#float11').offset().top;
            f12 = $('#float12').offset().top;
            f13 = $('#float13').offset().top;
            f14 = $('#float14').offset().top;
            f15 = $('#float15').offset().top;

            if (sTop <= f2 - 100) {
                blackTop.fadeOut(300).css('display', 'none')
            }
            else {
                blackTop.fadeIn(300).css('display', 'block')
            }

            if (sTop >= fl) {
                fixRight.eq(0).addClass('cur').siblings().removeClass('cur');
            }
            if (sTop >= f2 - 100) {
                fixRight.eq(1).addClass('cur').siblings().removeClass('cur');
            }
            if (sTop >= f3 - 100) {
                fixRight.eq(2).addClass('cur').siblings().removeClass('cur');
            }
            if (sTop >= f4 - 100) {
                fixRight.eq(3).addClass('cur').siblings().removeClass('cur');
            }
            if (sTop >= f5 - 100) {
                fixRight.eq(4).addClass('cur').siblings().removeClass('cur');
            }
            if (sTop >= f6 - 100) {
                fixRight.eq(5).addClass('cur').siblings().removeClass('cur');
            }
            if (sTop >= f7 - 100) {
                fixRight.eq(6).addClass('cur').siblings().removeClass('cur');
            }
            if (sTop >= f8 - 100) {
                fixRight.eq(7).addClass('cur').siblings().removeClass('cur');
            }
            if (sTop >= f9 - 100) {
                fixRight.eq(8).addClass('cur').siblings().removeClass('cur');
            }
            if (sTop >= f10 - 100) {
                fixRight.eq(9).addClass('cur').siblings().removeClass('cur');
            }
            if (sTop >= f11 - 100) {
                fixRight.eq(10).addClass('cur').siblings().removeClass('cur');
            }
            if (sTop >= f12 - 100) {
                fixRight.eq(11).addClass('cur').siblings().removeClass('cur');
            }
            if (sTop >= f13 - 100) {
                fixRight.eq(12).addClass('cur').siblings().removeClass('cur');
            }
            if (sTop >= f14 - 100) {
                fixRight.eq(13).addClass('cur').siblings().removeClass('cur');
            }
            if (sTop >= f15 - 100) {
                fixRight.eq(14).addClass('cur').siblings().removeClass('cur');
            }
        }

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
            })
            $(window).scroll(scrolls)
            scrolls()
        });
    };
    window.index = index;
})($, document);