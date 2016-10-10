// city
+function ($) {
    var ext = '.html';
    $.ajax({
        url: "/country/getpickerdata" + ext,
        type: "GET",
        async: false,
        success: function (data) {
            var raw = JSON.parse(data);

            var format = function (data) {
                var result = [];
                for (var i = 0; i < data.length; i++) {
                    var d = data[i];
                    if (d.name === "请选择") continue;
                    result.push(d.name);
                }
                if (result.length) return result;
                return [""];
            };
            var formatid = function (data) {
                var result = [];
                for (var i = 0; i < data.length; i++) {
                    var d = data[i];
                    if (d.name === "请选择") continue;
                    result.push(d.id);
                }
                if (result.length) return result;
                return [""];
            };

            var sub = function (data) {
                if (!data.sub) return [""];
                return format(data.sub);
            };
            var subid = function (data) {
                if (!data.sub) return [""];
                return formatid(data.sub);
            };

            //var getDistrictId = function (p, c,d) {
            //    for (var i = 0; i < raw.length; i++) {
            //        if (raw[i].name === p) {
            //            for (var j = 0; j < raw[i].sub.length; j++) {
            //                if (raw[i].sub[j].name === c) {
            //                    for (var x = 0; x < raw[i].sub[j].sub.length; x++) {
            //                        if (raw[i].sub[j].sub[x].name == d)
            //                            return raw[i].sub[j].sub[x].id;
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}


            var getCities = function (d) {
                for (var i = 0; i < raw.length; i++) {
                    if (raw[i].name === d) { /*currentProvinceId = raw[i].id; */return sub(raw[i]); };
                }
                return [""];
            };

            var getCitiesid = function (d) {
                for (var i = 0; i < raw.length; i++) {
                    if (raw[i].name === d) return subid(raw[i]);
                }
                return [""];
            };

            var getDistricts = function (p, c) {
                for (var i = 0; i < raw.length; i++) {
                    if (raw[i].name === p) {
                        for (var j = 0; j < raw[i].sub.length; j++) {
                            if (raw[i].sub[j].name === c) {
                                //currentCityId = raw[i].sub[j].id;
                                return sub(raw[i].sub[j]);
                            }
                        }
                    }
                }
                return [""];
            };
            var getDistrictsid = function (p, c) {
                for (var i = 0; i < raw.length; i++) {
                    if (raw[i].name === p) {
                        for (var j = 0; j < raw[i].sub.length; j++) {
                            if (raw[i].sub[j].name === c) {
                                return subid(raw[i].sub[j]);
                            }
                        }
                    }
                }
                return [""];
            };

            //var raw = $.smConfig.rawCitiesData;
            var provinces = raw.map(function (d) {
                return d.name;
            });

            var provincesid = raw.map(function (d) {
                return d.id;
            });

            var initCities = sub(raw[0]);
            var initCitiesid = subid(raw[0]);

            var initDistricts = sub(raw[0].sub[0]);
            var initDistrictsid = subid(raw[0].sub[0]);

            var currentProvince = provinces[0];
            var currentCity = initCities[0];
            var currentDistrict = initDistricts[0];
            var currentProvinceId = 0;
            var currentCityId = 0;
            var currentDistrictId = 0;

            var t;
            var defaults = {
                cssClass: "city-picker",
                rotateEffect: false,
                onChange: function (picker, values, displayValues) {
                    var newProvince = picker.cols[0].displayValue;
                    var newCity;
                    if (newProvince !== currentProvince) {
                        clearTimeout(t);
                        t = setTimeout(function () {
                            var newCities = getCities(newProvince);
                            newCity = newCities[0];
                            var newCityids = getCitiesid(newProvince);
                            var newDistricts = getDistricts(newProvince, newCity);
                            var newDistrictids = getDistrictsid(newProvince, newCity);
                            picker.cols[1].replaceValues(newCityids, newCities);
                            picker.cols[2].replaceValues(newDistrictids, newDistricts);
                            currentProvince = newProvince;
                            currentCity = newCity;
                            picker.updateValue();
                        }, 200);
                        return;
                    }
                    newCity = picker.cols[1].displayValue;
                    if (newCity !== currentCity) {
                        clearTimeout(t);
                        t = setTimeout(function () {
                            var newDistricts = getDistricts(newProvince, newCity);
                            var newDistrictids = getDistrictsid(newProvince, newCity);
                            picker.cols[2].replaceValues(newDistrictids, newDistricts);
                            currentCity = newCity;
                            picker.updateValue();
                        }, 200);
                        return;
                    }
                    $(picker.input).attr("data-id", values);
                },

                cols: [
                {
                    textAlign: 'center',
                    values: provincesid,
                    displayValues: provinces,
                    cssClass: "col-province"
                },
                {
                    textAlign: 'center',
                    values: initCitiesid,
                    displayValues: initCities,
                    cssClass: "col-city"
                },
                {
                    textAlign: 'center',
                    values: initDistrictsid,
                    displayValues: initDistricts,
                    cssClass: "col-district"
                }
                ]
            };

            $.fn.cityPicker = function (params) {
                params.formatValue = function (picker, values, displayValues) {
                    return displayValues.join(' ');
                }
                return this.each(function () {
                    if (!this) return;
                    var p = $.extend(defaults, params);
                    //计算value
                    if (p.value) {
                        $(this).val(p.value.join(' '));
                    } else {
                        var val = $(this).val();
                        val && (p.value = val.split(' '));
                    }
                    if (p.value[0] == '请选择')
                        p.value = null;
                    if (p.value) {
                        //p.value = val.split(" ");
                        if (p.value[0]) {
                            currentProvince = p.value[0];
                            p.cols[1].displayValues = getCities(p.value[0]);
                            p.cols[1].values = getCitiesid(p.value[0]);
                        }
                        if (p.value[1]) {
                            currentCity = p.value[1];
                            p.cols[2].displayValues = getDistricts(p.value[0], p.value[1]);
                            p.cols[2].values = getDistrictsid(p.value[0], p.value[1]);
                        } else {
                            p.cols[2].displayValues = getDistricts(p.value[0], p.cols[1].values[0]);
                            p.cols[2].values = getDistrictsid(p.value[0], p.cols[1].values[0]);
                        }
                        !p.value[2] && (p.value[2] = '');
                        currentDistrictId = getDistrictId(currentProvince,currentCity,currentDistrict);
                        if ($(this).attr("data-id") != '') {
                            p.value = $(this).attr("data-id").split(',');
                        }
                        currentDistrict = p.value[2];
                    }
                    $(this).picker(p);
                });
            };
        }
    })
}(Zepto);
// jshint ignore: end
/* jshint unused:false*/