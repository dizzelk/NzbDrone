﻿'use strict';

define(['jquery'],function ($) {

    $.fn.autoComplete = function (resource) {
        $(this).typeahead({
            source   : function (filter, callback) {
                $.ajax({
                    url     : window.NzbDrone.ApiRoot + resource,
                    dataType: 'json',
                    type    : 'GET',
                    data    : { query: filter },
                    success : function (data) {
                        callback(data);
                    }
                });
            },
            minLength: 3,
            items    : 20
        });
    };
});
