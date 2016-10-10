(function ($) {
    function getHeight() {
        return $(window).height() - $('.height-begin').outerHeight(true);
    }

    var DataTable = function (el, options) {
        this.options = $.extend({ __DataTable: this }, DataTable.DEFAULTS, options);
        this.$el = $(el);
        this.$el.bootstrapTable(this.options);
        if (this.options.removeAllButton !== undefined) {
            if (this.options.removeAllCheck) {
                this.$el.on('check.bs.table uncheck.bs.table check-all.bs.table uncheck-all.bs.table', null, this, function (e) {
                    $(e.data.options.removeAllButton).prop('disabled', !e.data.$el.bootstrapTable('getSelections').length);
                });
            }
            else {
                $(this.options.removeAllButton).prop('disabled', false);
            }
            $(this.options.removeAllButton).click(this, function (e) {
                if (window.confirm('删除后不可恢复，确认删除？')) {
                    var self = e.data;
                    var ids = $.map(self.$el.bootstrapTable('getSelections'), self.options.getRowId);
                    var data = {};
                    data[self.options.idField] = ids.join(',');
                    Cnaws.ajax({
                        method: 'POST',
                        url: self.options.removeAllUrl,
                        data: data,
                        args: { self: self, ids: ids }
                    }, function (data, args) {
                        if (data.code == -200) {
                            args.self.$el.bootstrapTable('remove', {
                                field: args.self.$el.bootstrapTable('getOptions').idField,
                                values: args.ids
                            });
                            $(args.self.options.removeAllButton).prop('disabled', args.self.options.removeAllCheck);
                            Cnaws.showSuccess('删除成功');
                            args.self.$el.bootstrapTable('refresh');
                        }
                        else {
                            Cnaws.showError(data.data, data.code);
                        }
                    });
                }
            });
        }
    };

    DataTable.LOCALES = {};
    DataTable.DEFAULTS = {
        height: getHeight(),
        sortOrder: 'desc',
        dataField: 'data',
        ajax: function (params) {
            var options = this.options;
            var url = options.__DataTable.options.loadUrl.replace(/\{(page|size|sort|order|search|type)\}/ig, function (key) {
                if ('{page}' === key.toLowerCase())
                    return (params.data.offset === undefined || params.data.limit === undefined) ? 1 : Math.ceil(params.data.offset / params.data.limit) + 1;
                if ('{size}' === key.toLowerCase())
                    return params.data.limit === undefined ? options.pageSize : params.data.limit;
                if ('{sort}' === key.toLowerCase())
                    return params.data.sort === undefined ? options.idField : encodeURIComponent(params.data.sort);
                if ('{order}' === key.toLowerCase())
                    return params.data.order === undefined ? options.sortOrder : encodeURIComponent(params.data.order);
                if ('{search}' === key.toLowerCase())
                    return params.data.search === undefined ? '' : encodeURIComponent(params.data.search);
                if ('{type}' === key.toLowerCase())
                    return params.data.type === undefined ? '' : encodeURIComponent(params.data.type);
                return '';
            });
            Cnaws.ajax({
                url: url,
                args: params
            }, function (data, args) {
                if (data.code == -200) {
                    if (data.data.total == undefined)
                        args.success({ 'total': data.data.length, 'data': data.data });
                    else
                        args.success(data.data);
                }
                else {
                    args.error({ status: data });
                }
            });
        },
        mobileResponsive: true,
        pagination: true,
        sidePagination: 'server',
        pageSize: 10,
        pageList: [10],
        search: true,
        searchOnEnterKey: true,
        showColumns: true,
        showRefresh: true,
        showToggle: true,
        showExport: false,
        iconSize: "outline",
        icons: {
            refresh: "glyphicon-repeat",
            toggle: "glyphicon-list-alt",
            columns: "glyphicon-list",
            'export': "glyphicon-export icon-share"
        },

        onLoadError: function (data) {
            Cnaws.showError(data.data, data.code);
            return false;
        },
        onColumnSwitch: function (field, checked) {
            console.log(field)
            console.log(checked)
            return false;
        },

        loadUrl: undefined,
        removeAllButton: undefined,
        removeAllUrl: undefined,
        removeAllCheck: true,

        getRowId: function (row) {
            return row.Id;
        }
    };

    DataTable.prototype.getOptions = function () {
        return this.options;
    };
    DataTable.prototype.getTable = function () {
        return this.table;
    };
    DataTable.prototype.destroy = function () {

    };

    var allowedMethods = [
        'getOptions',
        'getTable',
        'destroy'
    ];

    $.fn.DaTab = function (option) {
        var value;
        var args = Array.prototype.slice.call(arguments, 1);
        this.each(function () {
            var $this = $(this);
            var data = $this.data('Cnaws.DataTable');
            if (typeof option === 'string') {
                if ($.inArray(option, allowedMethods) < 0) {
                    throw new Error("Unknown method: " + option);
                }
                if (!data) {
                    return;
                }
                value = data[option].apply(data, args);
                if (option === 'destroy') {
                    $this.removeData('Cnaws.DataTable');
                }
            }
            if (!data) {
                $this.data('Cnaws.DataTable', (data = new DataTable(this, $.extend({}, DataTable.DEFAULTS, $this.data(), typeof option === 'object' && option))));
            }
        });
        return typeof value === 'undefined' ? this : value;
    };

    $.fn.DaTab.Constructor = DataTable;
    $.fn.DaTab.locales = DataTable.LOCALES;
    $.fn.DaTab.methods = allowedMethods;
})(jQuery);