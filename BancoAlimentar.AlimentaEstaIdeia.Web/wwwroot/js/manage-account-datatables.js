(function ($) {
    'use strict';

    function readJsonConfig(elementId) {
        var node = document.getElementById(elementId);
        if (!node || !node.textContent) {
            return null;
        }

        try {
            return JSON.parse(node.textContent);
        } catch (e) {
            return null;
        }
    }

    function initDonationHistory() {
        var tableSelector = '#donationHistory';
        if ($(tableSelector).length === 0 || $.fn.DataTable.isDataTable(tableSelector)) {
            return;
        }

        var i18n = readJsonConfig('donation-history-i18n') || {};
        $(tableSelector).DataTable({
            processing: true,
            serverSide: true,
            ajax: {
                url: '?handler=DataTableData',
                type: 'GET',
                dataSrc: 'data'
            },
            language: {
                emptyTable: i18n.emptyTable || '',
                processing: i18n.processing || '',
                lengthMenu: i18n.lengthMenu || '',
                zeroRecords: i18n.zeroRecords || '',
                search: i18n.search || '',
                info: i18n.info || '',
                infoEmpty: i18n.infoEmpty || '',
                paginate: {
                    first: i18n.paginateFirst || '',
                    previous: i18n.paginatePrevious || '',
                    next: i18n.paginateNext || '',
                    last: i18n.paginateLast || ''
                }
            },
            orderCellsTop: true,
            autoWidth: false,
            pageLength: 10,
            lengthMenu: [[10, 25, 50, 100], [10, 25, 50, 100]],
            order: [[1, 'desc']],
            initComplete: function () {
                var $searchInput = $('#donationHistory_filter input[type="search"]');
                if ($searchInput.length && i18n.searchHelpTooltip) {
                    $searchInput.attr('title', i18n.searchHelpTooltip);
                }
            },
            columns: [
                { data: 'Id' },
                {
                    data: 'DonationDate',
                    render: function (data, type) {
                        if (!data) {
                            return '';
                        }

                        if (type !== 'display' && type !== 'filter') {
                            return data;
                        }

                        var parsed = moment(data, moment.ISO_8601, true);
                        if (!parsed.isValid()) {
                            parsed = moment(data);
                        }

                        return parsed.isValid() ? parsed.format('DD/MM/YYYY') : '';
                    }
                },
                { data: 'FoodBank' },
                { data: 'DonationAmount', render: $.fn.dataTable.render.number(',', '.', 2, '', ' €') },
                {
                    data: 'Payments',
                    render: function (data) {
                        var labels = i18n.paymentTypes || {};
                        var result = '';
                        if (Array.isArray(data) && data.length > 0) {
                            for (var i = 0; i < data.length; i++) {
                                var paymentType = data[i].PaymentType;
                                if (labels.hasOwnProperty(paymentType)) {
                                    paymentType = labels[paymentType];
                                }

                                result += '<div>' + paymentType + '</div>';
                            }
                        }

                        return result;
                    }
                },
                {
                    data: 'PaymentStatus',
                    render: function (data) {
                        var labels = i18n.paymentStatuses || {};
                        if (labels.hasOwnProperty(data)) {
                            return labels[data];
                        }

                        return data;
                    }
                },
                {
                    data: 'SubscriptionPublicId',
                    render: function (data) {
                        if (data == null) {
                            return '';
                        }

                        return '<a class="btn btn-primary" style="min-width: 120px" target="_blank" href="/Identity/Account/Manage/Subscriptions/Details?PublicId=' + data + '">' + (i18n.details || 'Details') + '</a>';
                    }
                },
                {
                    data: 'PublicId',
                    render: function (data, type, row) {
                        if (row.PaymentStatus === 'Payed') {
                            if ((row.Nif !== null && row.Nif !== '000000000') || (row.UsersNif !== null && row.UsersNif !== '000000000')) {
                                return '<p class="alert alert-primary text-center" role="alert"><a class="text-primary" target="_blank" href="/Identity/Account/Manage/GenerateInvoice?publicDonationId=' + data + '">' + (i18n.donationInvoice || '') + '</a></p>';
                            }

                            return '<p class="alert alert-warning text-center" role="alert"><a class="text-warning" href="/Identity/Account/Manage">' + (i18n.invalidNif || '') + '</a></p>';
                        }

                        return '<p class="alert alert-warning text-center" role="alert">' + (i18n.paymentPendingStatus || '') + '</p>';
                    }
                },
                {
                    data: 'DonationId',
                    render: function (data, type, row) {
                        if (row.PaymentStatus !== 'Payed' && row.SubscriptionPublicId == null) {
                            return '<a class="btn btn-primary" target="_blank" href="/Payment?publicDonationId=' + row.PublicId + '">' + (i18n.completeDonation || '') + '</a>';
                        }

                        return '';
                    }
                }
            ]
        });
    }

    function initSubscriptions() {
        var tableSelector = '#subscriptions';
        if ($(tableSelector).length === 0 || $.fn.dataTable.isDataTable(tableSelector)) {
            return;
        }

        var i18n = readJsonConfig('subscriptions-i18n') || {};
        var culture = $(tableSelector).data('culture') || 'EN';

        $.ajax('?handler=DataTableData', { method: 'get', dataType: 'json' })
            .then(function (data) {
                $(tableSelector).dataTable({
                    language: { url: '/resources/dataTable.' + culture + '.json' },
                    orderCellsTop: true,
                    autoWidth: true,
                    data: data,
                    columns: [
                        { data: 'Id' },
                        {
                            data: 'Created',
                            render: function (data) {
                                return data ? moment(new Date(data)).format('DD/MM/YYYY') : '';
                            }
                        },
                        {
                            data: 'ExpirationTime',
                            render: function (data) {
                                return data ? moment(new Date(data)).format('DD/MM/YYYY') : '';
                            }
                        },
                        {
                            data: 'StartTime',
                            render: function (data) {
                                return data ? moment(new Date(data)).format('DD/MM/YYYY') : '';
                            }
                        },
                        { data: 'SubscriptionType' },
                        {
                            data: 'Status',
                            render: function (data) {
                                var labels = i18n.statuses || {};
                                if (labels.hasOwnProperty(data)) {
                                    return labels[data];
                                }

                                return data;
                            }
                        },
                        {
                            data: 'Frequency',
                            render: function (data) {
                                var labels = i18n.frequencies || {};
                                if (labels.hasOwnProperty(data)) {
                                    return labels[data];
                                }

                                return data;
                            }
                        },
                        { data: 'DonationAmount', render: $.fn.dataTable.render.number(',', '.', 2, '', ' €') },
                        {
                            data: 'PublicId',
                            render: function (data) {
                                return '<a class="btn btn-primary" href="/Identity/Account/Manage/Subscriptions/Details?PublicId=' + data + '">' + (i18n.details || 'Details') + '</a>';
                            }
                        }
                    ]
                });
            });
    }

    $(function () {
        if (typeof $.fn.dataTable !== 'function' && typeof $.fn.DataTable !== 'function') {
            return;
        }

        initDonationHistory();
        initSubscriptions();
    });
})(jQuery);
