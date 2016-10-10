Date.prototype.format = function (format) {
    var o = {
        'M+': this.getMonth() + 1,
        'd+': this.getDate(),
        'h+': this.getHours(),
        'm+': this.getMinutes(),
        's+': this.getSeconds(),
        'q+': Math.floor((this.getMonth() + 3) / 3),
        'S': this.getMilliseconds()
    };
    if (/(y+)/.test(format) || /(Y+)/.test(format)) {
        format = format.replace(RegExp.$1, (this.getFullYear() + '').substr(4 - RegExp.$1.length));
    }
    for (var k in o) {
        if (new RegExp('(' + k + ')').test(format)) {
            format = format.replace(RegExp.$1, RegExp.$1.length == 1 ? o[k] : ('00' + o[k]).substr(('' + o[k]).length));
        }
    }
    return format;
};
var Cnaws = {
    options: {},
    defaultOptions: {
        urlExt: '.html',
        resourcesUrl: '',
        passportUrl: ''
    },
    defaultAjaxOptions: {
        url: null,
        data: null,
        args: null,
        type: 'json',
        async: true,
        method: 'GET'
    },
    init: function (options) {
        Cnaws.options = $.extend({}, Cnaws.defaultOptions, options);
    },
    getUrl: function (url) {
        return url + Cnaws.options.urlExt;
    },
    getResUrl: function (url) {
        return Cnaws.options.resourcesUrl + Cnaws.getUrl(url);
    },
    getPassUrl: function (url) {
        return Cnaws.options.passportUrl + Cnaws.getUrl(url);
    },
    ajax: function (op, func) {
        var o = $.extend({}, Cnaws.defaultAjaxOptions, op);
        if (o.url) {
            var d = null;
            if ("POST" == o.method) {
                d = o.data;
                if (d) {
                    if (d && "string" == typeof d) {
                        d = $(d).serialize();
                    }
                    else {
                        var i = 0;
                        var json = '';
                        for (var key in d) {
                            if (i++ > 0)
                                json += '&';
                            json += key + '=' + encodeURIComponent(d[key]);
                        }
                        d = json;
                    }
                }
            }
            if (o.async) {
                return $.ajax({
                    type: o.method,
                    url: o.url,
                    data: d,
                    dataType: o.type,
                    async: o.async,
                    __func: func,
                    __args: o.args,
                    success: function (result) {
                        this.__func(result, this.__args);
                    }
                });
            }
            else {
                var result = null;
                var xhr = $.ajax({
                    type: o.method,
                    url: o.url,
                    data: d,
                    dataType: o.type,
                    async: o.async
                });
                if (o.type == 'json')
                    result = xhr.responseJSON;
                else if (o.type == 'xml')
                    result = responseXML;
                else
                    result = responseText;
                return func(result, o.args);
                return xhr;
            }
        }
        return undefined;
    },
    checkCaptcha: function (name, captcha, func) {
        Cnaws.postAjax(Cnaws.getResUrl('/captcha/checkcaptcha'), 'name=' + encodeURIComponent(name) + '&captcha=' + encodeURIComponent(captcha), function (data, args) {
            func && func(data, args);
        });
        return false;
    },
    changeCaptcha: function (img, name) {
        $(img).attr('src', Cnaws.getResUrl('/captcha/custom/' + name) + '?' + parseInt(Math.random() * 10000));
        return false;
    },
    showInfo: function (msg, title) {
        title = title || '';
        msg = msg || title;
        return toastr.info(title, msg)
    },
    showSuccess: function (msg, title) {
        title = title || '';
        msg = msg || title;
        return toastr.success(title, msg)
    },
    showWarning: function (msg, title) {
        title = title || '';
        msg = msg || title;
        return toastr.warning(title, msg)
    },
    showError: function (msg, title) {
        title = title || '';
        msg = msg || title;
        return toastr.error(title, msg)
    }
};

(function () {
    toastr.options = {
        "closeButton": true,
        "debug": false,
        "progressBar": true,
        "positionClass": "toast-top-right",
        "onclick": null,
        "showDuration": "400",
        "hideDuration": "1000",
        "timeOut": "7000",
        "extendedTimeOut": "1000",
        "showEasing": "swing",
        "hideEasing": "linear",
        "showMethod": "fadeIn",
        "hideMethod": "fadeOut"
    };
    if ($.validator !== undefined) {
        $.validator.setDefaults({
            highlight: function (e) {
                $(e).closest(".form-group").removeClass("has-success").addClass("has-error")
            },
            success: function (e) {
                e.closest(".form-group").removeClass("has-error").addClass("has-success")
            },
            errorElement: "span",
            errorPlacement: function (e, r) {
                e.appendTo(r.is(":radio") || r.is(":checkbox") ? r.parent().parent().parent() : r.parent())
            },
            errorClass: "help-block m-b-none",
            validClass: "help-block m-b-none"
        });
    }
    if ($.fn.editable !== undefined)
        $.fn.editable.defaults.mode = 'inline';
})();