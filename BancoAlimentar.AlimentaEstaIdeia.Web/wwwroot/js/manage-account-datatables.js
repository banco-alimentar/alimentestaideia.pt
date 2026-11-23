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
        var $table = $(tableSelector);
        var $error = $('#donation-history-load-error');
        var textRenderer = $.fn.dataTable.render.text();
        $table.on('xhr.dt', function (event, settings, json) {
            $error.text(json === null ? i18n.loadFailed : '').toggleClass('d-none', json !== null);
            if (json === null) {
                return true;
            }
        });

        $table.DataTable({
            processing: true,
            serverSide: true,
            dom: '<"account-history-toolbar"lf><"account-history-table-region table-responsive"t><"account-history-footer"ip>r',
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
                $(tableSelector).closest('.account-history-table-region').attr({
                    tabindex: '0',
                    role: 'region',
                    'aria-labelledby': 'donation-history-title'
                });
                var $searchInput = $('#donationHistory_filter input[type="search"]');
                if ($searchInput.length && i18n.searchHelpTooltip) {
                    $searchInput.attr('title', i18n.searchHelpTooltip);
                }
            },
            columns: [
                // Preserve the server's sort-column indexes while hiding the row counter.
                { data: 'Id', orderable: false, visible: false },
                {
                    data: 'DonationDate',
                    width: '15%',
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
                { data: 'FoodBank', width: '19%', render: textRenderer },
                {
                    data: 'DonationAmount',
                    width: '14%',
                    className: 'account-history-amount',
                    render: $.fn.dataTable.render.number(',', '.', 2, '', ' €')
                },
                {
                    data: 'Payments',
                    width: '16%',
                    orderable: false,
                    render: function (data) {
                        var labels = i18n.paymentTypes || {};
                        var result = '';
                        if (Array.isArray(data) && data.length > 0) {
                            for (var i = 0; i < data.length; i++) {
                                var paymentType = data[i].PaymentType;
                                if (labels.hasOwnProperty(paymentType)) {
                                    paymentType = labels[paymentType];
                                }

                                result += '<div>' + textRenderer.display(paymentType) + '</div>';
                            }
                        }

                        return result;
                    }
                },
                {
                    data: 'PaymentStatus',
                    width: '16%',
                    render: function (data, type) {
                        var labels = i18n.paymentStatuses || {};
                        var label = labels.hasOwnProperty(data) ? labels[data] : data;
                        if (type !== 'display') {
                            return label;
                        }

                        var statusClass = data === 'Payed' ? 'success' : data === 'ErrorPayment' ? 'error' : 'pending';
                        return '<span class="account-history-status account-history-status-' + statusClass + '">' + textRenderer.display(label) + '</span>';
                    }
                },
                {
                    data: 'PublicId',
                    width: '20%',
                    orderable: false,
                    render: function (data, type, row) {
                        if (type !== 'display') {
                            return data;
                        }

                        var actions = '';
                        if (row.SubscriptionPublicId != null) {
                            actions += '<a class="btn btn-sm btn-outline-primary" target="_blank" rel="noopener" href="/Identity/Account/Manage/Subscriptions/Details?PublicId=' + encodeURIComponent(row.SubscriptionPublicId) + '">' + textRenderer.display(i18n.subscription || '') + '</a>';
                        }

                        if (row.PaymentStatus === 'Payed') {
                            if ((row.Nif !== null && row.Nif !== '000000000') || (row.UsersNif !== null && row.UsersNif !== '000000000')) {
                                actions += '<a class="btn btn-sm btn-outline-primary" target="_blank" rel="noopener" href="/Identity/Account/Manage/GenerateInvoice?publicDonationId=' + encodeURIComponent(data) + '">' + textRenderer.display(i18n.donationInvoice || '') + '</a>';
                            } else {
                                actions += '<a class="btn btn-sm donation-history-nif" href="/Identity/Account/Manage">' + textRenderer.display(i18n.invalidNif || '') + '</a>';
                            }
                        } else if (row.SubscriptionPublicId == null) {
                            actions += '<a class="btn btn-sm btn-primary" target="_blank" rel="noopener" href="/Payment?publicDonationId=' + encodeURIComponent(data) + '">' + textRenderer.display(i18n.completeDonation || '') + '</a>';
                        }

                        return '<div class="account-history-actions">' + actions + '</div>';
                    }
                }
            ],
            drawCallback: function () {
                $('#donationHistory .donation-history-nif').attr('title', i18n.invalidNifTooltip || '');
            }
        });
    }

    function initSubscriptions() {
        var tableSelector = '#subscriptions';
        if ($(tableSelector).length === 0 || $.fn.dataTable.isDataTable(tableSelector)) {
            return;
        }

        var i18n = readJsonConfig('subscriptions-i18n') || {};
        var culture = $(tableSelector).data('culture') || 'EN';
        var $table = $(tableSelector);
        var $loading = $('#subscriptions-loading');
        var $error = $('#subscriptions-load-error');
        var textRenderer = $.fn.dataTable.render.text();

        function formatDate(value) {
            if (!value) {
                return '';
            }

            var parsed = moment(value, moment.ISO_8601, true);
            return parsed.isValid() ? parsed.format('DD/MM/YYYY') : '';
        }

        $loading.show();
        $error.addClass('d-none').text('');

        $.ajax('?handler=DataTableData', { method: 'get', dataType: 'json' })
            .done(function (data) {
                $loading.hide();
                $table.attr('aria-busy', 'false');
                if (!Array.isArray(data)) {
                    $error.text(i18n.loadFailed || '').removeClass('d-none').show();
                    return;
                }

                $('#subscriptions-total-count').text(data.length);
                $('#subscriptions-active-count').text(data.filter(function (row) { return row.Status === 'Active'; }).length);
                $('#subscriptions-inactive-count').text(data.filter(function (row) { return row.Status === 'Inactive'; }).length);
                $('#subscriptions-summary').removeClass('d-none');
                $table.dataTable({
                    language: { url: '/resources/dataTable.' + culture + '.json' },
                    dom: '<"account-history-toolbar"lf><"account-history-table-region table-responsive"t><"account-history-footer"ip>r',
                    orderCellsTop: true,
                    autoWidth: false,
                    order: [[1, 'desc']],
                    data: data,
                    initComplete: function () {
                        $table.closest('.account-history-table-region').attr({
                            tabindex: '0',
                            role: 'region',
                            'aria-labelledby': 'subscriptions-title'
                        });
                    },
                    columns: [
                        { data: 'Id', visible: false, orderable: false },
                        {
                            data: 'Created',
                            width: '30%',
                            render: function (data, type, row) {
                                if (type === 'sort' || type === 'type') {
                                    return data || '';
                                }

                                var dates = [[i18n.starts, row.StartTime], [i18n.ends, row.ExpirationTime]];
                                if (type === 'filter') {
                                    return [formatDate(data)].concat(dates.map(function (date) {
                                        var value = formatDate(date[1]);
                                        return value ? (date[0] || '') + ' ' + value : '';
                                    })).join(' ');
                                }

                                var result = textRenderer.display(formatDate(data));
                                dates.forEach(function (date) {
                                    var value = formatDate(date[1]);
                                    if (value) {
                                        result += '<div class="subscription-history-date"><span class="subscription-history-date-label">' + textRenderer.display(date[0] || '') + '</span>' + value + '</div>';
                                    }
                                });

                                return result;
                            }
                        },
                        {
                            data: 'Frequency',
                            width: '15%',
                            render: function (data) {
                                var labels = i18n.frequencies || {};
                                return textRenderer.display(labels.hasOwnProperty(data) ? labels[data] : data);
                            }
                        },
                        {
                            data: 'DonationAmount',
                            width: '14%',
                            className: 'account-history-amount',
                            render: $.fn.dataTable.render.number(',', '.', 2, '', ' €')
                        },
                        {
                            data: 'SubscriptionType',
                            width: '16%',
                            render: function (data) {
                                var labels = i18n.paymentTypes || {};
                                return textRenderer.display(labels.hasOwnProperty(data) ? labels[data] : data);
                            }
                        },
                        {
                            data: 'Status',
                            width: '13%',
                            render: function (data, type) {
                                var labels = i18n.statuses || {};
                                var label = labels.hasOwnProperty(data) ? labels[data] : data;
                                if (type !== 'display') {
                                    return label;
                                }

                                var statusClass = data === 'Active' ? 'success' : data === 'Inactive' ? 'inactive' : data === 'Error' ? 'error' : 'pending';
                                return '<span class="account-history-status account-history-status-' + statusClass + '">' + textRenderer.display(label) + '</span>';
                            }
                        },
                        {
                            data: 'PublicId',
                            width: '12%',
                            orderable: false,
                            render: function (data, type) {
                                if (type !== 'display') {
                                    return data;
                                }

                                return '<div class="account-history-actions"><a class="btn btn-sm btn-outline-primary" href="/Identity/Account/Manage/Subscriptions/Details?PublicId=' + encodeURIComponent(data) + '">' + textRenderer.display(i18n.details || '') + '</a></div>';
                            }
                        }
                    ]
                });
            })
            .fail(function () {
                $loading.hide();
                $error.text(i18n.loadFailed || '').removeClass('d-none').show();
                $table.attr('aria-busy', 'false');
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
