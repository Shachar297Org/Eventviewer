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

                working = false;
            }
            else {

                $('#fromResult').text($("#searchForm #from").val());
                $('#toResult').text($("#to").val());

                $('#datesRange').show();

                if (($("#deviceSerialNumber").val() != '') && ($("#deviceType").val() != '')) {
                    $('#dsnResult').text($("#deviceSerialNumber").val());
                    $('#dtResult').text($("#deviceType").val());

                    $('#deviceName').removeClass('d-none');
                } else {
                    $('#deviceName').addClass('d-none');
                }

                table.ajax.reload(null, true);

                working = false;
            }

        });
    });

    var table = $(".events-datatable").DataTable({
        "processing": true,
        "serverSide": true,
        "filter": true,
        "ajax": {
            "url": "/getEvents/",
            "type": "POST",
            "datatype": "json",
            "data": {
                "id": window.location.pathname.split('/').pop()
            }
        },
        //"columnDefs": [{
        //    "targets": [0],
        //    "visible": true,
        //    "searchable": false
        //}],
        "columns": [
            //{ "data": "id", "name": "Id", "autoWidth": true },
            { "data": "deviceSerialNumber", "name": "DeviceSerialNumber", "autoWidth": true },
            { "data": "deviceType", "name": "DeviceType", "autoWidth": true },
            { "data": "entryKey", "name": "EntryKey", "autoWidth": true },
            { "data": "entryValue", "name": "EntryValue", "autoWidth": true },
            { "data": "entryTimestamp", "name": "EntryTimestamp", "autoWidth": true },

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

