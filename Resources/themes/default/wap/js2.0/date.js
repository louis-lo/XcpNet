$(function () {
	'use strict';
	$(document).on("pageInit", "#page-datetime-picker", function(e) {
	    $("#datetime-picker").datetimePicker({
	    	toolbarTemplate: '<header class="bar bar-nav">\
	    	<button class="button button-link pull-right close-picker">确定</button>\
	    	<h1 class="title">选择日期和时间</h1>\
	    	</header>'
	    });
	    
	});
	
	$.init();
});