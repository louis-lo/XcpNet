/*!
 *pc端确认订单页面操作脚本
 * Requires jQuery v1.7 or later
 * Created by liu mixoaxin on 2016-07-15
 */
+(function (doc, $) {
    "use strict";
    //获取表单元素
    var get = function (id) {
        return doc.getElementById(id);
    },
    stop = function (e) {
        e.preventDefault();
        e.stopPropagation();
    };
    //确认订单页面脚本
    var Buy = function (options) {
        var defaults = {
            cartUrl: Cnaws.getPassUrl("/cart"),
            freightUrl: Cnaws.getPassUrl("/buy/freight"),
            getPaymentUrl: function (orderId) {
                return Cnaws.getPassUrl("/buy/payment/" + orderId);
            },
            delAddressUrl: Cnaws.getPassUrl("/shippingaddress/delete"),
            areaCountyUrl: "/country/",
            setDefaultUrl: Cnaws.getPassUrl("/shippingaddress/setDefault")
        };
        options = $.extend({}, defaults, options || {});

        //页面元素
        var ui = {
            btnSwitchAddress: $('a.jsAdr-switch'),
            defaultAdrs: '#default_A',
            postTo: $('#postTo'),
            addressName: $('#recName'),
            btnBack: get('btnBack'),
            totalMoney: get('TotalMoney'),
            form: get('form'),
            adrsForm: $('#adrsForm'),
            btnSubmit: get('btnSubmit')
        },
         //验证添加地址表单
         validateAdd = function () {
             var mobile = $('#Mobile');
             getAdrsForm().jqxValidator('hide');
             getAdrsForm().jqxValidator({
                 scroll: false,
                 rules: [
                     { input: '#Consignee', message: '收货人不能为空', action: 'keyup, blur', rule: 'required' },
                     { input: '#Address', message: '详细地址不能为空', action: 'keyup, blur', rule: 'required' },
                     { input: '#Mobile', message: '手机号码不能为空', action: 'keyup, blur', rule: 'required' },
                     {
                         input: '#area_counties', message: '县、区不能为空', action: 'keyup, blur', rule: function (input) {
                             var value = input.val();
                             if (value != '') {
                                 return parseInt(value) > 0;
                             }
                         }
                     },
                     {
                         input: '#Mobile', message: '手机号码无效', action: 'keyup, blur', rule: function (input) {
                             var value = input.val();
                             if (value !== '') {
                                 var _emp = /^\s*|\s*$/g;
                                 value = value.replace(_emp, "");
                                 var _d = /^1[3578][01379]\d{8}$/g;//电信
                                 var _l = /^1[34578][01256]\d{8}$/g;//联通
                                 var _y = /^(134[012345678]\d{7}|1[34578][012356789]\d{8})$/g;//移动

                                 if (_d.test(value) || _l.test(value) || _y.test(value)) {
                                     return true;
                                 } else {
                                     return false;
                                 }
                             }
                             else {
                                 return true;
                             }
                         }
                     }
                 ]
             });

             return getAdrsForm().jqxValidator('validate');
         },
         //保存地址
         saveAddress = function () {
             if (validateAdd()) {
                 var form = getAdrsForm();
                 Cnaws.postAjax(form.attr('action'), form.serialize(), function (data, args) {
                     if (data.code == -200) {
                         location.reload();
                     }
                     else {
                         ShowBox.showErrorByCode(data.code);
                     }
                 }, null);
             }
         },
         creatPostMsg = function () {
             var msg = [];
             $('div[data-orderid]').each(function (index, item) {
                 msg.push($(item).data('orderid') + '_' + $(item).find('textarea').val());
             });
             return '@' + msg.join('@');
         }
        ;

        var getAdrsForm = function () {
            if (!ui.adrsForm || ui.adrsForm.length === 0)
                return $('#adrsForm');
            return ui.adrsForm;
        };

        if (get('area') != null)
            Cnaws.Area.Init('area', options.areaCountyUrl, options.location);

        //删除地址
        $("input.jsAdr-Del").click(function (e) {
            stop(e);
            var xlt = new Xalert(this, {
                tmpl: '确定要删除收货地址吗？',
                callback: function (xalert) {
                    var add = $(xalert.target).parent();
                    var addressId = add.data('addressid');
                    Cnaws.postAjax(options.delAddressUrl, { Id: addressId }, function (data, args) {
                        xalert.close();
                        if (data.code == -200) {
                            location.reload();
                        }
                        else {
                            ShowBox.showErrorByCode(data.code);
                        }
                    }, null);
                }
            });
            xlt.show();
        });
        var click = 0;
        //修改地址
        $("input.jsAdr-Edit").click(function (e) {
            stop(e);
            var xlt = new Xalert(this, {
                tmpl: '#tmpl-address',
                height: '380px',
                width: '715px',
                onShow: function (xalert) {
                    var adrs = $(xalert.target).parent(),
                    addressId = adrs.data('addressid');
                    
                    Cnaws.Area.Init('area', options.areaCountyUrl, adrs.data('county'));
                    xalert.content.find('#area').html('');
                    xalert.content.find('input[name="Id"]').val(addressId);
                    xalert.content.find('#Consignee').val(adrs.find('.jsAdr-name').text());
                    xalert.content.find('#Address').val(adrs.data('address'));
                    xalert.content.find('#PostId').val(adrs.data('postid'));
                    //xalert.content.find('#Email').val(adrs.data('email'));
                    xalert.content.find('#Mobile').val(adrs.find('.jsAdr-mobile').text());
                },
                callback: saveAddress
            });
            if (click == 0) {
                xalt.show(); click++;
            }
        });
        //使用新地址弹窗
        $('#btnNewAddress').xalert({
            tmpl: '#tmpl-address',
            height: '380px',
            width: '715px',
            onShow: function () {
                Cnaws.Area.Init('area', options.areaCountyUrl, options.location);
            },
            callback: function (xalert) {
                if (validateAdd()) {
                    saveAddress();
                }
            }
        });
        //地址切换
        ui.btnSwitchAddress.click(function () {
            Cnaws.showInfo('正在切换地址，请稍后...');
        });
        //返回购物车
        ui.btnBack.onclick = function (e) {
            stop(e);

            location.href = options.cartUrl;
        };
        //订单提交支付
        ui.form.onsubmit = function (e) {
            stop(e);
            if (options.hasInvalid === 'True') {
                Cnaws.showWarning('您的订单中含有无效的商品，请返回购物删除再提交！');
                return;
            }
            if (options.adrsId <= 0) {
                Cnaws.showWarning('请先填写地址！');
                return;
            }

            var postData = {
                Id: options.orderId,
                Address: options.adrsId,
                Message: creatPostMsg(),
            };
            $.post(this.action, postData, function (data) {
                if (data.code === -200) {
                    location.href = options.getPaymentUrl(data.data.OrderId);
                }
                else {
                    ShowBox.showErrorByCode(data.code);
                }
            }, 'json');
        }

        if (getAdrsForm().length !== 0) {
            getAdrsForm().submit(function (e) {
                stop(e);
                saveAddress();
            });
        }
    }

    window.Buy = Buy;
})(document, $);