/*!
 * 加盟商供应商共用的平账模块
 * Requires jQuery v1.1 or later
 * Created by liu mixoaxin on 2016-06-29
 */
+(function (doc, $) {
    "use strict";
    if (typeof $ !== 'function') {
        throw Error('没有引用jQuery');
        return;
    }

    function Pz(getMaxMoneyUrl, pzUrl, pzForm) {
        //获取表单元素
        var get = function (id) {
            return doc.getElementById(id);
        },
        //页面元素
        ui = {
            pzContainer: $('#pzContainer'),
            btnPz: $('input.js-pz'),
            pzForm: $(pzForm),
            pzDMoney: get('pzDMoney'),
            pzBankName: get('pzBankName'),
            pzBankCard: get('pzBankCard'),
            btnOk: get('btnOk'),
            btnCancel: get('btnCancel'),
            maxDMoney: get('maxDMoney')
        },
        //请求地址
        url = '',
        //防止重复提交
        isPosting = false,
        //初始化弱窗
         init = function () {
             ui.pzContainer.jqxWindow({
                 width: '450px',
                 height: '250px',
                 autoOpen: false,
                 resizable: false,
                 draggable: false,
                 isModal: true,
                 modalOpacity: 0.3,
                 cancelButton: ui.btnCancel,
                 initContent: function () {
                     ui.btnOk.focus();
                 }
             });
         },
        //获取最大可提现金额
        getArrivalMoney = function (callback) {
            url = getMaxMoneyUrl + getUserId() + Url_Ext;
            postAjax(url, {}, function (data) {
                if (data.code === -200) {
                    typeof callback == 'function' && callback(data.data);
                }
            }, null);
        },
        //获取用户Id
        getUserId = function () {
            return sessionStorage.getItem('USERID');
        },
        //清空表单
        clearForm = function () {
            ui.pzDMoney.value =
            ui.pzBankName.value =
            ui.pzBankCard.value = '';
        },
        //平账请求
        pingZhang = function () {
            if (!isPosting) {
                postAjax(pzUrl, {
                    DrawMoney: ui.pzDMoney.value,
                    UserId: getUserId(),
                    BankName: ui.pzBankName.value,
                    BankCard: ui.pzBankCard.value
                }, function (data) {
                    isPosting = false;
                    if (data.code === -200) {
                        showNotify('success', '平账成功！');
                        ui.pzContainer.jqxWindow('close');
                    }
                    else {
                        showNotify('error', data.data);
                    }
                }, null);
            }
        };
        //调用初始
        init();
        //绑定确定事件
        ui.btnOk.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();

            if (ui.pzForm.jqxValidator('validate')) {
                pingZhang();
            }
        }, false);
        //平账按钮事件
        ui.btnPz.on('click', function () {
            //把用户Id保存起来，方便下一步操作
            sessionStorage.setItem("USERID", $(this).data('userid'));
            ui.pzContainer.jqxWindow('open');
            getArrivalMoney(function (maxDMoney) {
                ui.pzForm.jqxValidator({
                    rules: [{ input: '#pzDMoney', message: '请输入平账金额！', action: 'blur,keyup', rule: 'required' },
                        {
                            input: '#pzDMoney', message: '平账金额无效！', action: 'blur,keyup', rule: function (input) {
                                return input.val() <= maxDMoney && input.val() > 0;
                            }
                        },
                          { input: '#pzBankName', message: '请输入银行名称！', action: 'blur,keyup', rule: 'required' },
                        { input: '#pzBankCard', message: '请输入银行卡号！', action: 'blur,keyup', rule: 'required' }
                    ]
                });
                ui.maxDMoney.innerText = '￥' + maxDMoney;
                clearForm();
            });
        });
    }

    window.PingZhang = Pz;
})(document, $);