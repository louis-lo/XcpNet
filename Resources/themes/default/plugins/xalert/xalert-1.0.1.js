/*!
 * pc端右手边工具条脚本，实现用户登录注册功能，加入购物车，显示购物车产品列表和删除购物车功能
 * Requires  jQuery 1.1.1 or later
 * @version 1.0.0
 * @author liu mixoaxin  2016-07-27
 *----------------------------------------------
 * @version 1.0.1
 * 1.修复了在一个页面上多个弹窗时，不能独立定制界面的问题
 * 2.增加了手动弹窗模式
 * @author liu mixoaxin  2016-08-15
 */
; +(function ($, doc) {
    "use strict";

    var Xalert = function (ele, params) {
        var _self = this,
            defaults = {
                container: '#xContainer',
                bg: '#xbg',
                content: '#xContent',
                btnOk: '#xbtnOk',
                btnCancel: '#xbtnCancel',
                btnClose: '#xbtnClose',
                width: '58%',
                height: '67%',
                top: '10%',
                left: '23%',
                btnOkTxt: '确认',
                tmpl: '',//必传，可以是文本和html模板Id
                enableClose: true,
                enableCancel: true,
                enableOk: true,
                callback: null,//必传
                onShow: null,//显示之后回调
                onClose: null,//关闭之后回调
            };

        this.options = $.extend({}, defaults, arguments.length === 1 ? arguments[0] : arguments[1]);
        this.target = arguments.length === 1 ? _self : ele;
        this.container = $(this.options.container);
        this.bg = $(this.options.bg);
        this.content = this.container.find(this.options.content);
        this.btnOk = this.container.find(this.options.btnOk);
        this.btnCancel = this.container.find(this.options.btnCancel);
        this.btnClose = this.container.find(this.options.btnClose);

        var initAction = function () {
            if (_self.options.enableClose) {
                _self.btnClose.click(_self.close).show();
            }
            else {
                _self.btnClose.hide();
            }

            if (_self.options.enableCancel) {
                _self.btnCancel.click(_self.close).show();
            }
            else {
                _self.btnCancel.hide();
            }

            if (_self.options.enableOk) {
                _self.btnOk
                    .off('click')//在一个页面上如果有多个弹窗，需要解除上个单击事件
                    .click(function (e) {
                        _self.options.callback(_self, e);
                    })
                    .show();
            }
            else {
                _self.btnOk.hide();
            }
        },
         initContent = function () {
             _self.container.show();
             _self.bg.show();
             _self.bg.width(doc.body.scrollWidth);
             _self.bg.height($(doc).height());
             _self.btnOk.text(_self.options.btnOkTxt);
             if (_self.options.tmpl.indexOf('#') === 0) {
                 _self.content.html($(_self.options.tmpl).html());
                 _self.content.css({ 'text-align': '', 'margin-top': '0' });
                 _self.container.css({ width: _self.options.width, height: _self.options.height, top: _self.options.top, left: _self.options.left });
             }
             else {
                 _self.container.css({ width: '20%', height: '140px', top: '40%', left: '42%' });
                 _self.content.html(_self.options.tmpl).css({ 'text-align': 'center', 'margin-top': '50px' });
             }
         }

        this.show = function () {
            initAction();
            initContent();

            typeof this.options.onShow === 'function' && this.options.onShow(this);
        };

        this.close = function () {
            _self.container.hide();
            _self.bg.hide();
            typeof _self.options.onClose === 'function' && _self.options.onClose(this);
        };

        //自动弹窗模式，绑定事件源
        if (arguments.length === 2 && ele)
            ele.onclick = $.proxy(_self.show, _self);
    };
    //手动弹窗模式的时候使用
    window.Xalert = Xalert;

    $.fn.xalert = function (options) {
        return this.each(function () {
            var data;
            if (!this.dataset) {
                this.dataset = {};
            }
            data = this.dataset.xalert;

            if (!data) this.dataset.xalert = new Xalert(this, options);
        });
    };

})(jQuery || Zepto, document)