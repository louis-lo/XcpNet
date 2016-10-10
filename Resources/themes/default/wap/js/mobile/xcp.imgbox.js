/**
  *点击图片放大插件
 * Created by liu mixoaxin on 2016-06-17.
 *Requires jQuery v1.1，Zepto.1.1.6 or later
 */
; +(function ($, doc) {
    'use strict'
    if (typeof $ === 'undefined') {
        throw new Error('未引用jquery或者Zepto');
        return;
    }

    var get = function () {
        var obj = doc.getElementById(arguments[0]);
        return obj || document.getElementsByClassName(arguments[0]);
    },
    getbgUrl = function () {
        var bg = arguments[0],
	    start = bg.indexOf('('),
	    end = bg.indexOf(')');
        return bg.substr(start + 2, end - 6);
    },
	 EnlargeImg = function (ele, options) {
	     this.ele = ele;
	     this.getEle();
	     this.init();
	 };

    EnlargeImg.prototype = {
        getEle: function () {
            this.light = get('light');
            this.bigImg = get('bigImg');
            this.fade = get('fade');
        },
        init: function () {
            if (this.light === null || this.light.length === 0) {
                var html = '<div id="light"><img id="bigImg" width="100%" height="100%" /> </div> <div id="fade"></div>';
                $(html).appendTo(doc.body);
                this.getEle();
                this.fade.style.cssText = 'display: none;position: fixed;top: 0%;left: 0%;width:100%;height:100%;background-color:darkgray;z-index:1001;-moz-opacity: 0.8;opacity: .80;filter: alpha(opacity=80);';
                this.light.style.cssText = 'display: none;position: absolute;top: 25%;width: 100%;padding:10px;z-index: 1002;overflow: hidden;background-color: white;';
                $(this.bigImg).on('click', $.proxy(this.hide, this));
            }
            $(this.ele).on('click', $.proxy(this.enlarge, this));
        },
        enlarge: function (e) {
            this.show();
            var bg = e.target.style.background;
            this.bigImg.src = getbgUrl(bg);
        },
        show: function () {
            this.light.style.display = 'block';
            this.fade.style.display = 'block';
        },
        hide: function () {
            this.light.style.display = 'none';
            this.fade.style.display = 'none';
        }
    };

    $.fn.enlargeImg = function (options) {
        return this.each(function () {
            var $this = $(this),
                instance = $this.data('plugin');
            if (!instance) {
                $this.data('plugin', new EnlargeImg(this, options));
            }
            if ($.type(options) === 'string') instance[options]();
        });
    };
})(jQuery || Zepto, document)