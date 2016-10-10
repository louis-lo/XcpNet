/*!
 * pc端右手边工具条脚本，实现用户登录注册功能，加入购物车，显示购物车产品列表和删除购物车功能
 * Requires cnaws.js，passport.js，parabola.min.js，shoppingCart.js，artTemplete.js and jQuery
 * @version 1.0.0
 * @author liu mixoaxin  2016-07-27
 */
; +(function ($, doc) {
    'use strict';

    var get = function (selector) {
        return doc.querySelector(selector);
    },
    //工具条类
    Toolbar = function (option) {
        var _self = this,
            defaults = {
                cartInstance: new Cnaws.Cart(),
                flyItemSelector: '#flyItem',
                cartCountSelector: 'span.jsCount,div.jsCount',
                btnCartSelector: '.btnCart',
                btnDelCartSelector: 'i.del-btn',
                captchaImgSelector: '#captchaImg',
                captchaLinkSelector: '#captchaLink',
                cartContainerSelector: '#cartInfo',
                loginForm: $('#formLogin'),
                isFly: true,
                //事件
                onMoveComplete: null,
                onAddComplete: null,
                onBeforeAdd: null
            };

        this.options = $.extend({}, defaults, option || {});

        this.passport();
        this.onlineService();
        this.parabola();
        this.shopCartInstance = this.shopCart();
    };
    //登录注册功能模块
    Toolbar.prototype.passport = function () {
        var _self = this,
            getCaptcha = function () {
                Cnaws.changeCaptcha(_self.options.captchaImgSelector, 'login');
            },
            captLink = get(this.options.captchaLinkSelector);

        if (captLink != null) {//说明未登录
            getCaptcha();

            get(this.options.captchaLinkSelector).onclick = function () {
                getCaptcha();
            };

            Cnaws.Passport.Init({
                targetUrl: location.href
            });

            this.options.loginForm.jqxValidator({
                hintType: 'label',
                animationDuration: 0,
                rules: [
                    { input: '#UserName', message: ' ', action: 'keyup, blur', rule: 'required' },
                    { input: '#Password', message: ' ', action: 'keyup, blur', rule: 'required' },
                    { input: '#Captcha', message: ' ', action: 'keyup, blur', rule: 'required' }
                ]
            });

            this.options.loginForm.submit(function (e) {
                e.preventDefault();
                e.stopPropagation();

                if (_self.options.loginForm.jqxValidator('validate')) {
                    Cnaws.Passport.login(this, '#errmsg', _self.options.captchaLinkSelector);
                }
            });

            $('#right_login').mouseover(function () {
                $(this).children(".dropdown").show();
            }).mouseout(function () {
                $(this).children(".dropdown").hide();
            }).mouseleave(function () {
                $(this).children(".dropdown").hide();
            });
        }
    };
    //在线服务
    Toolbar.prototype.onlineService = function () {
        $("div.online-service").mouseover(function () {
            $(this).children(".dropdown").show();
        }).mouseout(function () {
            $(this).children(".dropdown").hide();
        }).mouseleave(function () {
            $(this).children(".dropdown").hide();
        });
    };
    //购物车抛物线功能
    Toolbar.prototype.parabola = function () {
        // 元素以及其他一些变量
        var _self = this,
            eleFlyElement = get(this.options.flyItemSelector),
             eleShopCart = get(this.options.btnCartSelector),
             eleShopCartIcon = get('#shopCart').querySelector('i'),
             numberItem = 0;
        // 抛物线运动
        var myParabola = funParabola(eleFlyElement, eleShopCartIcon, {
            speed: 200, //抛物线速度
            curvature: 0.0008, //控制抛物线弧度
            complete: function () {
                eleFlyElement.style.visibility = "hidden";
            }
        });
        //有可能外部调用
        this.fly = function (event, callback) {
            // 滚动大小
            var scrollLeft = doc.documentElement.scrollLeft || doc.body.scrollLeft || 0,
                scrollTop = doc.documentElement.scrollTop || doc.body.scrollTop || 0;
            eleFlyElement.style.left = event.clientX + scrollLeft + "px";
            eleFlyElement.style.top = event.clientY + scrollTop + "px";
            eleFlyElement.style.visibility = "visible";

            // 需要重定位
            myParabola.position().move();

            if (typeof _self.options.onMoveComplete === 'function') {
                _self.options.onMoveComplete(_self);
            }
        };

        // 绑定点击事件
        $(this.options.btnCartSelector).each(function (index, button) {
            button.addEventListener("click", function (event) {
                var func = function () {
                    var productId = event.target.getAttribute('data-productid'),
                     count = event.target.getAttribute('data-count');

                    _self.options.cartInstance.add(productId, count, function () {
                        _self.shopCartInstance.loadList();
                        if (typeof _self.options.onAddComplete === 'function') {
                            _self.options.onAddComplete(_self, function () {
                                _self.options.isFly && _self.fly(event);
                            });
                        }
                        else {
                            _self.fly(event, _self.options.onMoveComplete)
                        }
                    });
                };

                if (typeof _self.options.onBeforeAdd === 'function') {
                    _self.options.onBeforeAdd(func);
                }
                else {
                    func();
                }

            });
        });
    };
    //购物车模块
    Toolbar.prototype.shopCart = function () {
        var _self = this,

            init = function () {
                if (_self.flowDrop.length > 0) {
                    _self.loadList();
                    _self.cartContainer.mouseover(function () {
                        _self.flowDrop.show();
                    }).mouseout(function () {
                        _self.flowDrop.hide();
                    }).mouseleave(function () {
                        _self.flowDrop.hide();
                    });
                }

                var headHeight2 = 200, //这个高度其实有更好的办法的。使用者根据自己的需要可以手工调整。
                  top = $("#backToTop");       //要悬浮的容器的id
                $(window).scroll(function () {
                    if ($(this).scrollTop() > headHeight2) {
                        top.removeClass("disabled");
                    }
                    else {
                        top.addClass("disabled");
                    }
                });

                top.click(function () {
                    $('body,html').animate({ scrollTop: 0 }, 800);
                    return false;
                });

            },
            bindDel = function () {
                //绑定购物车删除按钮
                $(_self.options.btnDelCartSelector).on('click', function () {
                    var $this = $(this);
                    _self.options.cartInstance.del($this.data('cartid'), function () {
                        $this.parent('li').remove();
                        setCount(-1);
                    });
                });
            },
            setCount = function (count) {
                if (count) {
                    var cartCount = $(_self.options.cartCountSelector);
                    if (count < 0)
                        count = parseInt(cartCount.get(1).innerText.replace('(', '').replace(')', '')) + count;
                    cartCount.html(count);
                }
                else {
                    _self.options.cartInstance.getCount(_self.options.cartCountSelector);
                }
            };
        this.flowDrop = $("#flowDrop");
        this.cartContainer = $(this.options.cartContainerSelector);

        this.loadList = function () {
            _self.options.cartInstance.getList(function (data) {
                var html = template('tmpl-cartList', data),
               containter = _self.cartContainer.find('.bar');
                containter.find('.cart_goods').remove();
                containter.append(html);
                bindDel();
                setCount();
            });
        };
        init();
        return this;
    };
    //提供给外部访问
    window.Toolbar = Toolbar;
})($, document);