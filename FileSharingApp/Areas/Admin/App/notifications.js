﻿

var connection = new SignalR.HubConnectionBuilder().withUrl("/notify").build();

connection.on("RecievedNotification", function (msg) {

    $("#notification-list").prepend(`

        <div class="dropdown-divider"></div>
        <a href="#" class="dropdown-item">
            <i class="fas fa-envelope mr-2"></i> ` + msg + `
            <span class="float-right text-muted text-sm">3 mins</span>
        </a>

    `);

});

connection.start();