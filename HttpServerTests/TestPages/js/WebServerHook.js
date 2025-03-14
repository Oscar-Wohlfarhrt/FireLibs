function getWebSocketUrl(uri) {
    var xhr = new XMLHttpRequest();
    xhr.open("GET", uri, false);
    xhr.send();

    if (xhr.status === 200)
        return xhr.responseText;
    
    return '';
}

function getWebSocketUrl2(uri) {
    return fetch(uri).then(function(res){
        return res.ok ? res.text() : '';
    });
}