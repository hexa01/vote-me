async function AjaxCall(url, data) {

    var Dropdown = JSON.stringify(data);
    var result;
    await $.ajax({
        type: "Post",
        url: url,
        timeout: 999999999,
        data: Dropdown,
        dataType: "Text",
        contentType: "application/json;charset=utf-8",
        cache: false,
        async: true,
        
        success: function (responce) {
            result = responce;
        },
        error: function (error) {
            alert("Error:Server Error");
        }
    });
    return result;
};
