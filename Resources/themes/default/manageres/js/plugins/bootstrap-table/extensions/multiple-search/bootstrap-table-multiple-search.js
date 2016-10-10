/**
 * @author: Dennis Hernández
 * @webSite: http://djhvscf.github.io/Blog
 * @version: v1.0.0
 */

!function ($) {

    'use strict';

    $.extend($.fn.bootstrapTable.defaults, {
        enableSearchTypes: false,
        searchTypes: [],
        searchType: undefined,
        queryParams: function (params) {
            return $.extend({}, { type: this.searchType }, params);
        }
    });

    var BootstrapTable = $.fn.bootstrapTable.Constructor,
        _initToolbar = BootstrapTable.prototype.initToolbar;

    BootstrapTable.prototype.initToolbar = function () {
        _initToolbar.apply(this, Array.prototype.slice.apply(arguments));
        if (this.options.enableSearchTypes) {
            this.$searchTypes = $('<div class="input-group-btn"></div>');
            this.$searchLink = $('<button class="btn btn-white dropdown-toggle" data-toggle="dropdown">选择搜索类型<span class="caret"></span></button>');
            this.$searchTypeList = $('<ul class="dropdown-menu"></ul>');
            this.$toolbar.find('.search').addClass('input-group').addClass('col-sm-6');
            this.$toolbar.find('.search input').before(this.$searchTypes);
            this.$searchTypes.append(this.$searchLink);
            this.$searchTypes.append(this.$searchTypeList);

            var iter;
            var def = '选择搜索类型';
            for (var i = 0; i < this.options.searchTypes.length; ++i) {
                iter = this.options.searchTypes[i];
                if (iter.value === this.options.searchType) {
                    def = iter.text;
                }
                this.$searchTypeList.append($('<li><a href="javascript:void(0)" data-value="' + iter.value + '">' + iter.text + '</a></li>'));
            }
            this.$searchLink.html(def + '<span class="caret"></span>');

            this.$searchTypeList.find('a').off('click').on('click', null, this, function (e) {
                e.data.$searchLink.html($(this).html() + '<span class="caret"></span>');
                e.data.options.searchType = $(this).attr('data-value');
            });
        }
    };

}(jQuery);
