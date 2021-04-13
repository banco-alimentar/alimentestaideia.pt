$(function () {
    $.ajax("DonationHistory?handler=DataTableData", { method: "get" })
        .then(function (data) {
            $('#donationHistory').dataTable({
                orderCellsTop: true,
                autoWidth: true,
                data: data,
                columns: [
                    { "data": "Id" },
                    { "data": "DonationDate" },
                    { "data": "FoodBank" },
                    { "data": "DonationAmount", render: $.fn.dataTable.render.number(',', '.', 2, '', ' €') },
                    {
                        "data": "Payments",
                        "render": function (data, type, row, meta) {
                            result = '';
                            if (Array.isArray(data)) {
                                if (data.length > 0) {
                                    for (i = 0; i < data.length; i++) {
                                        result += '<div>' + data[i].PaymentType + '</div><a class="btn btn-primary" target="_blank" href="/Payment?publicDonationId=' + data[i].PaymentItemId + '" >@Localizer["CompleteDonation"]</a>';
                                    }                                    
                                }
                            }

                            return result;
                        }
                    },
                    { "data": "PaymentStatus" },
                    {
                        "data": "PublicId",
                        "render": function (data, type, row, meta) {
                            if (row['PaymentStatus'] === "Payed") {
                                return '<a class="btn btn-primary" target="_blank" href="/Identity/Account/Manage/GenerateInvoice?publicDonationId=' + data + '" >@Localizer["Invoice"]</a>';
                            }
                            else {
                                return '';
                            }
                        }
                    },
                    {
                        "data": "DonationId",
                        "render": function (data, type, row, meta) {
                            if (row['PaymentStatus'] !== "Payed") {
                                return '<a class="btn btn-primary" target="_blank" href="/Payment?publicDonationId=' + row['PublicId'] + '" >@Localizer["CompleteDonation"]</a>';
                            }
                            else {
                                return '';
                            }
                        }
                    },
                ]
            });
        });
});