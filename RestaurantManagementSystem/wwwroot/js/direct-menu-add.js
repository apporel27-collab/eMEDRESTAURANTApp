// direct-menu-add.js
// This script provides a direct and reliable way to add menu items in all scenarios

(function() {
    // Run when DOM is ready
    document.addEventListener('DOMContentLoaded', function() {
        initDirectMenuAdd();
    });
    
    // Also run on window load as a fallback
    window.addEventListener('load', function() {
        setTimeout(initDirectMenuAdd, 500);
    });
    
    function initDirectMenuAdd() {
        console.log('[DIRECT FIX] Initializing direct menu item add functionality');
        
        // Create a direct, reliable function to find menu items
        function findMenuItem(input) {
            if (!input) return null;
            
            console.log('[DIRECT FIX] Finding menu item:', input);
            
            let result = {
                id: null,
                name: null,
                price: null
            };
            
            try {
                // METHOD 1: Direct match by value in the datalist
                const options = document.querySelectorAll('#menuItems option');
                for (let i = 0; i < options.length; i++) {
                    const option = options[i];
                    if (option.value.toLowerCase() === input.toLowerCase()) {
                        result.id = option.getAttribute('data-id');
                        result.name = option.value;
                        result.price = parseFloat(option.getAttribute('data-price'));
                        console.log('[DIRECT FIX] Found exact match in datalist:', result.name);
                        return result;
                    }
                }
                
                // METHOD 2: Partial match by value
                for (let i = 0; i < options.length; i++) {
                    const option = options[i];
                    if (option.value.toLowerCase().includes(input.toLowerCase()) || 
                        input.toLowerCase().includes(option.value.toLowerCase())) {
                        result.id = option.getAttribute('data-id');
                        result.name = option.value;
                        result.price = parseFloat(option.getAttribute('data-price'));
                        console.log('[DIRECT FIX] Found partial match in datalist:', result.name);
                        return result;
                    }
                }
                
                // METHOD 3: Match by text content
                for (let i = 0; i < options.length; i++) {
                    const option = options[i];
                    const text = option.textContent || option.innerText;
                    if (text.toLowerCase().includes(input.toLowerCase())) {
                        result.id = option.getAttribute('data-id');
                        result.name = option.value;
                        result.price = parseFloat(option.getAttribute('data-price'));
                        console.log('[DIRECT FIX] Found match by text content in datalist:', result.name);
                        return result;
                    }
                }
            } catch (err) {
                console.error('[DIRECT FIX] Error finding menu item:', err);
            }
            
            return null;
        }
        
        // Main function to directly add menu items
        function directAddMenuItem() {
            try {
                console.log('[DIRECT FIX] Direct add menu item triggered');
                
                // Get input values
                const input = document.getElementById('menuItemInput').value.trim();
                const quantityInput = document.getElementById('quantity').value;
                const quantity = parseInt(quantityInput) || 1;
                
                if (!input) {
                    showToast('error', 'Please enter a menu item name');
                    return false;
                }
                
                if (quantity < 1) {
                    showToast('error', 'Quantity must be at least 1');
                    return false;
                }
                
                // Find the menu item
                const menuItem = findMenuItem(input);
                
                if (!menuItem || !menuItem.id) {
                    showToast('error', 'Menu item not found. Please select from the dropdown list.');
                    return false;
                }
                
                const menuItemId = menuItem.id;
                const menuItemName = menuItem.name;
                const menuItemPrice = menuItem.price;
                
                console.log('[DIRECT FIX] Menu item found:', menuItem);
                
                // Check if this item already exists in the order
                // First, make sure we have the latest state of existing items
                updateExistingItemsMap();
                
                if (window.existingItemsMap && window.existingItemsMap[menuItemId]) {
                    // Update existing item quantity instead of adding new
                    const existingItem = window.existingItemsMap[menuItemId];
                    
                    try {
                        // Find the quantity field and update it
                        const qtyInput = existingItem.row.querySelector('input[name="quantity"]');
                        const newQuantity = existingItem.quantity + quantity;
                        
                        if (qtyInput) {
                            qtyInput.value = newQuantity;
                            
                            // Update the display quantity if it exists
                            const qtyDisplay = existingItem.row.querySelector('.qty-display');
                            if (qtyDisplay) {
                                qtyDisplay.textContent = newQuantity;
                            }
                            
                            // Update the subtotal if it exists
                            const newSubtotal = (newQuantity * existingItem.price).toFixed(2);
                            const subtotalDisplay = existingItem.row.querySelector('.subtotal-display');
                            if (subtotalDisplay) {
                                subtotalDisplay.textContent = '₹' + newSubtotal;
                            }
                            
                            // Update our tracking
                            existingItem.quantity = newQuantity;
                            
                            // Highlight the row
                            existingItem.row.classList.add('highlight-row');
                            setTimeout(function() {
                                existingItem.row.classList.remove('highlight-row');
                            }, 2000);
                            
                            // Show success message
                            showToast('success', `Updated ${menuItemName} quantity to ${newQuantity}`);
                            
                            // Clear the inputs
                            document.getElementById('menuItemInput').value = '';
                            document.getElementById('quantity').value = '1';
                            document.getElementById('menuItemInput').focus();
                            
                            return true;
                        }
                    } catch (updateErr) {
                        console.error('[DIRECT FIX] Error updating existing item:', updateErr);
                    }
                }
                
                // If we get here, we need to add a new item
                console.log('[DIRECT FIX] Adding new menu item row');
                
                // Generate a temporary ID for the new item
                let tempId = -1;
                if (typeof window.nextTempId === 'number') {
                    tempId = window.nextTempId--;
                } else {
                    window.nextTempId = -2;
                }
                
                // Calculate subtotal
                const subtotal = (quantity * menuItemPrice).toFixed(2);
                
                // Create the new row HTML
                const newRow = createNewItemRow(tempId, menuItemId, menuItemName, menuItemPrice, quantity, subtotal);
                
                // Find or create the table body to insert the row
                let tbody = ensureOrderTableExists(subtotal);
                
                if (tbody) {
                    // Insert the new row
                    tbody.insertAdjacentHTML('afterbegin', newRow);
                    
                    // Store the new item in our tracking
                    if (!window.newOrderItems) {
                        window.newOrderItems = [];
                    }
                    
                    window.newOrderItems.push({
                        tempId: tempId,
                        menuItemId: menuItemId,
                        menuItemName: menuItemName,
                        quantity: quantity,
                        unitPrice: menuItemPrice,
                        specialInstructions: '',
                        isNew: true
                    });
                    
                    // Update our existing items map
                    if (!window.existingItemsMap) {
                        window.existingItemsMap = {};
                    }
                    
                    const insertedRow = document.querySelector(`tr[data-temp-id="${tempId}"]`);
                    window.existingItemsMap[menuItemId] = {
                        orderItemId: tempId,
                        quantity: quantity,
                        price: menuItemPrice,
                        name: menuItemName,
                        row: insertedRow
                    };
                    
                    // Clear the inputs
                    document.getElementById('menuItemInput').value = '';
                    document.getElementById('quantity').value = '1';
                    document.getElementById('menuItemInput').focus();
                    
                    // Show success message
                    showToast('success', `Added ${menuItemName} to the order`);
                    
                    return true;
                } else {
                    console.error('[DIRECT FIX] Could not find or create table body');
                    showToast('error', 'Could not add item to the order table');
                    return false;
                }
            } catch (err) {
                console.error('[DIRECT FIX] Error adding menu item:', err);
                showToast('error', 'An error occurred while adding the menu item');
                return false;
            }
        }
        
        // Create a new item row HTML
        function createNewItemRow(tempId, menuItemId, menuItemName, menuItemPrice, quantity, subtotal) {
            return `
                <tr class="new-item-row" data-temp-id="${tempId}" data-menu-item-id="${menuItemId}">
                    <td>
                        <input type="checkbox" class="fireItem new-item-checkbox" value="${tempId}" form="fireItemsForm" name="SelectedItems" data-is-new="true" />
                        <button type="button" class="btn btn-sm btn-outline-danger" title="Remove Item" onclick="window.removeNewItem(${tempId})">
                            <i class="fas fa-times"></i>
                        </button>
                    </td>
                    <td>
                        <div><strong>${menuItemName}</strong></div>
                        <form class="d-flex align-items-center gap-2 mt-1 order-item-edit-form new-item-form">
                            <input type="hidden" name="orderId" value="${document.getElementById('orderId').value}" />
                            <input type="hidden" name="orderItemId" value="${tempId}" />
                            <input type="hidden" name="menuItemId" value="${menuItemId}" />
                            <input type="hidden" name="menuItemName" value="${menuItemName}" />
                            <input type="hidden" name="unitPrice" value="${menuItemPrice}" />
                            <input type="number" name="quantity" value="${quantity}" min="1" class="form-control form-control-sm w-auto item-qty" style="max-width:60px;" onchange="if(window.updateNewItemDetails)window.updateNewItemDetails(${tempId}, this.value, ${menuItemPrice})" />
                            <input type="text" name="specialInstructions" value="" placeholder="Note" class="form-control form-control-sm w-auto item-note" style="max-width:120px;" />
                        </form>
                    </td>
                    <td class="align-middle qty-display">${quantity}</td>
                    <td class="align-middle">₹${menuItemPrice.toFixed(2)}</td>
                    <td class="align-middle subtotal-display">₹${subtotal}</td>
                    <td class="align-middle">
                        <span class="badge bg-info">New (Unsaved)</span>
                    </td>
                </tr>
            `;
        }
        
        function ensureOrderTableExists(initialSubtotal) {
            let tbody = document.querySelector('#orderItemsTable tbody');
            if (tbody) {
                return tbody;
            }

            const orderSection = document.getElementById('orderItemsSection');
            if (!orderSection) {
                console.warn('[DIRECT FIX] orderItemsSection not found');
                return null;
            }

            // Attempt to capture an existing anti-forgery token if one already exists on the page
            let antiForgeryToken = null;
            const existingToken = document.querySelector('input[name="__RequestVerificationToken"]');
            if (existingToken) {
                antiForgeryToken = existingToken.value;
            }

            orderSection.innerHTML = createOrderTable(initialSubtotal, antiForgeryToken);

            // Bind form submission safeguard if helper is available
            const submitOrderForm = orderSection.querySelector('#submitOrderForm');
            if (submitOrderForm && !submitOrderForm.dataset.directAddBound) {
                submitOrderForm.addEventListener('submit', function() {
                    if (typeof window.includeNewItemsInFormData === 'function') {
                        window.includeNewItemsInFormData();
                    }
                });
                submitOrderForm.dataset.directAddBound = 'true';
            }

            return orderSection.querySelector('#orderItemsTable tbody');
        }

        // Create a complete order table HTML
        function createOrderTable(subtotal, antiForgeryToken) {
            return `
                <div class="table-responsive">
                    <table id="orderItemsTable" class="table table-striped table-hover">
                        <thead>
                            <tr>
                                <th style="width: 90px">Actions</th>
                                <th>Item</th>
                                <th style="width: 80px">Qty</th>
                                <th style="width: 100px">Unit Price</th>
                                <th style="width: 100px">Subtotal</th>
                                <th style="width: 120px">Status</th>
                            </tr>
                        </thead>
                        <tbody></tbody>
                        <tfoot>
                            <tr>
                                <td colspan="6" class="text-center">
                                    <form id="submitOrderForm" action="/Order/UpdateMultipleOrderItems" method="post" style="display:inline;" onsubmit="return false;">
                                        <input type="hidden" name="orderId" value="${document.getElementById('orderId').value}" />
                                        ${antiForgeryToken ? `<input type="hidden" name="__RequestVerificationToken" value="${antiForgeryToken}" />` : ''}
                                        <input type="hidden" name="submitType" value="orderSave" />
                                        <button type="button" id="saveOrderDetailsBtn" class="btn btn-primary btn-lg btn-save-order-details" onclick="try { if(typeof window.guaranteedOrderSave === 'function') { window.guaranteedOrderSave(); } else if(typeof submitOrderWithEdits === 'function') { submitOrderWithEdits(); } else { document.getElementById('submitOrderForm').submit(); } } catch(e) { console.error(e); alert('Attempting direct form submission'); document.getElementById('submitOrderForm').submit(); }">
                                            <i class="fas fa-save"></i> Save Order Details
                                        </button>
                                    </form>
                                </td>
                            </tr>
                            <tr class="table-light">
                                <td colspan="4" class="text-end"><strong>Subtotal:</strong></td>
                                <td colspan="2">
                                    <strong class="order-subtotal">₹${subtotal}</strong>
                                </td>
                            </tr>
                        </tfoot>
                    </table>
                </div>
            `;
        }
        
        // Function to track existing items in the order
        function updateExistingItemsMap() {
            window.existingItemsMap = {};

            const editForms = document.querySelectorAll('#orderItemsTable tbody form.order-item-edit-form');

            editForms.forEach(form => {
                try {
                    const menuItemInput = form.querySelector('input[name="menuItemId"]');
                    if (!menuItemInput) {
                        return;
                    }

                    const menuItemId = menuItemInput.value;
                    if (!menuItemId) {
                        return;
                    }

                    const quantityInput = form.querySelector('input[name="quantity"]');
                    const unitPriceInput = form.querySelector('input[name="unitPrice"]');

                    const quantity = quantityInput ? (parseInt(quantityInput.value, 10) || 1) : 1;
                    const price = unitPriceInput ? (parseFloat(unitPriceInput.value) || 0) : 0;
                    const row = form.closest('tr');

                    window.existingItemsMap[menuItemId] = {
                        quantity: quantity,
                        price: price,
                        row: row
                    };
                } catch (err) {
                    console.error('[DIRECT FIX] Error mapping row:', err);
                }
            });

            console.log('[DIRECT FIX] Existing items map updated:', window.existingItemsMap);
        }
        
        // Helper function to show toast messages
        function showToast(type, message) {
            if (typeof toastr !== 'undefined') {
                toastr[type](message);
            } else {
                if (type === 'error') {
                    alert('Error: ' + message);
                } else {
                    alert(message);
                }
            }
        }
        
        // Add CSS for highlighting rows
        const style = document.createElement('style');
        style.textContent = '.highlight-row { background-color: #fff8c5 !important; transition: background-color 2s; }';
        document.head.appendChild(style);
        
        // Override the quick add form submit event
        const quickAddForm = document.getElementById('quickAddForm');
        if (quickAddForm) {
            quickAddForm.addEventListener('submit', function(event) {
                event.preventDefault();
                directAddMenuItem();
                return false;
            });
            
            // Remove any existing handler
            if (quickAddForm.getAttribute('onsubmit')) {
                quickAddForm.removeAttribute('onsubmit');
            }
        }
        
        // Override the quick add button click event
        const quickAddButton = document.getElementById('quickAddButton');
        if (quickAddButton) {
            quickAddButton.addEventListener('click', function(event) {
                event.preventDefault();
                directAddMenuItem();
                return false;
            });
            
            // Remove any existing handler
            if (quickAddButton.getAttribute('onclick')) {
                quickAddButton.removeAttribute('onclick');
            }
        }
        
        // Also handle the fallback button
        const fallbackAddButton = document.getElementById('fallbackAddButton');
        if (fallbackAddButton) {
            fallbackAddButton.addEventListener('click', function(event) {
                event.preventDefault();
                directAddMenuItem();
                return false;
            });
            
            // Remove any existing handler
            if (fallbackAddButton.getAttribute('onclick')) {
                fallbackAddButton.removeAttribute('onclick');
            }
        }
        
        // Initialize our tracking of existing items
        updateExistingItemsMap();
        
        // Expose our function to the global scope
        window.directAddMenuItem = directAddMenuItem;
        window.updateExistingItemsMap = updateExistingItemsMap;
        
        console.log('[DIRECT FIX] Direct menu add functionality initialized');
    }
})();