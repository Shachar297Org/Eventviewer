$(document).ready(function () {
    var year = new Date().getFullYear()
    $("#year").html(year + " ©");

    var xhr;
    var working = false;

    const id = window.location.pathname.split('/').pop();

    $("#searchForm").submit(function (e) {

        e.preventDefault();

        if (working) {
            xhr.abort();
            working = false;

            $('#loading').addClass('d-none');
            $('#loader').removeClass('d-none');

            return;
        }

        //console.log('Doing ajax submit');
        working = true;

        var formAction = $(this).attr("action");
        var fdata = new FormData(this);
        fdata.append("id", id);

        $('#loader').addClass('d-none');
        $('#loading').removeClass('d-none');

        $('#error').text("");

        xhr = $.ajax({
            type: 'post',
            url: formAction,
            data: fdata,
            processData: false,
            contentType: false
        }).done(function (result) {
            // do something with the result now
            //console.log(result);

            $('#loading').addClass('d-none');
            $('#loader').removeClass('d-none');

            if (!result.success) {
                /*if (result.errorCode == 0) {
                    $.ajax({
                        url: '/EventViewer/Error',
                        data: { 'errorMessage': result.errorMessage, 'errorCode': result.errorCode },
                        type: "get",
                        cache: false,
                        success: function (savingStatus) {
                            //$("#hdnOrigComments").val($('#txtComments').val());
                            //$('#lblCommentsNotification').text(savingStatus);
                        },
                        error: function (xhr, ajaxOptions, thrownError) {
                            //$('#lblCommentsNotification').text("Error encountered while saving the comments.");
                        }
                    });
                }*/
                $('#error').text(result.errorMessage);

                $('#deviceName').addClass('d-none');
                $('#datesRange').addClass('d-none');
                $('#tocsv').addClass('d-none');

                working = false;
            }
            else {             

                if (result.type == "events") {
                    $('#events #fromResult').text($("#searchForm #from").val());
                    $('#events #toResult').text($("#to").val());

                    $('#events #datesRange').removeClass('d-none');

                    if (($("#deviceSerialNumber").val() != '') && ($("#deviceType").val() != '')) {
                        $('#events #dsnResult').text($("#deviceSerialNumber").val());
                        $('#events #dtResult').text($("#deviceType").val());

                        $('#events #deviceName').removeClass('d-none');
                        $('#events #tocsv').removeClass('d-none');
                    } else {
                        $('#events #deviceName').addClass('d-none');
                        $('#events #tocsv').addClass('d-none');
                    }  

                    $('#events-tab').click();
                    eventsTable.ajax.reload(null, true);
                } else {
                    $('#commands #fromResult').text($("#searchForm #from").val());
                    $('#commands #toResult').text($("#to").val());

                    $('#commands #datesRange').removeClass('d-none');

                    if (($("#deviceSerialNumber").val() != '') && ($("#deviceType").val() != '')) {
                        $('#commands #dsnResult').text($("#deviceSerialNumber").val());
                        $('#commands #dtResult').text($("#deviceType").val());

                        $('#commands #deviceName').removeClass('d-none');
                        $('#commands #tocsv').removeClass('d-none');
                    } else {
                        $('#commands #deviceName').addClass('d-none');
                        $('#commands #tocsv').addClass('d-none');
                    } 

                    $('#commands-tab').click();
                    commandsTable.ajax.reload(null, true);
                }              

                working = false;
            }

        });
    });

    $('a[data-toggle="tab"]').on('shown.bs.tab', function (e) {

        let target = e.target; // newly activated tab
        let previous = e.relatedTarget; // previous active tab

        if (target.id === "events-tab") {

            // Remove active class from previous elements
            if (previous.id === "commands-tab") {
                $('#commands').removeClass('show active');
                $('#commands').addClass('d-none');
            }
            // Add active class to target elements
            $('#events').addClass('show active');
            $('#events').removeClass('d-none');

            eventsTable.ajax.reload(null, true);
        }

        if (target.id === "commands-tab") {

            // Remove active class from previous elements
            if (previous.id === "events-tab") {
                $('#events').removeClass('show active');
                $('#events').addClass('d-none');
            }

            // Add active class to target elements
            $('#commands').addClass('show active');
            $('#commands').removeClass('d-none');

            commandsTable.ajax.reload(null, true);
        }

    })

    $(".generatecsv").click(function (e) {

        e.preventDefault();

        $('#csvgenpr').removeClass('d-none');
        $('#csvgen').addClass('d-none');

        var type = $('.tab-pane.show.active').attr("id");

        $.ajax({
            type: 'post',
            url: '/downloadDataAsCSV',
            datatype: "json",
            data: {
                "id": window.location.pathname.split('/').pop(),
                "type": type,
                "from": $("#fromResult").text(),
                "to": $("#toResult").text(),
                "deviceSN": $("#dsnResult").text(),
                "deviceType": $("#dtResult").text()
            },
            xhrFields: {
                responseType: 'blob'
            },
            beforeSend: function (xhr) {
                xhr.setRequestHeader("RequestVerificationToken",
                    $('input:hidden[name="__RequestVerificationToken"]').val());

            },
            success: function (data, textStatus, xhr) {
                $(' #csvgenpr').addClass('d-none');
                $(' #csvgen').removeClass('d-none');

                // check for a filename
                var filename = "";
                var disposition = xhr.getResponseHeader('Content-Disposition');
                if (disposition && disposition.indexOf('attachment') !== -1) {
                    var filenameRegex = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/;
                    var matches = filenameRegex.exec(disposition);
                    if (matches != null && matches[1]) filename = matches[1].replace(/['"]/g, '');
                    var a = document.createElement('a');
                    var url = window.URL.createObjectURL(data);
                    a.href = url;
                    a.download = filename;
                    document.body.append(a);
                    a.click();
                    a.remove();
                    window.URL.revokeObjectURL(url);
                }
                else {
                    console.log("CSV file does not exist");
                }
            },
            error: function (XMLHttpRequest, textStatus, errorThrown) {
                $('#csvgenpr').addClass('d-none');
                $('#csvgen').removeClass('d-none');
            },
            complete: function () {

            }
        });        

    });

    var eventsTable = $(".events-datatable").DataTable({
        "processing": true,
        "serverSide": true,
        "filter": true,
        "ajax": {
            "url": "/getData/",
            "type": "POST",
            "datatype": "json",
            "data": {
                "id": window.location.pathname.split('/').pop(),
                "type": "events"
            }
        },
        "autoWidth": false,
        "columnDefs": [
            { "width": "10%", "targets": 0 },
            { "width": "10%", "targets": 1 },
            { "width": "10%", "targets": 2 },
            { "width": "50%", "targets": 3 },
            { "width": "10%", "targets": 4 },
            { "width": "10%", "targets": 5 },
        ],
        "columns": [
            //{ "data": "id", "name": "Id", "autoWidth": true },
            { "data": "deviceSerialNumber", "name": "DeviceSerialNumber" },
            { "data": "deviceType", "name": "DeviceType" },
            { "data": "entryKey", "name": "EntryKey" },
            { "data": "entryValue", "name": "EntryValue" },
            { "data": "entryTimestamp", "name": "EntryTimestamp" },
            { "data": "localEntryTimestamp", "name": "LocalEntryTimestamp" }
        ]
    });

    var commandsTable = $(".commands-datatable").DataTable({
        "processing": true,
        "serverSide": true,
        "filter": true,
        "ajax": {
            "url": "/getData/",
            "type": "POST",
            "datatype": "json",
            "data": {
                "id": window.location.pathname.split('/').pop(),
                "type": "commands"
            }
        },
        "autoWidth": false,
        "columnDefs": [
            { "width": "10%", "targets": 0 },
            { "width": "10%", "targets": 1 },
            { "width": "10%", "targets": 2 },
            { "width": "10%", "targets": 3 },
            { "width": "40%", "targets": 4 },
            { "width": "10%", "targets": 5 },
            { "width": "10%", "targets": 6 },
        ],
        "columns": [
            { "data": "deviceSerialNumber", "name": "DeviceSerialNumber" },
            { "data": "deviceType", "name": "DeviceType" },
            { "data": "commandName", "name": "CommandName" },
            { "data": "entryKey", "name": "EntryKey" },
            { "data": "entryValue", "name": "EntryValue" },
            { "data": "timestamp", "name": "Timestamp" },
            { "data": "localEntryTimestamp", "name": "LocalEntryTimestamp" }
        ]
    });

    $.datetimepicker.setLocale('en');

    $('#from').datetimepicker({
        dayOfWeekStart: 1,
        lang: 'en'
    });
    $('#from').datetimepicker({ value: '2021/01/01 00:00', step: 10 });

    $('#to').datetimepicker({
        dayOfWeekStart: 1,
        lang: 'en'
    });
    $('#to').datetimepicker({ value: '@DateTime.Now.ToString("yyyy/MM/dd HH:mm")', step: 10 });

});

