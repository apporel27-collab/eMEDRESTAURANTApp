// Optimized Estimation JavaScript
$(document).ready(function() {
    let menuData = [];
    
    // Initialize page
    loadMenuItems();
    
    // Event handlers
    $('#menuItemSearch').on('input', debounce(filterItems, 300));
    $('#categoryFilter').on('change', filterItems);
    $('#estimateBtn').on('click', generateEstimate);
    $('#clearEstimateBtn').on('click', clearEstimate);
    $('#printEstimateBtn').on('click', printEstimate);
    $(document).on('change', '.qty-input', validateQuantity);
    
    function debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }
    
    function loadMenuItems() {
        showLoading();
        $.get('/Menu/Index')
            .done(function(data) {
                const items = $(data).find('.table tbody tr');
                menuData = [];
                items.each(function() {
                    const row = $(this);
                    menuData.push({
                        plu: row.find('td:eq(0)').text().trim(),
                        name: row.find('td:eq(2)').text().trim(),
                        category: row.find('td:eq(3)').text().trim(),
                        price: parseFloat(row.find('td:eq(4)').text().replace(/[^\d.]/g, '')) || 0
                    });
                });
                renderMenuItems(menuData);
                populateCategories();
            })
            .fail(() => showError('Failed to load menu items'));
    }
    
    function renderMenuItems(items) {
        const tbody = $('#menuItemsTableBody');
        tbody.empty();
        
        if (items.length === 0) {
            tbody.html('<tr><td colspan="5" class="text-center text-muted">No items found</td></tr>');
            return;
        }
        
        items.forEach(item => {
            tbody.append(`
                <tr data-price="${item.price}">
                    <td>${item.name}</td>
                    <td>₹${item.price.toFixed(2)}</td>
                    <td>${item.category}</td>
                    <td>${item.plu}</td>
                    <td><input type="number" class="form-control form-control-sm qty-input" min="0" value="0"></td>
                </tr>
            `);
        });
    }
    
    function populateCategories() {
        const categories = [...new Set(menuData.map(item => item.category))];
        const select = $('#categoryFilter');
        select.empty().append('<option value="all">All Categories</option>');
        categories.forEach(cat => select.append(`<option value="${cat.toLowerCase()}">${cat}</option>`));
    }
    
    function filterItems() {
        const search = $('#menuItemSearch').val().toLowerCase();
        const category = $('#categoryFilter').val();
        
        let filtered = menuData;
        if (search) filtered = filtered.filter(item => item.name.toLowerCase().includes(search));
        if (category !== 'all') filtered = filtered.filter(item => item.category.toLowerCase() === category);
        
        renderMenuItems(filtered);
    }
    
    function generateEstimate() {
        const items = [];
        let total = 0;
        
        $('#menuItemsTable tbody tr').each(function() {
            const qty = parseInt($(this).find('.qty-input').val()) || 0;
            if (qty > 0) {
                const name = $(this).find('td:eq(0)').text();
                const price = parseFloat($(this).data('price')) || 0;
                const subtotal = qty * price;
                items.push({name, price, qty, subtotal});
                total += subtotal;
            }
        });
        
        renderEstimation(items, total);
    }
    
    function renderEstimation(items, total) {
        const tbody = $('#estimationTableBody');
        tbody.empty();
        
        if (items.length === 0) {
            tbody.html('<tr><td colspan="4" class="text-center text-muted">No items selected</td></tr>');
            $('.estimation-section').hide();
            return;
        }
        
        items.forEach(item => {
            tbody.append(`
                <tr>
                    <td>${item.name}</td>
                    <td class="text-end">₹${item.price.toFixed(2)}</td>
                    <td class="text-center">${item.qty}</td>
                    <td class="text-end">₹${item.subtotal.toFixed(2)}</td>
                </tr>
            `);
        });
        
    const tax = total * 0.05;
    const grand = total + tax;
        
        tbody.append(`
            <tr class="border-top subtotal-row"><td colspan="3" class="text-end fw-semibold">Subtotal</td><td class="text-end fw-semibold">₹${total.toFixed(2)}</td></tr>
            <tr class="gst-row"><td colspan="3" class="text-end small">GST (5%)</td><td class="text-end small">₹${tax.toFixed(2)}</td></tr>
            <tr class="total-row"><td colspan="3" class="text-end fw-bold">Grand Total</td><td class="text-end fw-bold">₹${grand.toFixed(2)}</td></tr>
        `);

        // Update side summary card
        $('#subtotalValue').text(`₹${total.toFixed(2)}`);
        $('#gstValue').text(`₹${tax.toFixed(2)}`);
        $('#grandTotalValue').text(`₹${grand.toFixed(2)}`);
        $('#estimateMeta').text(`${items.length} item(s) • Updated ${new Date().toLocaleTimeString()}`);
        
        $('.estimation-section').show();
        $('#printEstimateBtn, #clearEstimateBtn').show();
        showToast('Estimate generated successfully', 'success');
    }
    
    function clearEstimate() {
        $('.qty-input').val(0);
        $('#estimationTableBody').empty();
        $('.estimation-section').hide();
        showToast('Estimate cleared', 'info');
    }
    
    function printEstimate() {
        const date = new Date();
        const estimateNo = `EST-${date.getFullYear()}${(date.getMonth()+1).toString().padStart(2,'0')}${date.getDate().toString().padStart(2,'0')}-${Math.floor(Math.random()*1000)}`;
        const subtotal = $('#subtotalValue').text();
        const gst = $('#gstValue').text();
        const grand = $('#grandTotalValue').text();
        const meta = $('#estimateMeta').text();

        let rowsHtml = '';
        
        $('#estimationTableBody tr').each(function() {
            const tds = $(this).find('td');
            if (tds.length === 4) {
                rowsHtml += '<tr>';
                tds.each(function(i){
                    const cls = i === 1 || i === 3 ? 'text-right' : i === 2 ? 'text-center' : '';
                    rowsHtml += `<td class="${cls}">${$(this).text()}</td>`;
                });
                rowsHtml += '</tr>';
            }
        });

        const content = `<!DOCTYPE html><html><head><title>Order Estimate</title>
            <meta charset='utf-8' />
            <style>
                body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Arial,sans-serif;margin:28px;color:#222;}
                h1,h2,h3{margin:0;font-weight:600}
                .doc-header{text-align:center;margin-bottom:18px;padding-bottom:10px;border-bottom:3px solid #4b6cb7}
                table{width:100%;border-collapse:collapse;margin-top:10px;font-size:13px}
                th,td{border:1px solid #dcdfe3;padding:6px 8px}
                th{background:#f1f3f6;text-align:left;font-weight:600}
                .text-right{text-align:right}.text-center{text-align:center}
                .totals{margin-top:18px;max-width:280px;margin-left:auto;font-size:13px}
                .totals table{border:1px solid #dcdfe3}
                .totals td{border:0;padding:4px 0}
                .totals tr.divider td{border-top:1px solid #c7ccd1}
                .grand{font-size:15px;font-weight:700}
                .meta{font-size:11px;color:#555;margin-top:4px}
                .footer{text-align:center;margin-top:40px;font-style:italic;font-size:12px;color:#555}
                @media print { body{margin:8mm 10mm;} }
            </style>
        </head><body>
            <div class='doc-header'>
                <h2>Restaurant Management System</h2>
                <h3>ORDER ESTIMATE</h3>
                <div class='meta'>Estimate #: ${estimateNo} | Date: ${date.toLocaleDateString()} | ${meta}</div>
            </div>
            <table><thead><tr><th>Item</th><th class='text-right'>Unit Price</th><th class='text-center'>Qty</th><th class='text-right'>Amount</th></tr></thead><tbody>${rowsHtml}</tbody></table>
            <div class='totals'>
                <table style='width:100%;border-collapse:collapse;'>
                    <tr><td>Subtotal</td><td class='text-right'>${subtotal}</td></tr>
                    <tr><td>GST (5%)</td><td class='text-right'>${gst}</td></tr>
                    <tr class='divider grand'><td>Total</td><td class='text-right'>${grand}</td></tr>
                </table>
            </div>
            <div class='footer'>Thank you for choosing our restaurant!</div>
        </body></html>`;
        
        const win = window.open('', '_blank', 'width=900,height=650');
        win.document.write(content);
        win.document.close();
        win.focus();
        setTimeout(() => { win.print(); }, 400);
        showToast('Print ready', 'success');
    }
    
    function validateQuantity() {
        const val = parseInt($(this).val());
        if (isNaN(val) || val < 0) $(this).val(0);
    }
    
    function showLoading() {
        $('#menuItemsTableBody').html('<tr><td colspan="5" class="text-center"><div class="loading-spinner mx-auto"></div></td></tr>');
    }
    
    function showError(msg) {
        $('#menuItemsTableBody').html(`<tr><td colspan="5" class="text-center text-danger">${msg}</td></tr>`);
    }
    
    function showToast(message, type = 'info') {
        $('.toast-notification').remove();
        const colors = {success: 'bg-success', warning: 'bg-warning', error: 'bg-danger', info: 'bg-info'};
        const toast = $(`<div class="toast-notification position-fixed top-0 end-0 m-3 ${colors[type]} text-white p-3 rounded shadow">${message}</div>`);
        $('body').append(toast);
        setTimeout(() => toast.fadeOut(() => toast.remove()), 3000);
    }
});