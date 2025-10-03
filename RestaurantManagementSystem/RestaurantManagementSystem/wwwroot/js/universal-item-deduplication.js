// universal-item-deduplication.js
// This script ensures that menu items are properly deduplicated regardless of order ID or page structure

(function() {
    // Execute when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initUniversalDeduplication);
    } else {
        initUniversalDeduplication();
    }
    
    // Also execute after window load to ensure all scripts have run
    window.addEventListener('load', function() {
        setTimeout(initUniversalDeduplication, 500);
    });
    
    function initUniversalDeduplication() {
        console.log('[UNIVERSAL FIX] Initializing universal item deduplication');
        
        // Create a robust map builder that works across different DOM structures
        window.universalUpdateOrderItemsMap = function() {
            console.log('[UNIVERSAL FIX] Updating universal order items map');
            
            // Initialize or reset the map
            if (!window.orderItemsMap) window.orderItemsMap = {};
            
            // First try the standard structure
            $('.order-item-edit-form').each(function() {
                try {
                    const form = $(this);
                    const menuItemId = parseInt(form.find('input[name="menuItemId"]').val());
                    const orderItemId = form.find('input[name="orderItemId"]').val();
                    const quantity = parseInt(form.find('input[name="quantity"]').val()) || 1;
                    const price = parseFloat(form.find('input[name="unitPrice"]').val()) || 0;
                    const row = form.closest('tr');
                    
                    if (menuItemId && !isNaN(menuItemId)) {
                        window.orderItemsMap[menuItemId] = {
                            orderItemId: orderItemId,
                            quantity: quantity,
                            price: price,
                            name: row.find('strong').first().text() || 'Item #' + menuItemId,
                            row: row
                        };
                        console.log('[UNIVERSAL FIX] Mapped item:', menuItemId);
                    }
                } catch (err) {
                    console.warn('[UNIVERSAL FIX] Error mapping form item:', err);
                }
            });
            
            // Also check for alternative structure
            $('.order-item-row').each(function() {
                try {
                    const row = $(this);
                    const menuItemId = parseInt(row.data('menu-item-id'));
                    
                    if (menuItemId && !isNaN(menuItemId) && !window.orderItemsMap[menuItemId]) {
                        const orderItemId = row.data('order-item-id');
                        const quantity = parseInt(row.find('.item-qty').val() || row.find('input[name="quantity"]').val()) || 1;
                        const priceText = row.find('.price-display').text() || '0';
                        const price = parseFloat(priceText.replace(/[^0-9.]/g, '')) || 0;
                        
                        window.orderItemsMap[menuItemId] = {
                            orderItemId: orderItemId,
                            quantity: quantity,
                            price: price,
                            name: row.find('.item-name').text() || 'Item #' + menuItemId,
                            row: row
                        };
                        console.log('[UNIVERSAL FIX] Mapped item from alternative structure:', menuItemId);
                    }
                } catch (err) {
                    console.warn('[UNIVERSAL FIX] Error mapping row item:', err);
                }
            });
            
            console.log('[UNIVERSAL FIX] Order items map updated:', window.orderItemsMap);
        };
        
        // Create a universal robust version of the addMenuItem function
        window.universalAddMenuItem = function() {
            console.log('[UNIVERSAL FIX] Universal add menu item called');
            
            // Ensure we have the latest map
            window.universalUpdateOrderItemsMap();
            
            const menuItemInput = $('#menuItemInput').val().trim();
            const quantity = parseInt($('#quantity').val()) || 1;
            
            if (!menuItemInput || quantity < 1) {
                toastr.error('Please enter a valid menu item and quantity');
                return false;
            }
            
            console.log('[UNIVERSAL FIX] Looking for menu item:', menuItemInput);
            
            // Enhanced menu item lookup logic
            let menuItemId = null;
            let menuItemName = null;
            let menuItemPrice = null;
            let found = false;
            
            // METHOD 1: Try direct lookup in the datalist by exact match
            $('#menuItems option').each(function() {
                if ($(this).val().toLowerCase() === menuItemInput.toLowerCase()) {
                    menuItemId = parseInt($(this).data('id'));
                    menuItemName = $(this).val();
                    menuItemPrice = parseFloat($(this).data('price'));
                    found = true;
                    console.log('[UNIVERSAL FIX] Found exact match in datalist:', menuItemName);
                    return false; // break the loop
                }
            });
            
            // METHOD 2: Try direct lookup by text content if not found
            if (!found) {
                $('#menuItems option').each(function() {
                    const optionText = $(this).text();
                    if (optionText.toLowerCase().includes(menuItemInput.toLowerCase())) {
                        menuItemId = parseInt($(this).data('id'));
                        menuItemName = $(this).val();
                        menuItemPrice = parseFloat($(this).data('price'));
                        found = true;
                        console.log('[UNIVERSAL FIX] Found by text content in datalist:', menuItemName);
                        return false; // break the loop
                    }
                });
            }
            
            // METHOD 3: Try fuzzy matching in the datalist
            if (!found) {
                $('#menuItems option').each(function() {
                    if ($(this).val().toLowerCase().includes(menuItemInput.toLowerCase()) ||
                        menuItemInput.toLowerCase().includes($(this).val().toLowerCase())) {
                        menuItemId = parseInt($(this).data('id'));
                        menuItemName = $(this).val();
                        menuItemPrice = parseFloat($(this).data('price'));
                        found = true;
                        console.log('[UNIVERSAL FIX] Found fuzzy match in datalist:', menuItemName);
                        return false; // break the loop
                    }
                });
            }
            
            // METHOD 4: Try menuItemsMap if available
            if (!found && window.menuItemsMap) {
                const lowerInput = menuItemInput.toLowerCase();
                
                // Try exact match
                if (window.menuItemsMap[lowerInput]) {
                    const entry = window.menuItemsMap[lowerInput];
                    menuItemId = parseInt(entry.id);
                    menuItemName = entry.name;
                    menuItemPrice = parseFloat(entry.price);
                    found = true;
                    console.log('[UNIVERSAL FIX] Found in menuItemsMap:', menuItemName);
                }
                // Try fuzzy matching
                else {
                    for (const key in window.menuItemsMap) {
                        if (key.includes(lowerInput) || lowerInput.includes(key)) {
                            const entry = window.menuItemsMap[key];
                            menuItemId = parseInt(entry.id);
                            menuItemName = entry.name;
                            menuItemPrice = parseFloat(entry.price);
                            found = true;
                            console.log('[UNIVERSAL FIX] Found with fuzzy match in menuItemsMap:', menuItemName);
                            break;
                        }
                    }
                }
            }
            
            if (!menuItemId || isNaN(menuItemId)) {
                console.error('[UNIVERSAL FIX] Menu item not found:', menuItemInput);
                toastr.warning('Menu item not found. Please select from the dropdown list.');
                return false;
            }
            
            console.log('[UNIVERSAL FIX] Selected menu item:', { id: menuItemId, name: menuItemName, price: menuItemPrice });
            
            // Check if this menu item already exists in the order
            if (window.orderItemsMap && window.orderItemsMap[menuItemId]) {
                // Item exists - update the quantity instead of adding new
                console.log('[UNIVERSAL FIX] Item already exists in order, updating quantity');
                const existingItem = window.orderItemsMap[menuItemId];
                const existingRow = existingItem.row;
                
                // Find quantity field using multiple selectors for maximum compatibility
                const existingQtyField = existingRow.find('input[name="quantity"], .item-qty');
                const existingDisplayQty = existingRow.find('.qty-display');
                const existingSubtotalDisplay = existingRow.find('.subtotal-display');
                
                // Update the quantity
                const newQuantity = existingItem.quantity + quantity;
                existingQtyField.val(newQuantity);
                if (existingDisplayQty.length > 0) {
                    existingDisplayQty.text(newQuantity);
                }
                
                // Update the subtotal
                const newSubtotal = (newQuantity * existingItem.price).toFixed(2);
                if (existingSubtotalDisplay.length > 0) {
                    existingSubtotalDisplay.text('₹' + newSubtotal);
                }
                
                // Update our tracking
                window.orderItemsMap[menuItemId].quantity = newQuantity;
                
                // Show success message
                toastr.success(`Updated ${menuItemName} quantity to ${newQuantity}`);
                
                // Highlight the row briefly
                existingRow.addClass('highlight-update');
                setTimeout(function() {
                    existingRow.removeClass('highlight-update');
                }, 2000);
                
                // Update totals if the function exists
                if (typeof updateOrderTotals === 'function') {
                    updateOrderTotals();
                }
                
                // Clear the input fields
                $('#menuItemInput').val('').focus();
                $('#quantity').val('1');
                
                return true;
            }
            
            // If not found, defer to the original addMenuItem function
            console.log('[UNIVERSAL FIX] Item not found in order, deferring to original addMenuItem');
            
            // Call the original function if it exists
            if (typeof addMenuItem === 'function') {
                return addMenuItem();
            } else if (typeof window.addMenuItemWithDOMFallback === 'function') {
                return window.addMenuItemWithDOMFallback();
            } else if (typeof checkAndAddMenuItem === 'function') {
                return checkAndAddMenuItem();
            }
            
            // If no original function exists, show error
            toastr.error('Could not add menu item. Please try again.');
            return false;
        };
        
        // Override the quickAddForm submit handler to use our universal function
        $('#quickAddForm').off('submit').on('submit', function(e) {
            e.preventDefault();
            window.universalAddMenuItem();
            return false;
        });
        
        // Override the quickAddButton click handler
        $('#quickAddButton, #fallbackAddButton').off('click').on('click', function(e) {
            e.preventDefault();
            window.universalAddMenuItem();
            return false;
        });
        
        // Initial map update
        window.universalUpdateOrderItemsMap();
        
        console.log('[UNIVERSAL FIX] Universal item deduplication initialized');
    }
})();