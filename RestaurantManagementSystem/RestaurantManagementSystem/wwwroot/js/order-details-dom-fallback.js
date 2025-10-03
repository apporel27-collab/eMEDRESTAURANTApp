// Pure DOM implementation for fallback in case jQuery fails
const domFallbackScript = {
    // Function to initialize when the DOM is ready
    init: function() {
        console.log('DOM Fallback script initialized');
        
        // Initialize menu items map
        this.initializeMenuItemsMap();
        
        // Add event listeners
        const quickAddButton = document.getElementById('quickAddButton');
        const quickAddForm = document.getElementById('quickAddForm');
        
        if (quickAddButton) {
            quickAddButton.addEventListener('click', function(e) {
                e.preventDefault();
                domFallbackScript.addMenuItem();
                return false;
            });
        }
        
        if (quickAddForm) {
            quickAddForm.addEventListener('submit', function(e) {
                e.preventDefault();
                domFallbackScript.addMenuItem();
                return false;
            });
        }
        
        // Look for Save Order Details button and add event listener
        const submitButtons = document.querySelectorAll('#submitOrderForm button[type="button"]');
        if (submitButtons && submitButtons.length > 0) {
            submitButtons.forEach(function(button) {
                button.addEventListener('click', function() {
                    domFallbackScript.submitOrderWithEdits();
                });
            });
        }
    },
    
    // Initialize menu items map
    initializeMenuItemsMap: function() {
        if (window.menuItemsInitialized) {
            return true;
        }
        
        try {
            window.menuItemsMap = {};
            window.nextTempId = window.nextTempId || -1;
            window.newOrderItems = window.newOrderItems || [];
            
            const options = document.querySelectorAll('#menuItems option');
            options.forEach(function(option) {
                const name = option.value.toLowerCase();
                const id = option.dataset.id;
                const price = parseFloat(option.dataset.price);
                
                if (name && id) {
                    window.menuItemsMap[name] = {
                        id: id,
                        name: option.value,
                        price: price
                    };
                }
            });
            
            window.menuItemsInitialized = true;
            console.log("DOM: Menu items map initialized with", Object.keys(window.menuItemsMap).length, "items");
            return true;
        } catch (error) {
            console.error("DOM: Error initializing menu items map:", error);
            return false;
        }
    },
    
    // Add menu item function
    addMenuItem: function() {
        try {
            const menuItemInput = document.getElementById('menuItemInput').value;
            const qty = parseInt(document.getElementById('quantity').value) || 1;
            
            console.log('DOM: Adding menu item:', menuItemInput, 'quantity:', qty);
            
            if (!menuItemInput) {
                if (typeof toastr !== 'undefined') {
                    toastr.error('Please select a menu item');
                } else {
                    console.error('Please select a menu item');
                }
                return;
            }
            
            if (qty < 1) {
                if (typeof toastr !== 'undefined') {
                    toastr.error('Quantity must be at least 1');
                } else {
                    console.error('Quantity must be at least 1');
                }
                return;
            }
            
            // Make sure the menu items map is initialized
            if (!window.menuItemsInitialized) {
                this.initializeMenuItemsMap();
            }
            
            // Find the selected menu item
            let menuItemId = null;
            let menuItemPrice = null;
            let menuItemName = null;
            
            // Try direct lookup in the map
            if (window.menuItemsMap && window.menuItemsMap[menuItemInput.toLowerCase()]) {
                const entry = window.menuItemsMap[menuItemInput.toLowerCase()];
                menuItemId = entry.id;
                menuItemName = entry.name;
                menuItemPrice = entry.price;
            } else {
                // Try to find by ID or name
                const options = document.querySelectorAll('#menuItems option');
                
                // First check if it's a direct ID input
                if (!isNaN(parseInt(menuItemInput))) {
                    const id = parseInt(menuItemInput);
                    for (const option of options) {
                        if (parseInt(option.dataset.id) === id) {
                            menuItemId = id;
                            menuItemName = option.value;
                            menuItemPrice = parseFloat(option.dataset.price);
                            break;
                        }
                    }
                }
                
                // If not found, try exact match by name
                if (!menuItemId) {
                    for (const option of options) {
                        if (option.value.toLowerCase() === menuItemInput.toLowerCase()) {
                            menuItemId = parseInt(option.dataset.id);
                            menuItemName = option.value;
                            menuItemPrice = parseFloat(option.dataset.price);
                            break;
                        }
                    }
                }
                
                // If still not found, try partial match
                if (!menuItemId) {
                    for (const option of options) {
                        if (option.value.toLowerCase().includes(menuItemInput.toLowerCase())) {
                            menuItemId = parseInt(option.dataset.id);
                            menuItemName = option.value;
                            menuItemPrice = parseFloat(option.dataset.price);
                            break;
                        }
                    }
                }
            }
            
            if (!menuItemId || !menuItemName) {
                if (typeof toastr !== 'undefined') {
                    toastr.error('Menu item not found. Please select from the dropdown list.');
                } else {
                    console.error('Menu item not found. Please select from the dropdown list.');
                }
                return;
            }
            
            console.log('DOM: Menu item found:', { id: menuItemId, name: menuItemName, price: menuItemPrice });
            
            // Calculate subtotal
            const subtotal = (qty * menuItemPrice).toFixed(2);
            
            // Add the item to the table
            this.addItemToTable(menuItemId, menuItemName, menuItemPrice, qty, subtotal);
            
        } catch (error) {
            console.error("DOM: Error adding menu item:", error);
            if (typeof toastr !== 'undefined') {
                toastr.error("Error adding menu item: " + error.message);
            } else {
                console.error("Error adding menu item: " + error.message);
            }
        }
    },
    
    // Add item to the table
    addItemToTable: function(menuItemId, menuItemName, menuItemPrice, qty, subtotal) {
        const tempId = window.nextTempId--;
        const orderId = document.getElementById('orderId').value;
        
        // Create the new row
        const newRow = document.createElement('tr');
        newRow.className = 'new-item-row';
        newRow.dataset.tempId = tempId;
        
        newRow.innerHTML = `
            <td>
                <input type="checkbox" class="fireItem new-item-checkbox" value="${tempId}" form="fireItemsForm" name="SelectedItems" data-is-new="true" />
                <button type="button" class="btn btn-sm btn-outline-danger" title="Remove Item" onclick="domFallbackScript.removeNewItem(${tempId})">
                    <i class="fas fa-times"></i>
                </button>
            </td>
            <td>
                <div><strong>${menuItemName}</strong></div>
                <form class="d-flex align-items-center gap-2 mt-1 order-item-edit-form new-item-form">
                    <input type="hidden" name="orderId" value="${orderId}" />
                    <input type="hidden" name="orderItemId" value="${tempId}" />
                    <input type="hidden" name="menuItemId" value="${menuItemId}" />
                    <input type="hidden" name="menuItemName" value="${menuItemName}" />
                    <input type="hidden" name="unitPrice" value="${menuItemPrice}" />
                    <input type="number" name="quantity" value="${qty}" min="1" class="form-control form-control-sm w-auto item-qty" style="max-width:60px;" onchange="domFallbackScript.updateNewItemDetails(${tempId}, this.value, ${menuItemPrice})" />
                    <input type="text" name="specialInstructions" value="" placeholder="Note" class="form-control form-control-sm w-auto item-note" style="max-width:120px;" />
                </form>
            </td>
            <td class="align-middle qty-display">${qty}</td>
            <td class="align-middle">₹${menuItemPrice.toFixed(2)}</td>
            <td class="align-middle subtotal-display">₹${subtotal}</td>
            <td class="align-middle">
                <span class="badge bg-info">New (Unsaved)</span>
            </td>
        `;
        
        // Find or create the table
        let tbody = document.querySelector('#orderItemsTable tbody');
        if (!tbody) {
            // Table doesn't exist yet, we need to create it
            const tableHtml = `
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
                                    <form id="submitOrderForm" action="/Order/UpdateMultipleOrderItems" method="post" style="display:inline;">
                                        <input type="hidden" name="orderId" value="${orderId}" />
                                        <input type="hidden" name="__RequestVerificationToken" value="${document.querySelector('input[name="__RequestVerificationToken"]').value}" />
                                        <button type="button" class="btn btn-primary btn-lg" onclick="domFallbackScript.submitOrderWithEdits()" 
                                                data-bs-toggle="tooltip" title="Save all order details, quantities, prices, and recalculate totals">
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
                </div>`;
                
            // Replace the "No items" message or add the table
            const noItemsMsg = document.querySelector('.alert.alert-info');
            if (noItemsMsg && noItemsMsg.innerText.includes('No items added')) {
                noItemsMsg.insertAdjacentHTML('afterend', tableHtml);
                noItemsMsg.style.display = 'none';
            } else {
                // Just append it to the container
                const container = document.querySelector('.order-items-container');
                if (container) {
                    container.insertAdjacentHTML('beforeend', tableHtml);
                }
            }
            
            // Get the new tbody reference
            tbody = document.querySelector('#orderItemsTable tbody');
        }
        
        // Add the row to the table
        if (tbody) {
            const firstRow = tbody.querySelector('tr:first-child');
            if (firstRow && firstRow.classList.contains('table-secondary')) {
                firstRow.insertAdjacentElement('afterend', newRow);
            } else {
                tbody.insertBefore(newRow, tbody.firstChild);
            }
            
            // Store the new item data
            if (!window.newOrderItems) {
                window.newOrderItems = [];
            }
            
            window.newOrderItems.push({
                tempId: tempId,
                menuItemId: menuItemId,
                menuItemName: menuItemName,
                quantity: qty,
                unitPrice: menuItemPrice,
                specialInstructions: '',
                isNew: true
            });
            
            // Clear the form
            document.getElementById('menuItemInput').value = '';
            document.getElementById('quantity').value = '1';
            document.getElementById('menuItemInput').focus();
            
            // Update order totals
            this.updateOrderTotals();
            
            // Show success message using toastr if available, otherwise fall back to console
            if (typeof toastr !== 'undefined') {
                toastr.success(menuItemName + ' added to order. Remember to click "Save Order Details" to save to database.');
            } else {
                console.log(menuItemName + ' added to order. Remember to click "Save Order Details" to save to database.');
            }
        }
    },
    
    // Update item details when quantity changes
    updateNewItemDetails: function(tempId, newQty, unitPrice) {
        newQty = parseInt(newQty) || 1;
        if (newQty < 1) newQty = 1;
        
        const row = document.querySelector(`tr[data-temp-id="${tempId}"]`);
        if (!row) {
            console.error('DOM: Row not found for tempId:', tempId);
            return;
        }
        
        // Update quantity display
        const qtyDisplay = row.querySelector('.qty-display');
        if (qtyDisplay) qtyDisplay.textContent = newQty;
        
        // Update subtotal
        const subtotal = (newQty * unitPrice).toFixed(2);
        const subtotalDisplay = row.querySelector('.subtotal-display');
        if (subtotalDisplay) subtotalDisplay.textContent = '₹' + subtotal;
        
        // Update in the newOrderItems array
        if (window.newOrderItems && Array.isArray(window.newOrderItems)) {
            const itemIndex = window.newOrderItems.findIndex(item => item.tempId === tempId);
            if (itemIndex !== -1) {
                window.newOrderItems[itemIndex].quantity = newQty;
            }
        }
        
        // Update overall order total
        this.updateOrderTotals();
    },
    
    // Update order totals
    updateOrderTotals: function() {
        // Calculate total from all items
        let total = 0;
        
        // Add up existing items
        const existingSubtotals = document.querySelectorAll('.order-item-row .subtotal-display');
        existingSubtotals.forEach(function(element) {
            const subtotal = parseFloat(element.textContent.replace(/[^\d.-]/g, '')) || 0;
            total += subtotal;
        });
        
        // Add up new items
        const newSubtotals = document.querySelectorAll('.new-item-row .subtotal-display');
        newSubtotals.forEach(function(element) {
            const subtotal = parseFloat(element.textContent.replace(/[^\d.-]/g, '')) || 0;
            total += subtotal;
        });
        
        // Update the order subtotal display
        const subtotalDisplay = document.querySelector('.order-subtotal');
        if (subtotalDisplay) {
            subtotalDisplay.textContent = '₹' + total.toFixed(2);
        }
    },
    
    // Remove a new item
    removeNewItem: function(tempId) {
        if (confirm('Are you sure you want to remove this item?')) {
            const row = document.querySelector(`tr[data-temp-id="${tempId}"]`);
            if (row) {
                row.remove();
                
                // Remove from the newOrderItems array
                if (window.newOrderItems && Array.isArray(window.newOrderItems)) {
                    window.newOrderItems = window.newOrderItems.filter(item => item.tempId !== tempId);
                }
                
                // Update order totals
                this.updateOrderTotals();
            }
        }
    },
    
    // Save order details
    submitOrderWithEdits: function() {
        console.log('DOM: Submitting order with edits');
        
        const orderId = document.getElementById('orderId').value;
        if (!orderId) {
            if (typeof toastr !== 'undefined') {
                toastr.error('Order ID not found');
            } else {
                console.error('Order ID not found');
            }
            return;
        }
        
        // Show loading state on button
        const submitButton = document.querySelector('#submitOrderForm button[type="button"]');
        if (submitButton) {
            submitButton.disabled = true;
            submitButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Saving...';
        }
        
        // Collect items to update
        const itemsToUpdate = [];
        
        // Get existing items
        const existingForms = document.querySelectorAll('.order-item-edit-form:not(.new-item-form)');
        existingForms.forEach(function(form) {
            const orderItemId = parseInt(form.querySelector('input[name="orderItemId"]').value);
            const quantity = parseInt(form.querySelector('input[name="quantity"]').value) || 1;
            const specialInstructions = form.querySelector('input[name="specialInstructions"]').value || '';
            
            if (orderItemId && quantity > 0) {
                itemsToUpdate.push({
                    OrderItemId: orderItemId,
                    Quantity: quantity,
                    SpecialInstructions: specialInstructions,
                    IsNew: false
                });
            }
        });
        
        // Get new items
        if (window.newOrderItems && window.newOrderItems.length > 0) {
            window.newOrderItems.forEach(function(item) {
                const row = document.querySelector(`tr[data-temp-id="${item.tempId}"]`);
                if (row) {
                    const currentQuantity = parseInt(row.querySelector('input[name="quantity"]').value) || item.quantity;
                    const currentInstructions = row.querySelector('input[name="specialInstructions"]').value || '';
                    
                    itemsToUpdate.push({
                        OrderItemId: item.tempId,
                        MenuItemId: parseInt(item.menuItemId),
                        Quantity: currentQuantity,
                        SpecialInstructions: currentInstructions,
                        IsNew: true,
                        TempId: item.tempId
                    });
                }
            });
        }
        
        if (itemsToUpdate.length === 0) {
            if (typeof toastr !== 'undefined') {
                toastr.info('No items to save.');
            } else {
                console.info('No items to save.');
            }
            if (submitButton) {
                submitButton.disabled = false;
                submitButton.innerHTML = '<i class="fas fa-save"></i> Save Order Details';
            }
            return;
        }
        
        // Get anti-forgery token
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
        
        // Make the API call using fetch API
        fetch(`/Order/UpdateMultipleOrderItems?orderId=${orderId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify(itemsToUpdate)
        })
        .then(response => response.json())
        .then(data => {
            console.log('DOM: Server response:', data);
            
            if (data && data.success) {
                if (typeof toastr !== 'undefined') {
                    toastr.success(data.message || 'Order updated successfully');
                } else {
                    console.log(data.message || 'Order updated successfully');
                }
                
                // Clear the new items array
                window.newOrderItems = [];
                
                // Reload the page
                setTimeout(function() {
                    window.location.reload();
                }, 1000);
            } else {
                if (typeof toastr !== 'undefined') {
                    toastr.error(data ? data.message : 'Failed to update order');
                } else {
                    console.error(data ? data.message : 'Failed to update order');
                }
                if (submitButton) {
                    submitButton.disabled = false;
                    submitButton.innerHTML = '<i class="fas fa-save"></i> Save Order Details';
                }
            }
        })
        .catch(error => {
            console.error('DOM: AJAX error:', error);
            if (typeof toastr !== 'undefined') {
                toastr.error('Failed to update order. Please try again.');
            } else {
                console.error('Failed to update order. Please try again.');
            }
            
            if (submitButton) {
                submitButton.disabled = false;
                submitButton.innerHTML = '<i class="fas fa-save"></i> Save Order Details';
            }
        });
    }
};

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    domFallbackScript.init();
});

// Make it globally available
window.domFallbackScript = domFallbackScript;