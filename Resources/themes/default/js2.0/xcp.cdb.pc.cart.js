/*!
 *pc端购物车页面操作脚本
 * Requires jQuery v1.7 or later
 * Created by liu mixoaxin on 2016-07-15
 */
+(function (doc, $) {
    "use strict";
    //获取表单元素
    var get = function (id) {
        return doc.getElementById(id);
    },
     Cart = function (options) {
         //默认参数
         var defaults = {
             cartInstance: new Cnaws.Cart(),
             totalMoney: get('totalMoney'),
             btnClear: $('span.clear_cart'),
             btnSubmit: get('btnSubmit'),
             btnCancel: get('btnCancel'),
             btnOk: get('btnOk'),
             btnClose: get('btnClose'),
             dels: $('a.js-del'),
             checks: $('input[name="Id"]'),
             chkAll: $('#chkAll'),
             form: get('form')
         };

         options = $.extend({}, defaults, options || {});

         //获取选中的id集合
         var getSelectedIdAndCount = function () {
             var cartIds = [], productIds = [], counts = [];
             $('input:[name="Id"]').each(function (index, item) {
                 if (item.checked) {
                     cartIds.push(item.getAttribute('data-id'));
                     productIds.push(item.value);
                     var count = $(item).parents("li").find('input[name="Count"]').val();
                     counts.push(count);
                 }
             });
             return {
                 cartId: cartIds.join(','),
                 productId: productIds.join(','),
                 count: counts.join(',')
             };
         },
         //计算店铺最小价格
         checkStoreMinPrice = function (options) {
             var check = true;
             $(options.form).find('li').each(function (e) {
                 if (e >= 1) {                    
                     var items = $(this).find('.goods_checkbox:has(input:[checked])').parent(), totalCount = 0, totalMoney = 0.0;
                     items.each(function (index, item) {
                         var $item = $(item),
                             count = $item.find('.js-number').val(),
                             price = $item.find('.pricBig').text().replace('¥', '').replace(',', '');
                         totalCount += count;
                         totalMoney += parseFloat(price) * count;
                     });                     
                     if (items.length > 0 && totalMoney < parseFloat($(this).data("minprice"))) {
                         console.log(items.length);
                         check = false;
                     }
                 }
             })
             return check;
         },
            //计算单品总价
            calcItem = function (options) {
                var price = options.container.prev().find('.pricBig').html().replace('¥', '').replace(',', ''),
               count = options.number.value;
                options.container.next().find('.fb').text('¥' + (parseFloat(price) * count).toFixed(2))
            },
            //计算供应商总价
            calcSupplier = function (options) {
                var supplierId = options.container.parent().data("supplier");
                var li=options.container.parent().parent();
                var items = li.find('.supplier_' + supplierId), totalCount = 0, totalMoney = 0.0;
                items.each(function (index, item) {
                    var $item = $(item),
                        count = $item.find('.js-number').val(),
                        price = $item.find('.pricBig').text().replace('¥', '').replace(',', '');
                    totalCount += count;
                    totalMoney += parseFloat(price) * count;
                });

                if (totalMoney >= parseFloat(li.data("minprice"))) {
                    li.attr("style", "");
                    li.find('input[name="Id"]').prop("disabled", false);
                    li.find('#minorderprice_' + supplierId).hide();
                    calcTotal();
                }
                else {
                    console.log(li);
                    li.attr("style", "background:#e5e5e5");
                    li.find('input[name="Id"]').prop("disabled", true).prop("checked", false);
                    li.find('#minorderprice_' + supplierId).show();
                }

                li.find(".supplierTotal_" + supplierId).text('¥' + parseFloat(totalMoney).toFixed(2))
            },
            //计算总价
            calcTotal = function () {
                var items = $('li.goods_wrap:has(input:[checked])'), totalCount = 0, totalMoney = 0.0;
                items.each(function (index, item) {
                    var $item = $(item),
                        count = $item.find('.js-number').val(),
                        price = $item.find('.pricBig').text().replace('¥', '').replace(',', '');
                    totalCount += count;
                    totalMoney += parseFloat(price) * count;
                });
                options.totalMoney.innerText = '¥' + totalMoney.toFixed(2);
            };
         //加减按扭
         $('div.item_number').plusMinusBtn({
             plus: 'span.next',//加
             num: '.js-number',//文本框
             minus: 'span.prev',//减
             onChange: function (options) {
                 calcItem(options);
                 calcSupplier(options);
                 calcTotal();
             }
         });
         //删除单个商品
         $('a.js-del').xalert({
             tmpl: '确定要删除此商品吗？',
             callback: function (xalert) {
                 var id = $(xalert.target).parents('li').find('input[name="Id"]').data('id');
                 options.cartInstance.del(id, function () {
                     id = typeof id !== 'string' ? id.toString().split(',') : id.split(',');
                     for (var i = 0; i < id.length; i++) {
                         var divInput = $("input:[data-id=" + id[i] + "]").parents('div#Cart_' + id[i]);                      
                         if (parseInt(divInput.parents('li').children(".supplier_" + divInput.data("supplier") + "").length) <= 1) {
                             $("input:[data-id=" + id[i] + "]").parent().parents('li').remove();
                         }
                         $("input:[data-id=" + id[i] + "]").parents('div#Cart_' + id[i]).remove();
                     }
                     calcSupplier(options.container)
                     calcTotal();
                     Cnaws.showSuccess('删除成功');                     
                 });
                 xalert.close();
                
             }
         });
         //清空购物车选中的商品
         options.btnClear.xalert({
             tmpl: '确定要清空购物车吗',
             callback: function (xalert) {
                 var id = getSelectedIdAndCount().cartId;
                 if (id !== '') {
                     options.cartInstance.del(id, function () {
                         $('li.goods_wrap').remove();
                         xalert.close();
                         options.totalMoney.innerText = '¥0.00'
                     });
                 }
             }
         });

         //选择框
         options.checks.change(function () {
             calcTotal();
         });
         //全选
         options.chkAll.change(function () {
             var chkall = this;
             $('.goods_wrap input[name="Id"]').each(function (e) {
                 if (!$(this).prop("disabled"))
                     $(this).prop('checked', chkall.checked);
             });
             calcTotal();
         });
         options.btnSubmit.onclick = function (e) {
             e.preventDefault();
             e.stopPropagation();

             if ($('li.goods_wrap:has(input:checked)').length == 0) {
                 Cnaws.showError('请选择商品！');
             }
             if (!checkStoreMinPrice(options)) {
                 Cnaws.showError('抱歉，您购物车中的部分店铺不足最少下单量,请检查后再结算！');
             }
             else {
                 Cnaws.showInfo('正在提交数据，请稍后...')
                 var data = getSelectedIdAndCount();
                 options.cartInstance.buy(data.productId, data.count);
             }
         }
     }

    window.Cart = Cart;
})(document, $);