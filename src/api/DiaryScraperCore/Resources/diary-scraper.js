var pageSize = 20;
var pageCount = 10;
var currentPage = 1;
var postStrings = [];
// $("div.singlePost").css('visibility', 'visible');



function showPage(pageIndex) {
	if (pageIndex < 1) {
		pageIndex = 1;
	}
	currentPage = pageIndex;
	$("div.singlePost").remove();
	var startIndex = (pageIndex - 1) * pageSize;
	if (startIndex >= postStrings.lenth) {
		startIndex = postStrings.length - 1;
	}

	var endIndex = pageIndex * pageSize;
	endIndex = Math.min(endIndex, postStrings.length);
	restoreAnchors();
	var anchor = $("a.pageAnchor[page='" + pageIndex + "']");
	$("<strong>").addClass("pageAnchor").attr("page", pageIndex).text(pageIndex).insertAfter(anchor);
	anchor.remove();


	postStrings.slice(startIndex, endIndex).reverse().forEach(function(postString) {
		$("<div>").html(postString).children().insertAfter("div#pageBar");
	});
}

function nextPage() {
	if (currentPage >= pageCount) {
		return;
	}
	currentPage += 1;
	showPage(currentPage);
}

function prevPage() {
	if (currentPage <= 1) {
		return;
	}
	currentPage -= 1;
	showPage(currentPage);
}

function restoreAnchors() {
	$("#pageBar strong.pageAnchor").each(function () {
		var strong = $(this);
		var pageIndex = parseInt(strong.attr("page"), 10);
		$("<a>").addClass("pageAnchor")
			.attr("page", pageIndex)
			.text(pageIndex)
			.attr("href", "#" + pageIndex)
			.click(pageClickHandler)
			.insertAfter(strong);
		strong.remove();
	})
}

function pageClickHandler(e) {
	e.preventDefault();
	var anchor = $(e.target);
	var pageIndex = parseInt(anchor.attr("page"), 10);
	showPage(pageIndex);
}

function initPages(pageSizeParam) {
	pageSize = pageSizeParam;
	pageCount = Math.ceil(postStrings.length / pageSize);
	var td = $("#tdPages");
	td.html();
	for (var i = 1; i <= pageCount; i++) {
		var el = i === 1 ? $("<strong>") : $("<a>");
		el.text(i).attr("href", "#" + i).addClass("pageAnchor").attr("page", i).appendTo(td);
		if (i % 20 == 0) {
			$("<br>").appendTo(td);
		}
	}
	td.find(".pageAnchor").click(pageClickHandler);
	$("#anchorNext").click(function (e) {
		nextPage();
	});
	$("#anchorPrev").click(function (e) {
		prevPage();
	});
	showPage(1);
}


$(function () {
	$("a.LinkMore").click(function (e) {
		e.preventDefault();
		var spanId = $(e.target).attr("id");
		spanId = spanId.substring(4);
		$("#" + spanId).show().css('visibility', 'visible');;
		$(e.target).hide();
	});
});