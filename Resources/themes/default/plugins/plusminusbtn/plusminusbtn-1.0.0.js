/* !
 * 在产品详情页面或者购物车页面，都需要加减产品数量，本插件专为此应用场景下开发
 * 在多个文本框的页面中，支持计算单品总价，总数量和总金额，显示在界面中
 * Requires jQuery v1.1 or Zepto
 * @version 1.0.1
 * @author liu mixoaxin  2016-06-25
 */
; +(function ($, doc) {
    "use strict";
    var PlusMinusBtn = function (ele, options) {
        var defaults = {
            max: 9999, //最大库存数,如果需要对最大库存数进行验证，请在onMinus事件上写逻辑
            min: 1, //允许购买的最小数量
            price: 0.0,//价格
            plus: '.jsPlus',//加
            num: '.jsNumber',//文本框
            minus: '.jsMinus',//减
            tCount: '.jsTotalCount',//总数量选择器
            tMoney: '.jsTotalMoney',//总金额选择器
            singleMoney: 'jsSingleMoney',//单个商品总价
            //事件
            onCalc: null,//在计算完总数量和总价之后回调，一般情况下是需要把计算好的值进行额外的操作，所以提供此事件
            onPlus: null, //加事件回调
            onMinus: null,//减事件回调
            onChange: null//验证事件
        };
        this.container = $(ele);
        this.options = $.extend({}, defaults, options);
        this.btnMinus = this.container.find(this.options.minus);
        this.number = this.container.find(this.options.num)[0];//javascript对象
        this.btnPlus = this.container.find(this.options.plus);

        this.init();
    };

    PlusMinusBtn.prototype = {
        init: function () {
            this.btnMinus.click($.proxy(this.minus, this));
            this.btnPlus.click($.proxy(this.plus, this));
            this.number.onkeyup = this.number.onafterpaste = $.proxy(this.valid, this);

            var num = $(this.number),
                max = num.data('max'),
                min = num.data('min'),
              price = num.data('price');

            if (max > 0)
                this.options.max = max;
            if (min > 0)
                this.options.min = min;
            if (price > 0)
                this.options.price = price;
        },
        minus: function () {
            if (this.getNumber() > this.options.min) {
                this.number.value = parseInt(this.number.value) - 1;
                this.calcTotal();
                typeof this.options.onChange === 'function' && this.options.onChange(this);
            }
            else {
                Cnaws.showInfo('购买数量最少为' + this.options.min + '件！');
            }
            typeof this.options.onMinus === 'function' && this.options.onMinus(this);
        },
        plus: function () {
            if (this.getNumber() < this.options.max) {
                this.number.value = parseInt(this.number.value) + 1;
                this.calcTotal();
                typeof this.options.onChange === 'function' && this.options.onChange(this);
            }
            else {
                Cnaws.showInfo('购买数量不能大于库存数量！');
            }
            typeof this.options.onPlus === 'function' && this.options.onPlus(this);
        },
        valid: function (e) {
            var v = e.target.value;
            if (v.length === 1) {
                v = v.replace(/[^1-9]/g, '');
            } else {
                v = v.replace(/\D/g, '');
            }
            if (v === '') {
                v = this.options.min;
            } else {
                var num = parseInt(v);
                v = num > this.options.max ?
                    this.options.max : num < this.options.min ?
                    this.options.min : v;
            }
            e.target.value = v;
            this.calcTotal();
            typeof this.options.onChange === 'function' && this.options.onChange(this);
        },
        getNumber: function () {
            return parseInt(this.number.value);
        },
        calcTotal: function () {
            var _self = this;
            this.options.singleTotalMoney = (this.getNumber() * this.options.price).toFixed(2);
            this.options.idArray = [];
            this.options.countArray = [];
            this.options.totalCount = 0;
            this.options.totalMomey = 0.00;

            $(this.options.num).each(function (index, item) {
                var $this = $(item), num = parseInt(this.value);
                _self.options.totalCount += num;
                _self.options.totalMomey += num * parseFloat($this.data('price'));
                if ($this.data('id'))
                    _self.options.idArray.push(parseInt($this.data('id')))
                _self.options.countArray.push(num);
            });

            if (this.options.singleMoney !== '')
                $(this.options.singleMoney).text('¥' + this.options.singleTotalMoney);
            if (this.options.tCount !== '')
                $(this.options.tCount).text('¥' + this.options.totalCount);
            if (this.options.tMoney !== '')
                $(this.options.tMoney).text('¥' + this.options.totalMomey.toFixed(2));

            typeof this.options.onCalc === 'function' && this.options.onCalc(this);
        }
    }

    //如果需要每次都绑定，给isEveryBind传入true
    $.fn.plusMinusBtn = function (option, isEveryBind) {
        this.each(function (index) {
            var data = this.dataset.plusminusbtn,
             options = typeof option === 'object' && option,
             btn;

            if (isEveryBind) {
                btn = new PlusMinusBtn(this, options);
            } else if (!data) {
                btn=this.dataset.plusminusbtn = new PlusMinusBtn(this, options);
            }
            //在页面中有多个加减按钮的情况下，这样写是为了让这个方法只执行一次
            if (index == 0) {
                btn.calcTotal();
            }
        });
    };

    window.PlusMinusBtn = PlusMinusBtn;
})((typeof $ !== 'undefined' && typeof jQuery !== 'undefined') ? $ : Zepto, document)