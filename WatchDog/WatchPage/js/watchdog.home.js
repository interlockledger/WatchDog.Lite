$(document).ready(function () {
    $(".tabs").click(function () {
        $(".tabs").removeClass("active");
        $(".tabs h6").removeClass("font-weight-bold");
        $(".tabs h6").addClass("text-muted");
        $(this).children("h6").removeClass("text-muted");
        $(this).children("h6").addClass("font-weight-bold");
        $(this).addClass("active");
        current_fs = $(".active");
        next_fs = $(this).attr('id');
        next_fs = "#" + next_fs + "1";
        $("fieldset").removeClass("show");
        $(next_fs).addClass("show");
        current_fs.animate({}, {
            step: function () {
                current_fs.css({
                    'display': 'none',
                    'position': 'relative'
                });
                next_fs.css({
                    'display': 'block'
                });
            }
        });
    });
});

var pageIndex = 1;
var exPageIndex = 1;
var inCodePageIndex = 1;
document.getElementById('reqLog').style.textDecoration = "underline";
var connection = new signalR.HubConnectionBuilder().withUrl("/wtchdlogger").build();

connection.on("getLogs", function (data) {
    if (sessionStorage.getItem("loggedIn") !== null && sessionStorage.getItem("loggedIn") === sessionStorage.getItem("newloggedIn")) {
        if (data.type === "rqLog") {
            const firstDigitStr = String(data.log.responseStatus)[0];
            var statusColor = firstDigitStr === '2' ? "text-success" : firstDigitStr === '1' || firstDigitStr === '3' ? "text-primary" : "text-danger";
            const tr = $("<tr data-toggle='modal' data-target='#myModal' aria-expanded='false' aria-controls='collapse' class='collapsed'>" +
                "<td><b>" + data.log.method + "</b></td><td>" + data.log.path + "</td><td><b class='" + statusColor + "'> " + data.log.responseStatus + "</b></td><td>" + data.log.timeSpent + "</td><td>" + moment(data.log.startTime).format('LLL') + "</td></tr>");
            tr.on('click', populateModal.bind(null, data.log));
            $('#tableBody').prepend(tr);
        } else if (data.type === "exLog") {
            const tr = $("<tr data-toggle='modal' data-target='#myExceptionModal' aria-expanded='false' aria-controls='collapse' class='collapsed'>" +
                "<td><b>" + data.log.source + "</b></td><td>" + data.log.message + "</td><td>" + data.log.typeOf + "</td><td>" + moment(data.log.encounteredAt).format('LLL') + "</td></tr > ");
            tr.on('click', populateExceptionModal.bind(null, data.log));
            $('#tableBodyEx').prepend(tr);
        } else if (data.type === "log") {
            const tr = $("<tr aria-expanded='false' aria-controls='collapse' class='collapsed'>" +
                "<td><b>" + moment(data.log.timestamp).format('LLL') + "</b></td><td>" + data.log.callingFrom + "\nLine:" + data.log.lineNumber + "</td><td>" + data.log.callingMethod + "</td><td>" + data.log.message + "</td></tr > ");
            $('#tableBodyInCode').prepend(tr);
        }

    }

});

connection
    .start()
    .then(function () {
        $("[name='dot']").css("backgroundColor", "green");
        getLogs();
        getExceptionLogs();
        getInCodeLogs();
    }).catch(err => {
        getLogs();
        getExceptionLogs();
        getInCodeLogs();
        $("[name='dot']").css("backgroundColor", "red");
    });

$("#myVerbDropDown").change(function (event) {
    $('#tableBody').empty();
    if ($("#myVerbDropDown")[0].selectedIndex === 0) {
        getLogs($('#searchString').val(), "", $('#myStatusCodeDropDown').val() == "ALL" ? "" : $('#myStatusCodeDropDown').val())
    } else {
        filterVerb($(this).val());
    }

});

$("#myStatusCodeDropDown").change(function (event) {
    $('#tableBody').empty();
    if ($("#myStatusCodeDropDown")[0].selectedIndex === 0) {
        getLogs($('#searchString').val(), $('#myVerbDropDown').val() == "ALL" ? "" : $('#myVerbDropDown').val(), "")
    } else {
        filterStatusCode($(this).val());
    }

});

function getLogs(searchString = "", verb = "", statusCode = "") {
    $.ajax({
        type: "GET",
        url: "/WTCHDwatchpage?pageNumber=" + pageIndex + "&searchString=" + searchString + "&verbString=" + verb + "&statusCode=" + statusCode,
        context: document.body,
        success: function (data) {
            var totalPages = data.totalPages === 0 ? 1 : data.totalPages
            $('#pageMap').text("Page " + data.pageIndex + " of " + totalPages);
            if (data.hasNext === false) {
                $('#next').hide();
            }
            else {
                $('#next').show();

            }

            if (data.hasPrevious === false) {
                $('#prev').hide();
            }
            else {
                $('#prev').show();
            }

            if (sessionStorage.getItem("loggedIn") === null) {
                autologin();

            } else {
                if (sessionStorage.getItem("loggedIn") !== sessionStorage.getItem("newloggedIn")) {
                    autologin();
                } else {
                    for (var i = 0; i < data.logs.length; i++) {
                        const firstDigitStr = String(data.logs[i].responseStatus)[0];
                        var statusColor = firstDigitStr === '2' ? "text-success" : firstDigitStr === '1' || firstDigitStr === '3' ? "text-primary" : "text-danger";
                        const tr = $("<tr data-toggle='modal' data-target='#myModal' aria-expanded='false' aria-controls='collapse" + i + "' class='collapsed'>" +
                            "<td><b>" + data.logs[i].method + "</b></td><td>" + data.logs[i].path + "</td><td><b class='" + statusColor + "'> " + data.logs[i].responseStatus + "</b ></td ><td>" + data.logs[i].timeSpent + "</td><td>" + moment(data.logs[i].startTime).format('LLL') + "</td></tr > ");
                        tr.on('click', populateModal.bind(null, data.logs[i]));
                        $('#tableBody').append(tr);
                    }
                }
            }

        }
    });
}
function getExceptionLogs(searchString = "") {
    $.ajax({
        type: "GET",
        url: "/WTCHDwatchpage/Exceptions?pageNumber=" + exPageIndex + "&searchString=" + searchString,
        context: document.body,
        success: function (data) {
            var totalPages = data.totalPages === 0 ? 1 : data.totalPages
            $('#exPageMap').text("Page " + data.pageIndex + " of " + totalPages);
            if (data.hasNext === false) {
                $('#exNext').hide();
            }
            else {
                $('#exNext').show();

            }

            if (data.hasPrevious === false) {
                $('#exPrev').hide();
            }
            else {
                $('#exPrev').show();
            }

            for (var i = 0; i < data.logs.length; i++) {
                const tr = $("<tr data-toggle='modal' data-target='#myExceptionModal' aria-expanded='false' aria-controls='collapse" + i + "' class='collapsed'>" +
                    "<td><b>" + data.logs[i].source + "</b></td><td>" + data.logs[i].message + "</td><td>" + data.logs[i].typeOf + "</td><td>" + moment(data.logs[i].encounteredAt).format('LLL') + "</td></tr > ");
                tr.on('click', populateExceptionModal.bind(null, data.logs[i]));
                $('#tableBodyEx').append(tr);
            }
        }
    });
}
function getInCodeLogs(searchString = "") {
    $.ajax({
        type: "GET",
        url: "/WTCHDwatchpage/Logs?pageNumber=" + inCodePageIndex + "&searchString=" + searchString,
        context: document.body,
        success: function (data) {
            var totalPages = data.totalPages === 0 ? 1 : data.totalPages
            $('#inCodePageMap').text("Page " + data.pageIndex + " of " + totalPages);
            if (data.hasNext === false) {
                $('#inCodeNext').hide();
            }
            else {
                $('#inCodeNext').show();

            }

            if (data.hasPrevious === false) {
                $('#inCodePrev').hide();
            }
            else {
                $('#inCodePrev').show();
            }

            for (var i = 0; i < data.logs.length; i++) {
                const tr = $("<tr aria-expanded='false' aria-controls='collapse" + i + "' class='collapsed'>" +
                    "<td><b>" + moment(data.logs[i].timestamp).format('LLL') + "</b></td><td>" + data.logs[i].callingFrom + "\nLine:" + data.logs[i].lineNumber + "</td><td>" + data.logs[i].callingMethod + "</td><td>" + data.logs[i].message + "</td></tr > ");

                $('#tableBodyInCode').append(tr);
            }
        }
    });
}


function clearLogs() {
    $.ajax({
        type: "POST",
        url: "/WTCHDwatchpage/ClearLogs",
        success: function (data) {
            window.location.reload()
        }
    });
}

function search() {
    pageIndex = 1;
    $('#tableBody').empty();
    var ss = $('#searchString').val();
    getLogs(ss, $('#myVerbDropDown').val() == "ALL" ? "" : $('#myVerbDropDown').val(), $('#myStatusCodeDropDown').val() == "ALL" ? "" : $('#myStatusCodeDropDown').val());
}

function exSearch() {
    exPageIndex = 1;
    $('#tableBodyEx').empty();
    var ss = $('#exSearchString').val();
    getExceptionLogs(ss);
}

function inCodeSearch() {
    inCodePageIndex = 1;
    $('#tableBodyInCode').empty();
    var ss = $('#inCodeSearchString').val();
    getInCodeLogs(ss);
}

function filterVerb(verbString) {
    pageIndex = 1;
    $('#tableBody').empty();
    getLogs($('#searchString').val(), verbString, $('#myStatusCodeDropDown').val() == "ALL" ? "" : $('#myStatusCodeDropDown').val());
}

function filterStatusCode(statusCodeString) {
    pageIndex = 1;
    $('#tableBody').empty();
    getLogs($('#searchString').val(), $('#myVerbDropDown').val() == "ALL" ? "" : $('#myVerbDropDown').val(), statusCodeString);
}

function nextPage() {
    ++pageIndex;
    $('#tableBody').empty();
    getLogs($('#searchString').val(), $('#myVerbDropDown').val() == "ALL" ? "" : $('#myVerbDropDown').val(), $('#myStatusCodeDropDown').val() == "ALL" ? "" : $('#myStatusCodeDropDown').val());
}

function prevPage() {
    if (pageIndex > 1)
        --pageIndex;
    $('#tableBody').empty();
    getLogs($('#searchString').val(), $('#myVerbDropDown').val() == "ALL" ? "" : $('#myVerbDropDown').val(), $('#myStatusCodeDropDown').val() == "ALL" ? "" : $('#myStatusCodeDropDown').val());
}

function exNextPage(e) {
    ++exPageIndex;
    $('#tableBodyEx').empty();
    getExceptionLogs($('#searchString').val());
}

function exPrevPage(e) {
    if (exPageIndex > 1)
        --exPageIndex;
    $('#tableBodyEx').empty();
    getExceptionLogs($('#searchString').val());
}

function inCodeNextPage(e) {
    ++inCodePageIndex;
    $('#tableBodyInCode').empty();
    getInCodeLogs($('#searchString').val());
}

function backToList(e) {
    pageIndex = 1;
    $('#tableBody').empty();
    $('#tableBodyEx').empty();
    $('#tableBodyInCode').empty();
    getLogs();
    getExceptionLogs();
    getInCodeLogs();

    $('#searchString').val("");
    $('#exSearchString').val("");
    $('#inCodeSearchString').val("");
    $('#myVerbDropDown').val("ALL");
    $('#myStatusCodeDropDown').val("ALL");

    e = e || window.event;
    e.preventDefault();
}

function logOut() {
    sessionStorage.clear();
    window.location.reload();
}

function login(event) {
    var form = document.querySelector('form')
    form.reportValidity()
    if (form.checkValidity()) {
        $.ajax({
            type: "POST",
            url: "/WTCHDwatchpage/Auth",
            data: {
                username: $("#uname").val(),
                password: $("#psw").val()
            },
            context: document.body,
            success: function (data) {
                logged(data);
            }
        });
    }
    event.preventDefault();
}

function autologin() {
    $.ajax({
        type: "GET",
        url: "/WTCHDwatchpage/AuthAuto",
        data: {},
        context: document.body,
        success: function (data) {
            if (logged(data) === false)
                $('#myLoginModal').modal('show');
        },
        error: function () {
            $('#myLoginModal').modal('show');
        }
    });
}

function logged(ok) {
    if (ok === true) {
        sessionStorage.setItem("newloggedIn", generateUUID());
        sessionStorage.setItem("loggedIn", sessionStorage.getItem("newloggedIn"));
        window.location.reload();
        return true;
    } else {
        sessionStorage.setItem("loggedIn", null);
        return false;
    }
}

function populateModal(data) {
    $('#host').text(data.host);
    $('#path').text(data.path);
    $('#query').text(data.queryString);
    $('#method').text(data.method);
    $('#ip').text(data.ipAddress);
    $('#statusCode').text(data.responseStatus);
    $('#time').text(data.timeSpent);
    $('#startTime').text(moment(data.startTime).format('LLL'));

    $('#reqHd').text(data.requestHeaders);
    $('#reqBody').text(data.requestBody);
    $('#reqType').text(data.requestContentType);
    $('#reqLength').text(data.requestContentLength);

    $('#resHd').text(data.responseHeaders);
    $('#resBody').text(data.responseBody);
    $('#resType').text(data.responseContentType);
    $('#resLength').text(data.responseContentLength);


    if (data.responseBody != null && data.responseBody !== "") {
        document.getElementById("resBodyCopy").style.display = "block";
        var element = document.getElementById("resBody");
        if (data.responseBody.includes("{")) {
            var obj = JSON.parse(element.innerText);
            element.innerHTML = JSON.stringify(obj, undefined, 2);
        }
    } else {
        document.getElementById("resBodyCopy").style.display = "none";
    }
    if (data.requestBody != null && data.requestBody !== "") {
        document.getElementById("reqBodyCopy").style.display = "block";
        var element2 = document.getElementById("reqBody");
        if (data.requestBody.includes("{")) {
            var obj2 = JSON.parse(element2.innerText);
            element2.innerHTML = JSON.stringify(obj2, undefined, 2);
        }
    } else {
        document.getElementById("reqBodyCopy").style.display = "none";
    }
}

function populateExceptionModal(data) {
    $('#exMessage').text(data.message);
    $('#exTypeOf').text(data.typeOf);
    $('#exSource').text(data.source);
    $('#exTime').text(moment(data.encounteredAt).format('LLL'));
    $('#exStackTrace').text(data.stackTrace);
    $('#exReqBody').text(data.requestBody);
    $('#exPath').text(data.path);
    $('#exMethod').text(data.method);
    $('#exQuery').text(data.queryString);

    if (data.stackTrace != null && data.stackTrace !== "") {
        document.getElementById("exStackTraceCopy").style.display = "block";
        var element = document.getElementById("exStackTrace");
        if (data.stackTrace.includes("{")) {
            var obj = JSON.parse(element.innerText);
            element.innerHTML = JSON.stringify(obj, undefined, 2);
        }
    } else {
        document.getElementById("exReqBodyCopy").style.display = "none";
    }
    if (data.requestBody != null && data.requestBody !== "") {
        document.getElementById("exReqBodyCopy").style.display = "block";
        var element2 = document.getElementById("exReqBody");
        if (data.requestBody.includes("{")) {
            var obj2 = JSON.parse(element2.innerText);
            element2.innerHTML = JSON.stringify(obj2, undefined, 2);
        }
    } else {
        document.getElementById("exReqBodyCopy").style.display = "none";
    }
}

function generateUUID() { // Public Domain/MIT
    var d = new Date().getTime();//Timestamp
    var d2 = ((typeof performance !== 'undefined') && performance.now && (performance.now() * 1000)) || 0;//Time in microseconds since page-load or 0 if unsupported
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        var r = Math.random() * 16;//random number between 0 and 16
        if (d > 0) {//Use timestamp until depleted
            r = (d + r) % 16 | 0;
            d = Math.floor(d / 16);
        } else {//Use microseconds since page-load if supported
            r = (d2 + r) % 16 | 0;
            d2 = Math.floor(d2 / 16);
        }
        return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16);
    });
}

function sleep(time) {
    return new Promise((resolve) => setTimeout(resolve, time));
}

function copyToClipboard(e, id, tag) {
    var copyText = document.getElementById(id).innerHTML;
    navigator.clipboard.writeText(copyText);

    /* Alert the copied text */
    document.getElementById(tag).innerHTML = "Copied!";

    sleep(1500).then(() => {
        document.getElementById(tag).innerHTML = "Copy";
    });
    e = e || window.event;
    e.preventDefault();
}

function makeActive(id, removeId, removeNextId) {
    document.getElementById(id).style.textDecoration = "underline";
    document.getElementById(removeId).style.textDecoration = "none";
    document.getElementById(removeNextId).style.textDecoration = "none";
}
