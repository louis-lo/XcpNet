var ShowBox = {
    ToastByCode: function (code) {
        var msg = '';
        if (code == -500)
            msg = '抱歉,系统出现错误.';
        else if (code == -401)
            msg = '未登录';
        else if (code == -1000)
            msg = '抱歉,手机号码格式错误,请认真核对.'
        else if (code == -1001)
            msg = '抱歉,获取验证码出错,请重新获取验证码.'
        else if (code == -1002)
            msg = '抱歉,验证码错误,请认真核对.'
        else if (code == -1003)
            msg = '抱歉,找不到对应的Mark标识.'
        else if (code == -1004)
            msg = '抱歉,Token为空.'
        else if (code == -1005)
            msg = '抱歉,未找到该用户.'
        else if (code == -1006)
            msg = '抱歉,对应Mark标识错误.'
        else if (code == -1007)
            msg = '抱歉,对应Token标识错误.'
        else if (code == -1008)
            msg = '抱歉,您输入的密码格式不正确.'
        else if (code == -1009)
            msg = '抱歉,该用户未审核.'
        else if (code == -1012)
            msg = '抱歉,操作数据失败.'
        else if (code == -1013)
            msg = '抱歉,短信模块初始化失败.'
        else if (code == -1014)
            msg = '抱歉,生成验证码失败.'
        else if (code == -1015)
            msg = '抱歉,短信接口配置错误.'
        else if (code == -1016)
            msg = '抱歉,短信发送失败.'
        else if (code == -1017)
            msg = '抱歉,必要的参数未被找到.'
        else if (code == -1018)
            msg = '抱歉,新增数据失败.'
        else if (code == -1019)
            msg = '抱歉,修改数据失败.'
        else if (code == -1020)
            msg = '抱歉,删除数据失败.'
        else if (code == -1021)
            msg = '抱歉,系统运行错误.'
        else if (code == -1022)
            msg = '抱歉,该用户已被注册.'
        else if (code == -1023)
            msg = '抱歉,找不到该商品信息.'
        else if (code == -1024)
            msg = '抱歉,购买该商品的数量不能为空.'
        else if (code == -1025)
            msg = '抱歉,商品或数量存在错误.'
        else if (code == -1026)
            msg = '抱歉,该商品信息存在错误.'
        else if (code == -1027)
            msg = '抱歉,产品库存不足.'
        else if (code == -1028)
            msg = '抱歉,创建订单失败.'
        else if (code == -1029)
            msg = '抱歉,创建订单详情出现异常,创建失败.'
        else if (code == -1030)
            msg = '抱歉,找到到该地址.'
        else if (code == -1031)
            msg = '抱歉,找到到该订单.'
        else if (code == -1032)
            msg = '抱歉,更新订单失败.'
        else if (code == -1033)
            msg = '抱歉,添加快递费失败.'
        else if (code == -1034)
            msg = '抱歉,根据对应的Mark找不到供应商.'
        else if (code == -1035)
            msg = '抱歉,该账号已经存在.'
        else if (code == -1036)
            msg = '抱歉,该用户不是加盟商,权限不足.'
        else if (code == -1037)
            msg = '抱歉,该用户不是供应商,权限不足.'
        else if (code == -1038)
            msg = '抱歉,在短时间内已发送过短信.'
        else if (code == -1039)
            msg = '抱歉,sign返回失败.'
        else if (code == -1040)
            msg = '抱歉,该用户未绑定手机号.'
        else if (code == -1041)
            msg = '抱歉,该用户已绑定手机号.'
        else if (code == -1042)
            msg = '抱歉,找不到对应的银行卡.'
        else if (code == -1043)
            msg = '抱歉,该产品已申请过售后,请勿重复申请.'
        else if (code == -1044)
            msg = '抱歉,余额不足,请先充值.'
        else if (code == -1045)
            msg = '抱歉,找不到物流信息.'
        else if (code == -1046)
            msg = '抱歉,退款金额超过最可退款数额.'
        else if (code == -1047)
            msg = '抱歉,退款金额超过最可退换货数量.'
        else if (code == -1048)
            msg = '抱歉,必要的参数错误.'
        else if (code == -1049)
            msg = '抱歉,今日已成功提醒过商家，请等待商家发货.'
        else if (code == -1050)
            msg = '抱歉,收益快照创建失败.'
        else if (code == -1051)
            msg = '抱歉,充值失败.'
        else if (code == -1052)
            msg = '抱歉,找不到对应的支付方式.'
        else if (code == -1053)
            msg = '抱歉,购物车ID错误.'
        else if (code == -1054)
            msg = '抱歉,已经售后期,无法申请售后.'
        else if (code == -1055)
            msg = '抱歉,该售后订单不能退邮费.'
        else if (code == -1056)
            msg = '抱歉，您购物车中的部分店铺不足最少下单量,请检查后再结算！'
        $.toast(msg);
    }
}