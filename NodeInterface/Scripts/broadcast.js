$(document).ready(function () {
    $("#AjaxformId").submit(function (event) {
        var dataString;
        event.preventDefault();
        event.stopImmediatePropagation();
        var action = $("#AjaxformId").attr("action");
        // Setting.  
        dataString = new FormData($("#AjaxformId").get(0));
        contentType = false;
        processData = false;
        $.ajax({
            type: "POST",
            url: action,
            data: dataString,
            dataType: "json",
            contentType: contentType,
            processData: processData,
            success: function (result) {
                // Result.  
                onAjaxRequestSuccess(result);
            },
            error: function (jqXHR, textStatus, errorThrown) {
                //do your own thing  
                alert("fail");
            }
        });
    }); //end .submit()  
});

var onAjaxRequestSuccess = function (result) {
    if (result.EnableError) {
        // Setting.  
        alert(result.ErrorMsg);
    } else if (result.EnableSuccess) {
        // Setting.  
        alert(result.SuccessMsg);
        // Resetting form.  
        $('#AjaxformId').get(0).reset();
    }
}  