/**
 * Order Details Page Fallback Scripts
 * 
 * This file provides DOM-level fallbacks for critical functionality
 * when the jQuery methods might fail. It uses pure JavaScript to 
 * ensure menu items can be added even if there are jQuery errors.
 */

// Fallback function for adding menu items to the order
function addMenuItemWithDOMFallback() {
    try {
        console.log("DOM fallback: Adding menu item");
        
        // Get input values
        const menuItemInput = document.getElementById('menuItemInput');
        const quantityInput = document.getElementById('quantity');
        
        if (!menuItemInput || !quantityInput) {
            console.error("DOM fallback: Input elements not found");
            return false;
        }
        
        const input = menuItemInput.value.trim();
        if (!input) {
            if (typeof toastr !== 'undefined') {
                toastr.error('Please select a menu item');
            } else {
                alert('Please select a menu item');
            }
            return false;
        }
        
        // Parse quantity
        let qty = parseInt(quantityInput.value, 10);
        if (isNaN(qty) || qty < 1) qty = 1;
        
        // Find menu item in the datalist
        const menuItemOptions = document.querySelectorAll('#menuItems option');
        let selectedOption = null;
        
        // Try exact match first
        for (let i = 0; i < menuItemOptions.length; i++) {
            if (menuItemOptions[i].value.toLowerCase() === input.toLowerCase()) {
                selectedOption = menuItemOptions[i];
                break;
            }
        }
        
        // Try fuzzy match if no exact match
        if (!selectedOption) {
            const lowerInput = input.toLowerCase();
            
            for (let i = 0; i < menuItemOptions.length; i++) {
                const optionValue = menuItemOptions[i].value.toLowerCase();
                
                if (optionValue.includes(lowerInput)) {
                    selectedOption = menuItemOptions[i];
                    break;
                }
            }
        }
        
        if (!selectedOption) {
            if (typeof toastr !== 'undefined') {
                toastr.error('Menu item not found. Please select from the list.');
            } else {
                alert('Menu item not found. Please select from the list.');
            }
            return false;
        }
        
        // Get menu item details
        const menuItemId = selectedOption.getAttribute('data-id');
        const menuItemName = selectedOption.value;
        const menuItemPrice = parseFloat(selectedOption.getAttribute('data-price'));
        
        if (!menuItemId || !menuItemName || isNaN(menuItemPrice)) {
            console.error("DOM fallback: Invalid menu item data", {
                id: menuItemId,
                name: menuItemName,
                price: menuItemPrice
            });
            if (typeof toastr !== 'undefined') {
                toastr.error('Error loading menu item data. Please try again.');
            } else {
                alert('Error loading menu item data. Please try again.');
            }
            return false;
        }
        
        // Calculate subtotal
        const subtotal = (qty * menuItemPrice).toFixed(2);
        
        // Generate a unique ID
        const tempId = -Math.abs(new Date().getTime());
        
        // Create new row HTML
        const newRow = `
            <tr class="new-item-row" data-temp-id="${tempId}">
                <td>
                    <input type="checkbox" class="fireItem" value="${tempId}" form="fireItemsForm" name="SelectedItems" />
                    <button type="button" class="btn btn-sm btn-outline-danger" title="Remove Item" onclick="removeNewItem(${tempId})">
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
                        <input type="number" name="quantity" value="${qty}" min="1" class="form-control form-control-sm w-auto item-qty" style="max-width:60px;" onchange="updateNewItemDetails(${tempId}, this.value, ${menuItemPrice})" />
                        <input type="text" name="specialInstructions" value="" placeholder="Note" class="form-control form-control-sm w-auto item-note" style="max-width:120px;" />
                    </form>
                </td>
                <td class="align-middle qty-display">${qty}</td>
                <td class="align-middle">₹${menuItemPrice.toFixed(2)}</td>
                <td class="align-middle subtotal-display">₹${subtotal}</td>
                <td class="align-middle">
                    <span class="badge bg-info">New (Unsaved)</span>
                </td>
            </tr>
        `;
        
        // Find the table or create it
        let orderTable = document.getElementById('orderItemsTable');
        
        if (!orderTable) {
            // No table exists yet, create one
            const newTableHtml = `
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
                        <tbody>${newRow}</tbody>
                        <tfoot>
                            <tr>
                                <td colspan="6" class="text-center">
                                    <form id="submitOrderForm" action="/Order/SubmitOrder" method="post" style="display:inline;">
                                        <input type="hidden" name="orderId" value="${document.getElementById('orderId').value}" />
                                        <button type="button" class="btn btn-primary btn-lg" onclick="submitOrderWithEdits()" 
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
                </div>
            `;
            
            // Find alert or container to replace/append
            const noItemsAlert = document.querySelector('.alert.alert-info');
            
            if (noItemsAlert && noItemsAlert.textContent.includes('No items')) {
                // Replace the alert with the table
                noItemsAlert.outerHTML = newTableHtml;
            } else {
                // Add to the card body
                const cardBody = document.querySelector('.card-body');
                if (cardBody) {
                    const tempContainer = document.createElement('div');
                    tempContainer.innerHTML = newTableHtml;
                    cardBody.appendChild(tempContainer.firstChild);
                } else {
                    console.error("DOM fallback: Could not find container for table");
                    return false;
                }
            }
        } else {
            // Add to existing table
            let tbody = orderTable.querySelector('tbody');
            
            // Create tbody if needed
            if (!tbody) {
                tbody = document.createElement('tbody');
                orderTable.appendChild(tbody);
            }
            
            // Add the new row
            const tempElement = document.createElement('template');
            tempElement.innerHTML = newRow.trim();
            tbody.insertBefore(tempElement.content.firstChild, tbody.firstChild);
        }
        
        // Store item data for later submission - use global newOrderItems if available
        if (window.newOrderItems !== undefined) {
            // Use the existing array
            window.newOrderItems.push({
                tempId: tempId,
                menuItemId: menuItemId,
                menuItemName: menuItemName,
                quantity: qty,
                unitPrice: menuItemPrice,
                specialInstructions: '',
                isNew: true
            });
        } else {
            // Create a new array
            window.newOrderItems = [{
                tempId: tempId,
                menuItemId: menuItemId,
                menuItemName: menuItemName,
                quantity: qty,
                unitPrice: menuItemPrice,
                specialInstructions: '',
                isNew: true
            }];
        }
        
        // Update totals if the function exists
        if (typeof window.updateOrderTotals === 'function') {
            window.updateOrderTotals();
        } else {
            // Simple fallback to update the subtotal
            const orderSubtotalElement = document.querySelector('.order-subtotal');
            if (orderSubtotalElement) {
                let total = 0;
                
                // Sum all displayed subtotals
                document.querySelectorAll('.subtotal-display').forEach(element => {
                    const value = parseFloat(element.textContent.replace('₹', '').trim());
                    if (!isNaN(value)) {
                        total += value;
                    }
                });
                
                orderSubtotalElement.textContent = '₹' + total.toFixed(2);
            }
        }
        
        // Clear inputs
        menuItemInput.value = '';
        quantityInput.value = '1';
        menuItemInput.focus();
        
        // Show success message
        if (typeof toastr !== 'undefined') {
            toastr.success(menuItemName + ' added (unsaved). Click "Save Order Details" to save to database.');
        } else {
            alert(menuItemName + ' added (unsaved). Click "Save Order Details" to save to database.');
        }
        
        return true;
    } catch (error) {
        console.error("DOM fallback error:", error);
        return false;
    }
}

// Make function globally available
window.addMenuItemWithDOMFallback = addMenuItemWithDOMFallback;

/**
 * Simple fallback for updating item details
 */
function updateNewItemDetailsFallback(tempId, newQuantity, unitPrice) {
    try {
        newQuantity = parseInt(newQuantity) || 1;
        if (newQuantity < 1) newQuantity = 1;
        
        // Find the row by data attribute
        const row = document.querySelector(`tr[data-temp-id="${tempId}"]`);
        if (!row) return false;
        
        // Update quantity display
        const qtyDisplay = row.querySelector('.qty-display');
        if (qtyDisplay) qtyDisplay.textContent = newQuantity;
        
        // Update subtotal
        const subtotal = (newQuantity * unitPrice).toFixed(2);
        const subtotalDisplay = row.querySelector('.subtotal-display');
        if (subtotalDisplay) subtotalDisplay.textContent = '₹' + subtotal;
        
        // Update in newOrderItems if available
        if (window.newOrderItems) {
            for (let i = 0; i < window.newOrderItems.length; i++) {
                if (window.newOrderItems[i].tempId === tempId) {
                    window.newOrderItems[i].quantity = newQuantity;
                    break;
                }
            }
        }
        
        return true;
    } catch (error) {
        console.error("Fallback update error:", error);
        return false;
    }
}

// Make function globally available
window.updateNewItemDetailsFallback = updateNewItemDetailsFallback;