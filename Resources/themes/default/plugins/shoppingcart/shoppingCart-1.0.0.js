/*!
 * 购物车增删改查的公共方法，适用于pc和手机端
 *  Requires cnaws.js and jQuery or Zepto
 * @version 1.0.0
 * @author liu mixoaxin  2016-07-25
 */
+(function (doc, $) {
    "use strict";

    var cnaws = Cnaws || {};
    cnaws.Cart = function (options) {
        var defaults = {
            addUrl: Cnaws.getUrl('/cart/add'),//加入购物车地址
            loginUrl: Cnaws.getPassUrl('/login'),//跳转登录地址
            getCountUrl: Cnaws.getUrl('/cart/count'),//获取购物车产品数量地址
            delUrl: Cnaws.getUrl('/cart/del'),//删除购物车地址
            getListUrl: Cnaws.getUrl('/cart/getList'),
            buyUrl: Cnaws.getUrl('/buy'),
            getPaymentUrl: function (orderId) {
                return Cnaws.getPassUrl('/buy/perfect/' + orderId);
            }
        },
        _self = this;

        this.options = $.extend({}, defaults, options);
        /**
        * 加入购物车
         * @param {Int} productId 产品Id
         * @param {Int} count 购买数量
         * @param {Function} callback 请求成功后的回调，默认的逻辑执行完之后会执行回调
         * @return {Null} 无返回值
         */
        this.add = function (id, count, callback) {
            $.post(this.options.addUrl, { id: id, count: count }, function (data) {
                if (data.code === -200) {
                    Cnaws.showSuccess('添加成功');
                    typeof callback === 'function' && callback(data);
                }
                else if (data.code === -401) {
                    Cnaws.showError('请先登录');
                    location.href = _self.options.loginUrl;
                    return;
                }
                else if (data.code === -1023) {
                    Cnaws.showError('找不到该商品');
                }
                else if (data.code === -1027) {
                    Cnaws.showError('库存不足');
                }
                else {
                    Cnaws.showError('已添加过该宝贝');
                }
            }, 'json');
        };
        /**
        * 获取购物车产品数量
        * @param {String,Function} param 如果是html选择器将直接把数量显示在页面上，回调函数会把参数传入
        * @return {Null} 无返回值
        */
        this.getCount = function (param) {
            $.get(_self.options.getCountUrl, function (data) {
                typeof param === 'string' && $(param).html(data.data);
                typeof param === 'function' && param(data);
            }, 'json');
        };
        /**
         * 删除购物车内商品
         * @param {String} id 以“,”隔开的Cartid，支持单个或者批量删除
         * @param {Function} callback 成功后的回调函数
         * @return {Null}
         */
        this.del = function (id, callback) {
            $.post(_self.options.delUrl, { id: id }, function (data) {
                if (data.code === -200) {
                    typeof callback === 'function' && callback(data);
                }
                else {
                    ShowBox.showErrorByCode(data.code);
                }
            }, 'json');
        };
        /**
         * 获取购物车列表
         * @param {Function} callback 成功后的回调函数
         * @return {Null} 无返回值
         */
        this.getList = function (callback) {
            $.get(_self.options.getListUrl, function (data) {
                if (data.code === -200) {
                    typeof callback === 'function' && callback(data);
                }
                else {
                    ShowBox.showErrorByCode(data.code);
                }
            }, 'json');
        };
        /**
         * 获取购物车列表
         * @param {Int} id 产品Id
         * @param {Int} count 购买数量
         * @param {Function} callback 请求成功后的回调
         * @return {Null} 无返回值
         */
        this.buy = function (id, count, callback) {
            $.post(_self.options.buyUrl, { Id: id, Count: count }, function (data) {
                if (data.code === -200) {
                    if (typeof callback === 'function')
                        callback(data);
                    else
                        location.href = _self.options.getPaymentUrl(data.data.OrderId);
                }
                else {
                    ShowBox.showErrorByCode(data.code);
                }
            }, 'json');
        };
    };

    window.Cnaws = cnaws;
})(document, $ || Zepto);
