﻿'use strict';

define(
    [
        'vent',
        'marionette'
    ], function (vent, Marionette) {
        return Marionette.ItemView.extend({
            template: 'Series/Index/List/ItemTemplate',

            ui: {
                'progressbar': '.progress .bar'
            },

            events: {
                'click .x-edit'  : 'editSeries',
                'click .x-remove': 'removeSeries'
            },

            editSeries: function () {
                vent.trigger(vent.Commands.EditSeriesCommand, {series: this.model});
            },

            removeSeries: function () {
                vent.trigger(vent.Commands.DeleteSeriesCommand, {series: this.model});
            }
        });
    });
