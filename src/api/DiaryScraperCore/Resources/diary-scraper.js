$(function () {
	$("a.LinkMore").click(function (e) {
		e.preventDefault();
		var spanId = $(e.target).attr("id");
		spanId = spanId.substring(4);
		$("#" + spanId).show().css('visibility', 'visible');;
		$(e.target).hide();
	});
});