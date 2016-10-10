+(function(){
    var getUrl = typeof (getUrl) != "undefined" ? getUrl : function (url) {
        return url + '.html';
    };
    var url = "/country/";
    var HOTCITYSLIST = [],
    PROVINCELIST = {},
    CITIESLIST = [],
    areaSelectChange = function (g, f, j, i) {
        function h(a, d, _f) {
            a && (this.selectContainer = g("#" + a), this.selectContent = this.selectContainer.find(".city_content"), this.areaName = this.selectContainer.find(".area_name"), this.areaTitle = this.selectContainer.find(".area_title"), this.area_list = this.selectContainer.find(".area_list"), this.loadedHot = !1, this.loadedProvince = !1, this.currentArea = {},
            this.o = d || {}, this._fun = _f || function () { },
            this.hasHot = this.o.hasHot || !1, this.hideCounty = this.o.hideCounty || !1, this.init())
        }
        return h.prototype = {
            init: function () {
                this.bindEvents(),
                this.tabChange(),
                this.createHideIpt()
            },
            bindEvents: function () {
                var m = this.selectContainer,
                l = this.selectContent,
                k = this,
                c = this.currentArea,
                b = this.o;
                if (b && b.hideName) {
                    b.hideName
                } else { }
                var a = this.areaTitle.is('input[type="text"]');
                this.areaName.on("click",
                function (d) {
                    1 == l.is(":visible") ? k.hidePanel() : (k.hasHot && k.showHotCitys(f), k.showProvince(j), k.showPanel(), k.o.showFun && k.o.showFun(), k.selectCurrentItem()),
                    d.stopPropagation()
                }),
                this.selectContainer.on("click", ".items a",
                function (d) {

                    var r = g(this),
                    q = r.attr("data-origin"),
                    p = k.areaTitle,
                    o = r.attr("data-id");
                    if ("hot" == q) {
                        a ? p.val(r.text()) : p.text(r.text()),
                        m.find('[data-target="city"]').val(o),
                        k.hidePanel()
                    } else {
                        if ("p" == q) {
                            c.p = r.text(),
                            m.find('[data-target="province"]').val(o),
                            m.find('[data-target="city"]').val(""),
                            m.find('[data-target="county"]').val(""),
                            k.tabItem("city_list"),
                            a ? p.val(c.p) : p.text(c.p),
                            k.showCity(o, "city_list", "c")
                        } else {
                            if ("c" == q) {

                                c.c = r.text(),
                                a ? p.val(c.p + " " + c.c) : p.text(c.p + "-" + c.c),
                                m.find('[data-target="city"]').val(o),
                                1 == k.hideCounty ? k.hidePanel() : (k.tabItem("county_list"), k.showCity(o, "county_list", "t"))
                            } else {
                                if ("t" == q) {
                                    c.t = r.text(),
                                    m.find('[data-target="county"]').val(o);
                                    var n = c.p + "-" + c.c + "-" + c.t;
                                    c.p == c.c && (n = c.p + "-" + c.t),
                                    a ? p.val(n.replace(/\//g, " ")) : p.text(n),
                                    k.hidePanel()
                                    k.Fun();
                                }
                            }
                        }
                    }
                    k.o.clickItemFun && k.o.clickItemFun(r),
                    d.stopPropagation()
                }),
                this.selectContainer.on("click",
                function (d) {
                    k.showPanel(),
                    d.stopPropagation()
                }),
                g(document.body).on("click",
                function () {
                    k.hidePanel()
                })
            },
            showPanel: function () {
                this.selectContent.show(),
                this.areaName.addClass("open")
            },
            hidePanel: function () {
                //this.selectContent.hide(),
                //this.areaName.removeClass("open"),
                //this.o.hideFun && this.o.hideFun()
            },
            Fun: function () {
                this._fun();
            },
            showHotCitys: function (l) {
                if (this.loadedHot === !1) {
                    this.area_list.hide();
                    for (var k = this.selectContainer.find(".hot_city"), p = '<ul class="items clear">', o = 0, n = l.length; n > o; o++) {
                        var m = l[o];
                        p += '<li><a href="javascript:;" data-origin="hot" data-id=' + m[0] + ">" + m[1] + "</a></li>"
                    }
                    p += "</ul>",
                    k.html(p).show(),
                    this.loadedHot = !0
                }
            },
            showProvince: function (l) {
                if (this.loadedProvince === !1) {
                    var k = this.selectContainer.find(".province_list");
                    this.area_list.hide();
                    var q = '<ul class="items clear">';
                    $.ajax({
                        type: "GET",
                        url: getUrl(url + 'provinces'),
                        dataType: "json",
                        async: false,
                        success: function (data) {
                            if (data.code == -200) {
                                if (data.data.length > 0) {
                                    for (var i = 0; i < data.data.length; i++) {
                                        q += '<li><a href="javascript:;" data-origin="p" data-id=' + data.data[i].id + ">" + data.data[i].sname + "</a></li>"
                                    }
                                }
                            }
                        }
                    });
                    q += "</ul>",
                    k.html(q).show(),
                    this.loadedProvince = !0
                }
            },
            showCity: function (s, r, q) {
                if (s != 0) {
                    var p = CITIESLIST;
                    this.area_list.hide();
                    var o = this.selectContainer.find("." + r);
                    var n = '<ul class="items clear">';
                    var newurl = '';
                    if (q == "c")
                        newurl = getUrl(url + 'cities/' + s)
                    else if (q == "t")
                        newurl = getUrl(url + 'counties/' + s)
                    $.ajax({
                        type: "GET",
                        url: newurl,
                        dataType: "json",
                        async: false,
                        success: function (data) {
                            if (data.code == -200) {
                                if (data.data.length > 0) {
                                    for (var i = 0; i < data.data.length; i++) {
                                        n += '<li><a href="javascript:;" data-origin=' + q + " data-id=" + data.data[i].id + ">" + data.data[i].sname + "</a></li>"
                                    }
                                }
                            }
                        }
                    });
                    n += "</ul>",
                    o.html(n).show()
                }
                else {
                    k.hidePanel();
                }
            },
            tabChange: function () {
                var a = this.selectContainer,
                c = this;
                a.on("click", "[data-to]",
                function () {
                    var d = g(this),
                    k = d.attr("data-to");
                    "province_list" == k && c.showProvince(j),
                    c.tabItem(k)
                })
            },
            tabItem: function (d) {
                var c = this.selectContainer;
                c.find("[data-to]").removeClass("current"),
                c.find(".area_list").hide(),
                c.find("[data-to=" + d + "]").addClass("current"),
                c.find("." + d).show()
            },
            selectCurrentItem: function () {
                var l = (this.areaTitle, this.selectContainer);
                l.find("[data-id]").removeClass("current");
                var k = l.find('[data-target="province"]').val(),
                o = l.find('[data-target="city"]').val(),
                n = l.find('[data-target="county"]').val();
                if (o) {
                    var m = l.find(".hot_city");
                    m.find("[data-id=" + o + "]").addClass("current"),
                    l.find(".city_list").find("[data-id=" + o + "]").addClass("current")
                }
                if (k) {
                    var m = l.find(".province_list");
                    m.find("[data-id=" + k + "]").addClass("current")
                }
                if (n) {
                    var m = l.find(".county_list");
                    m.find("[data-id=" + n + "]").addClass("current")
                }
            },
            createHideIpt: function () {
                var l = this.o;
                if (l && l.hideName) {
                    var k = l.hideName
                } else {
                    var k = {}
                }
                var r = (k.hotcityName, k.provinceName),
                q = k.cityName,
                p = k.countyName,
                o = this.selectContainer.attr("data-pid"),
                n = this.selectContainer.attr("data-cid"),
                m = this.selectContainer.attr("data-tid");
                this.selectContainer.append('<input type="hidden" data-target="province"  name=' + (r || "provinceName") + " id=" + (r || "provinceName") + " value=" + (o || "") + ">"),
                this.selectContainer.append('<input type="hidden" data-target="city" id=' + (q || "cityName") + ' name=' + (q || "cityName") + " value=" + (n || "") + ">"),
                1 != this.hideCounty && this.selectContainer.append('<input type="hidden" data-target="county" id=' + (p || "countyName") + ' name=' + (p || "countyName") + " value=" + (m || "") + ">")
            },
            getProNameByCode: function (a) {
                if (!a) {
                    return ""
                }
                a += "";
                for (var n in PROVINCELIST) {
                    for (var m = PROVINCELIST[n], l = 0, k = m.length; k > l; l++) {
                        if (g.trim(a) == m[l], [0]) {
                            return m[l], [1]
                        }
                    }
                }
                return ""
            },
            getCityNameByCode: function (k) {
                if (!k) {
                    return ""
                }
                k += "";
                for (var e = 0,
                m = CITIESLIST.length; m > e; e++) {
                    var l = CITIESLIST[e];
                    if (k == l[0]) {
                        return l[1]
                    }
                }
                return ""
            }
        },
        h
    }(jQuery, HOTCITYSLIST, PROVINCELIST, CITIESLIST);

    window.areaSelectChange = areaSelectChange;
})()